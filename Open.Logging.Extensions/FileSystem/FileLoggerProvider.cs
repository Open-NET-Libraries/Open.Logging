using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Open.Disposable;
using Open.Logging.Extensions.Writers;
using System.Globalization;
using System.Text.RegularExpressions;
using File = System.IO.File;

namespace Open.Logging.Extensions.FileSystem;

/// <summary>
/// A logger provider that writes logs to files with configurable formatting and retention policies.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2213:Disposable fields should be disposed",
	Justification = "Occurs in OnDisposeAsync")]
public sealed partial class FileLoggerProvider
	: AsyncDisposableBase
	, ILoggerProvider
	, IAsyncDisposable
{
	private readonly FileLoggerFormatterOptions _options;
	private readonly TemplateTextLogEntryWriter _writer;
	private readonly BufferedLogWriter<TextWriter> _bufferedWriter;
	private StreamWriter _fileWriter;
	private readonly DateTimeOffset _startTime;
	private readonly Lock _lock = new();

	// Regular expression to match placeholders in the file name pattern
	private static readonly Regex PlaceholderRegex = FileNamePlaceholderPattern();

	[GeneratedRegex(@"\{([^}]+)\}", RegexOptions.Compiled)]
	private static partial Regex FileNamePlaceholderPattern();

	/// <summary>
	/// Gets the path to the current log file.
	/// </summary>
	public string FilePath { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="FileLoggerProvider"/> class with default options.
	/// </summary>
	public FileLoggerProvider()
		: this(new FileLoggerFormatterOptions())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="FileLoggerProvider"/> class with the specified options.
	/// </summary>
	/// <param name="options">The options for configuring the file logger.</param>
	public FileLoggerProvider(FileLoggerFormatterOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);
		_options = options;
		_startTime = _options.StartTime;

		// Ensure the log directory exists
		Directory.CreateDirectory(_options.LogDirectory);

		// Apply file retention policy if configured
		ApplyRetentionPolicy();

		// Create the log file path
		FilePath = GetFormattedFilePath();

		// Create or open the log file
		_fileWriter = File.AppendText(FilePath);

		// Create the template writer
		_writer = new TemplateTextLogEntryWriter(_options);

		// Create a buffered writer that uses the template writer to format logs
		_bufferedWriter = new BufferedLogWriter<TextWriter>(
			WriteLogEntry,
			_startTime,
			_options.BufferSize,
			_options.AllowSynchronousContinuations,
			() => new(_fileWriter.FlushAsync()));
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="FileLoggerProvider"/> class with options from the DI container.
	/// </summary>
	/// <param name="options">The options for configuring the file logger.</param>
	public FileLoggerProvider(IOptionsSnapshot<FileLoggerFormatterOptions> options)
		: this(options?.Value ?? new FileLoggerFormatterOptions())
	{
	}

	/// <inheritdoc />
	protected override async ValueTask OnDisposeAsync()
	{
		try
		{
			// Flush any remaining logs in the buffer
			await _bufferedWriter.DisposeAsync().ConfigureAwait(false);

			// Close the file writer
			await _fileWriter.FlushAsync().ConfigureAwait(false);
			await _fileWriter.DisposeAsync().ConfigureAwait(false);
		}
		catch
		{
			// Swallow exceptions during disposal
#if DEBUG
			throw;
#endif
		}
	}

	/// <inheritdoc />
	public void Dispose()
	{
		// Dispose asynchronously to ensure all logs are flushed
		DisposeAsync().AsTask().Wait();
	}

	/// <summary>
	/// Creates a logger with the specified category name.
	/// </summary>
	/// <param name="categoryName">The category name for the logger.</param>
	/// <returns>An instance of <see cref="ILogger"/>.</returns>
	public ILogger CreateLogger(string categoryName)
	{
		AssertIsAlive();
		return new FileLogger(categoryName, _options.MinLogLevel, WriteLog, _startTime);
	}

	private void WriteLog(PreparedLogEntry entry)
	{
		// Use the buffered writer to handle the log entry
		_bufferedWriter.Write(in entry, _fileWriter);
	}

	private void WriteLogEntry(PreparedLogEntry entry, TextWriter writer)
	{
		// The ignored parameter is provided by the BufferedLogWriter but we use _fileWriter instead
		try
		{
			lock (_lock)
			{
				// Check if we need to roll the log file (size-based rolling)
				CheckAndRollFile();

				// Use the template writer to write the log entry to the file writer
				_writer.Write(in entry, writer);
			}
		}
		catch (Exception)
		{
			// Swallow exceptions during writing
#if DEBUG
			throw;
#endif
		}
	}

	private string GetFormattedFilePath()
	{
		var fileName = PlaceholderRegex.Replace(_options.FileNamePattern, match =>
		{
			var token = match.Groups[1].Value;
			if (token.StartsWith("Timestamp", StringComparison.OrdinalIgnoreCase))
			{
				string format = "yyyyMMdd";
				var colonIndex = token.IndexOf(':', StringComparison.Ordinal);
				if (colonIndex >= 0 && colonIndex < token.Length - 1)
				{
					format = token.Substring(colonIndex + 1);
				}

				var timestamp = _options.UseUtcTimestamp ? DateTime.UtcNow : DateTime.Now;
				return timestamp.ToString(format, CultureInfo.InvariantCulture);
			}
			else if (token.Equals("ProcessId", StringComparison.OrdinalIgnoreCase))
			{
				return Environment.ProcessId.ToString(CultureInfo.InvariantCulture);
			}

			return match.Value; // Keep original if no match
		});

		return Path.Combine(_options.LogDirectory, fileName);
	}

	private void CheckAndRollFile()
	{
		if (_options.RollSizeKb <= 0)
			return;  // Size-based rolling not enabled

		try
		{
			// Get the current file size
			var fileInfo = new FileInfo(FilePath);
			if (!fileInfo.Exists || fileInfo.Length < _options.RollSizeKb * 1024)
				return;

			// Need to roll the file
			// Close the current file
			_fileWriter.Flush();
			_fileWriter.Close();
			_fileWriter.Dispose();

			// Create a new file name
			var newLogFilePath = GetFormattedFilePath();
			if (newLogFilePath == FilePath)
			{
				// If the pattern doesn't result in a different file name,
				// append a timestamp to make it unique
				var directory = Path.GetDirectoryName(FilePath) ?? string.Empty;
				var fileNameWithoutExt = Path.GetFileNameWithoutExtension(FilePath);
				var extension = Path.GetExtension(FilePath);
				var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
				newLogFilePath = Path.Combine(directory, $"{fileNameWithoutExt}_{timestamp}{extension}");
			}

			// Move the old file to the new name
			if (File.Exists(FilePath))
			{
				File.Move(FilePath, newLogFilePath);
			}

			// Recreate the file writer with the original path
			_fileWriter = File.AppendText(FilePath);

			// Apply retention policy after rolling
			ApplyRetentionPolicy();
		}
		catch
		{
			// Swallow exceptions during file rolling
#if DEBUG
			throw;
#endif
		}
	}

	private void ApplyRetentionPolicy()
	{
		if (_options.MaxRetainedFiles <= 0)
			return;  // Retention policy not enabled

		try
		{
			var directory = new DirectoryInfo(_options.LogDirectory);
			if (!directory.Exists)
				return;

			// Get the file name pattern as a wildcard
			var wildcardPattern = PlaceholderRegex.Replace(_options.FileNamePattern, match => "*");

			// Get all log files matching the wildcard pattern
			var logFiles = directory
				.GetFiles(wildcardPattern)
				.OrderByDescending(f => f.LastWriteTime)
				.Skip(_options.MaxRetainedFiles)
				.ToArray();

			// Delete older files
			foreach (var file in logFiles)
			{
				try
				{
					file.Delete();
				}
				catch
				{
					// Ignore failures to delete individual files
				}
			}
		}
		catch
		{
			// Swallow exceptions during retention policy application
#if DEBUG
			throw;
#endif
		}
	}

	/// <summary>
	/// Implementation of <see cref="ILogger"/> that writes logs to a file.
	/// </summary>
	private sealed class FileLogger(
		string category,
		LogLevel minLogLevel,
		Action<PreparedLogEntry> handler,
		DateTimeOffset startTime)
		: PreparedLoggerBase(category, minLogLevel, new LoggerExternalScopeProvider(), startTime)
	{
		/// <inheritdoc/>
		protected override void WriteLog(PreparedLogEntry entry)
			=> handler(entry);
	}
}