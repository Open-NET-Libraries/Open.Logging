using System.Text;
using System.Text.RegularExpressions;

namespace Open.Logging.Extensions.FileSystem;

/// <summary>
/// Options for configuring the file logger.
/// </summary>
public partial record FileLoggerOptions : TemplateFormatterOptions
{
	/// <summary>
	/// Gets or sets the directory where log files will be stored.
	/// </summary>
	/// <remarks>
	/// If the directory does not exist, it will be created when the logger starts.
	/// </remarks>
	public string LogDirectory { get; set; } = Path.Combine(Path.GetTempPath(), "Logs");

	/// <summary>
	/// Gets or sets the file name pattern for log files.
	/// </summary>
	/// <remarks>
	/// The pattern can include the following placeholders:
	/// {Timestamp} - The current timestamp (can be formatted using standard date format strings: {Timestamp:yyyyMMdd})
	/// {ProcessId} - The current process ID
	/// </remarks>
	public string FileNamePattern { get; set; } = "log_{Timestamp:yyyyMMdd}.log";

	/// <summary>
	/// Gets or sets a value indicating whether to use UTC time for file naming patterns.
	/// </summary>
	public bool UseUtcTimestamp { get; set; }

	/// <summary>
	/// Gets or sets the size of the buffer for log message processing.
	/// </summary>
	/// <remarks>
	/// Larger buffers can improve performance but use more memory.
	/// </remarks>
	public int BufferSize { get; set; } = 10000;

	/// <summary>
	/// The size of the file buffer used when writing log messages to files.
	/// </summary>
	public int FileBufferSize { get; set; } = 4096;

	/// <summary>
	/// Gets or sets the maximum number of log entries to write to a file before rolling to a new file.
	/// </summary>
	/// <remarks>
	/// Leave this at 0 or negative to disable entry-based rolling.
	/// </remarks>
	public int MaxLogEntries { get; set; }

	/// <summary>
	/// The encoding used for log files.
	/// </summary>
	public Encoding Encoding { get; set; } = Encoding.UTF8;

	/// <summary>
	/// Gets or sets a value indicating whether the log message processing thread 
	/// can participate in message processing.
	/// </summary>
	public bool AllowSynchronousContinuations { get; set; }

	/// <summary>
	/// Throws an exception if the options are invalid.
	/// </summary>
	/// <exception cref="ArgumentException">One of the options is not valid.</exception>
	public FileLoggerOptions Validate()
	{
		if (string.IsNullOrWhiteSpace(LogDirectory))
			throw new ArgumentException("LogDirectory cannot be blank.", nameof(LogDirectory));
		if (string.IsNullOrWhiteSpace(FileNamePattern))
			throw new ArgumentException("FileNamePattern cannot be blank.", nameof(FileNamePattern));

		return this;
	}

	/// <summary>
	/// Gets the formatted file path based on the file name pattern.
	/// </summary>
	/// <returns>The full path to the log file.</returns>
	public static string GetFormattedFilePath(
		string logDirectory,
		string fileNamePattern,
		bool useUtcTimestamp = false)
	{
		var fileName = FileNamePlaceholderPattern().Replace(fileNamePattern, match =>
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

				var timestamp = useUtcTimestamp ? DateTime.UtcNow : DateTime.Now;
				return timestamp.ToString(format, CultureInfo.InvariantCulture);
			}
			else if (token.Equals("ProcessId", StringComparison.OrdinalIgnoreCase))
			{
				return Environment.ProcessId.ToString(CultureInfo.InvariantCulture);
			}

			return match.Value; // Keep original if no match
		});

		return Path.Combine(logDirectory, fileName);
	}

	/// <inheritdoc cref="GetFormattedFilePath(string, string, bool) "/>
	public string GetFormattedFilePath()
		=> GetFormattedFilePath(LogDirectory, FileNamePattern, UseUtcTimestamp);

	[GeneratedRegex(@"\{([^}]+)\}", RegexOptions.Compiled)]
	private static partial Regex FileNamePlaceholderPattern();
}