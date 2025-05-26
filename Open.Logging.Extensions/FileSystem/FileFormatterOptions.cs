namespace Open.Logging.Extensions.FileSystem;

/// <summary>
/// Options for configuring the file logger.
/// </summary>
public record FileFormatterOptions : TemplateFormatterOptions
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
	/// Gets or sets the maximum number of files to retain when using rolling logs.
	/// </summary>
	/// <remarks>
	/// Set to 0 or negative to disable file retention policy.
	/// </remarks>
	public int MaxRetainedFiles { get; set; }

	/// <summary>
	/// Gets or sets the size in bytes at which to roll to a new log file.
	/// </summary>
	/// <remarks>
	/// Set to 0 or negative to disable size-based rolling.
	/// </remarks>
	public long RollSizeKb { get; set; }

	/// <summary>
	/// Gets or sets the size of the buffer for log message processing.
	/// </summary>
	/// <remarks>
	/// Larger buffers can improve performance but use more memory.
	/// </remarks>
	public int BufferSize { get; set; } = 10000;

	/// <summary>
	/// Gets or sets a value indicating whether the log message processing thread 
	/// can participate in message processing.
	/// </summary>
	public bool AllowSynchronousContinuations { get; set; }
}