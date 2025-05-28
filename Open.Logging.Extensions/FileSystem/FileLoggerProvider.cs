using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Open.Disposable;
using Open.Logging.Extensions.Writers;

namespace Open.Logging.Extensions.FileSystem;

/// <summary>
/// A file logger provider that writes logs to files with support for rolling based on log entry count.
/// </summary>
[ProviderAlias("File")]
public sealed class FileLoggerProvider : FileLoggerProviderBase
{
	StreamManager _streamManager;

	readonly Func<StreamWriter> _swFactory;
	readonly int _maxLogEntries; // Default buffer size for the stream manager

	/// <summary>
	/// Initializes a new instance of the <see cref="FileLoggerProvider"/> class with options from the DI container.
	/// </summary>
	/// <param name="options">The options for configuring the file logger.</param>
	public FileLoggerProvider(
		FileLoggerOptions options) : base(options)
	{
		// Ensure the log directory exists
		if (!Directory.Exists(LogDirectory))
			Directory.CreateDirectory(LogDirectory);

		_maxLogEntries = options.MaxLogEntries;
		var logDirectory = options.LogDirectory;
		var pattern = options.FileNamePattern;
		var useUtc = options.UseUtcTimestamp;
		var encoding = options.Encoding;
		var fileBufferSize = options.FileBufferSize;

		_swFactory = () => new(FileLoggerOptions.GetFormattedFilePath(logDirectory, pattern, useUtc), true, encoding, fileBufferSize);
		_streamManager = new StreamManager(Formatter, _swFactory);
	}

	/// <inheritdoc cref="FileLoggerProvider(FileLoggerOptions)"/>
	public FileLoggerProvider(
		IOptionsSnapshot<FileLoggerOptions> options)
		: this(options?.Value ?? new FileLoggerOptions()) { }

	/// <inheritdoc />
	protected override async ValueTask OnDisposeAsync()
	{
		// The following will only be called once because of how AsyncDisposableBase works.
		await base.OnDisposeAsync().ConfigureAwait(false);
		await _streamManager.DisposeAsync().ConfigureAwait(false);
	}

	/// <inheritdoc />
	/// <remarks>
	/// This is only called one at a time.
	/// </remarks>
	protected override async ValueTask WriteLogEntryAsync(PreparedLogEntry entry)
	{
		_streamManager.Write(in entry);
		if (_streamManager.Count < _maxLogEntries) return;
		await using var _ = _streamManager.ConfigureAwait(false);
		_streamManager = new StreamManager(Formatter, _swFactory);
	}

	/// <inheritdoc />
	protected override async ValueTask OnBufferEmptied()
	{
		// Are we in the middle of disposing or have we already disposed?
		if (WasDisposed) return;
		// Flush the stream writer to ensure all buffered data is written to the file
		await _streamManager.FlushAsync().ConfigureAwait(false);
	}

	private sealed class StreamManager(
		TemplateTextLogEntryWriter formatter,
		Func<StreamWriter> swFactory) : AsyncDisposableBase
	{
		readonly Lazy<StreamWriter> _streamWriter = new(swFactory);

		public int Count { get; private set; }

		public void Write(in PreparedLogEntry entry)
		{
			formatter.Write(in entry, _streamWriter.Value);
			Count++;
		}

		public async ValueTask FlushAsync()
		{
			// Are we in the middle of disposing or have we already disposed?
			if (WasDisposed) return;
			if (!_streamWriter.IsValueCreated) return;
			// Flush the stream writer to ensure all buffered data is written to the file
			await _streamWriter.Value.FlushAsync().ConfigureAwait(false);
		}

		protected override async ValueTask OnDisposeAsync()
		{
			if (!_streamWriter.IsValueCreated) return;
			await _streamWriter.Value.DisposeAsync().ConfigureAwait(false);
		}
	}
}
