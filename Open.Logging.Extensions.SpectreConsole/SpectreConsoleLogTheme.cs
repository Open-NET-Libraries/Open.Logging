using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Collections.Concurrent;

namespace Open.Logging.Extensions.SpectreConsole;

/// <summary>
/// Defines color theme for SpectreConsoleLogger
/// </summary>
public record SpectreConsoleLogTheme
{
	/// <summary>
	/// Style for Trace level logs
	/// </summary>
	public Style Trace { get; init; } = "dim silver";

	/// <summary>
	/// Style for Debug level logs
	/// </summary>
	public Style Debug { get; init; } = "dim blue";

	/// <summary>
	/// Style for Information level logs
	/// </summary>
	public Style Information { get; init; } = "cyan";

	/// <summary>
	/// Style for Warning level logs
	/// </summary>
	public Style Warning { get; init; } = "bold gold1";

	/// <summary>
	/// Style for Error level logs
	/// </summary>
	public Style Error { get; init; } = "bold red3";

	/// <summary>
	/// Style for Critical level logs
	/// </summary>
	public Style Critical { get; init; } = "bold gray on red3";

	/// <summary>
	/// Style for timestamp
	/// </summary>
	public Style Timestamp { get; init; } = "dim gray";

	/// <summary>
	/// Style for category name
	/// </summary>
	public Style Category { get; init; } = "gray italic";

	/// <summary>
	/// Style for scope information
	/// </summary>
	public Style Scopes { get; init; } = "dim green";

	/// <summary>
	/// Style for Message details
	/// </summary>
	public Style Message { get; init; } = Style.Plain;

	/// <summary>
	/// Style for Exception details
	/// </summary>
	public Style Exception { get; init; } = "gray";

	/// <summary>
	/// Get the color for a specific log level
	/// </summary>
	/// <param name="logLevel">The log level</param>
	/// <returns>The appropriate color for the log level</returns>
	public Style GetStyleForLevel(LogLevel logLevel) => logLevel switch
	{
		LogLevel.Trace => Trace,
		LogLevel.Debug => Debug,
		LogLevel.Information => Information,
		LogLevel.Warning => Warning,
		LogLevel.Error => Error,
		LogLevel.Critical => Critical,
		_ => Debug,
	};

	private readonly ConcurrentDictionary<string, Text> _labelStyles = new();

	public Text GetTextForLevel(LogLevel logLevel, LogLevelLabels labels)
		=> _labelStyles.GetOrAdd(labels.GetLabelForLevel(logLevel), k => new(k, GetStyleForLevel(logLevel)));

	public static readonly SpectreConsoleLogTheme Default = new();
}
