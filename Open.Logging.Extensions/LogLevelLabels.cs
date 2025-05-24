using Microsoft.Extensions.Logging;

namespace Open.Logging.Extensions;

/// <summary>
/// Defines customizable labels for log levels
/// </summary>
public record LogLevelLabels
{
	/// <summary>
	/// Label text for Trace level logs
	/// </summary>
	public string Trace { get; init; } = "TRACE";

	/// <summary>
	/// Label text for Debug level logs
	/// </summary>
	public string Debug { get; init; } = "DEBUG";

	/// <summary>
	/// Label text for Information level logs
	/// </summary>
	public string Information { get; init; } = "INFO";

	/// <summary>
	/// Label text for Warning level logs
	/// </summary>
	public string Warning { get; init; } = "WARN";

	/// <summary>
	/// Label text for Error level logs
	/// </summary>
	public string Error { get; init; } = "ERROR";

	/// <summary>
	/// Label text for Critical level logs
	/// </summary>
	public string Critical { get; init; } = "CRITICAL";

	/// <summary>
	/// The default labels for log levels
	/// </summary>
	public static LogLevelLabels Default => new();

	/// <summary>
	/// Get the label for a specific log level
	/// </summary>
	/// <param name="logLevel">The log level</param>
	/// <returns>The appropriate label for the log level</returns>
	public string GetLabelForLevel(LogLevel logLevel) => logLevel switch
	{
		LogLevel.Trace => Trace,
		LogLevel.Debug => Debug,
		LogLevel.Information => Information,
		LogLevel.Warning => Warning,
		LogLevel.Error => Error,
		LogLevel.Critical => Critical,
		_ => logLevel.ToString(),
	};
}
