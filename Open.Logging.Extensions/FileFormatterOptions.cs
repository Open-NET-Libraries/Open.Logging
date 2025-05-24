using Microsoft.Extensions.Logging;

namespace Open.Logging.Extensions;

/// <summary>
/// Options for configuring the file formatter.
/// </summary>
/// <remarks>
/// The file formatter provides a customizable logging solution with features including:
/// <list type="bullet">
/// <item><description>Template-based log formatting</description></item>
/// <item><description>Timestamp-based file naming</description></item>
/// <item><description>Automatic file rolling when size limits are reached</description></item>
/// <item><description>Log file retention policies to manage disk space</description></item>
/// </list>
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Style", "IDE0032:Use auto property",
	Justification = "Specialized setters.")]
public record FileFormatterOptions : ConsoleTemplateFormatterOptions
{
    /// <summary>
    /// Gets or sets the directory where log files will be stored.
    /// </summary>
    /// <remarks>
    /// The directory will be created if it doesn't exist.
    /// </remarks>
    public string LogDirectory { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "logs");

    /// <summary>
    /// Gets or sets custom labels for different log levels.
    /// </summary>
    /// <remarks>
    /// These labels are used in the log output when the {Level} placeholder is used in the template.
    /// </remarks>
    public LogLevelLabels LevelLabels { get; set; } = new()
    {
        // The following are easy to read and are detected by log formatter extensions and tools.
        Trace = "TRACE",
        Debug = "DEBUG",
        Information = "INFO",
        Warning = "WARN",
        Error = "ERROR",
        Critical = "CRITICAL"
    };

	/// <summary>
	/// Gets or sets the file name pattern for log files.
	/// </summary>
	/// <remarks>
	/// The pattern can include the following placeholders:
	/// <list type="bullet">
	/// <item><description>{Timestamp} - The timestamp when the logger was created or when rolling to a new file</description></item>
	/// </list>
	/// Examples:
	/// <list type="bullet">
	/// <item><description>"log_{Timestamp:yyyyMMdd}.log" - Creates files like "log_20250523.log"</description></item>
	/// <item><description>"app-{Timestamp:yyyy-MM-dd_HH-mm-ss}.log" - Creates files like "app-2025-05-23_14-30-00.log"</description></item>
	/// </list>
	/// </remarks>
	private string _fileNamePattern = "log_{Timestamp:yyyyMMdd_HHmmss}.log";
    
    /// <summary>
    /// Gets or sets the file name pattern for log files.
    /// </summary>
    /// <remarks>
    /// When file rolling is enabled via <see cref="MaxFileSize"/>, new files will be created using this pattern.
    /// </remarks>
    public string FileNamePattern
    {
        get => _fileNamePattern;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _fileNamePattern = value;
            
            // Convert {Timestamp} or {Timestamp:format} to {0} or {0:format}
            if (value.Contains("{Timestamp:", StringComparison.OrdinalIgnoreCase))
            {
                FileNameFormatString = value.Replace("{Timestamp:", "{0:", StringComparison.OrdinalIgnoreCase);
            }
            else if (value.Contains("{Timestamp}", StringComparison.OrdinalIgnoreCase))
            {
                // If no format is specified, add a default format
                FileNameFormatString = value.Replace("{Timestamp}", "{0:yyyyMMdd_HHmmss}", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                FileNameFormatString = value;
            }
        }
    }

    /// <summary>
    /// Gets the format string for the file name.
    /// </summary>
    /// <remarks>
    /// This is a computed property based on the FileNamePattern.
    /// </remarks>
    public string FileNameFormatString { get; private set; } = "log_{0:yyyyMMdd_HHmmss}.log";

    /// <summary>
    /// Gets or sets whether to flush the log after each write.
    /// </summary>
    /// <remarks>
    /// This option is used as a hint. The actual flushing behavior is optimized to flush
    /// when the channel is empty or at regular intervals to balance performance and durability.
    /// </remarks>
    public bool AutoFlush { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to append to an existing file (if one exists).
    /// </summary>
    /// <remarks>
    /// When set to true, log entries will be added to existing log files.
    /// When set to false, existing log files will be overwritten.
    /// </remarks>
    public bool AppendToFile { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the maximum size of a log file in bytes before rolling to a new file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When a log file reaches this size, the logger will create a new file using the timestamp
    /// at the moment of rolling. This helps prevent any single log file from growing too large.
    /// </para>
    /// <para>
    /// Set to 0 to disable size-based rolling (default).
    /// </para>
    /// <para>
    /// Common sizes:
    /// <list type="bullet">
    /// <item><description>1,048,576 bytes = 1 MB</description></item>
    /// <item><description>10,485,760 bytes = 10 MB</description></item>
    /// <item><description>104,857,600 bytes = 100 MB</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public long MaxFileSize { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum number of log files to retain.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When this limit is reached, the oldest log files (based on last write time) will be deleted.
    /// This helps manage disk space by automatically removing older logs.
    /// </para>
    /// <para>
    /// Set to 0 to keep all files (default).
    /// </para>
    /// <para>
    /// A common strategy is to combine this with <see cref="MaxFileSize"/> to maintain a rolling window
    /// of logs that stays within a specific total size limit. For example, setting MaxFileSize=10MB and
    /// MaxRetainedFiles=10 would keep approximately 100MB of logs.
    /// </para>
    /// </remarks>
    public int MaxRetainedFiles { get; set; }
}
