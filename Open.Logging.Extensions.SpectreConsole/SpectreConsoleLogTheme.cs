using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Collections.Concurrent;

namespace Open.Logging.Extensions.SpectreConsole;

/// <summary>
/// Defines color theme for SpectreConsoleLogger
/// </summary>
public partial record SpectreConsoleLogTheme
{
	/// <summary>
	/// Style for Trace level logs
	/// </summary>
	public Style Trace { get; init; } = new Style(Color.Grey);

	/// <summary>
	/// Style for Debug level logs
	/// </summary>
	public Style Debug { get; init; } = new Style(Color.Blue);

	/// <summary>
	/// Style for Information level logs
	/// </summary>
	public Style Information { get; init; } = Color.Green;

	/// <summary>
	/// Style for Warning level logs
	/// </summary>
	public Style Warning { get; init; } = new Style(Color.Yellow, decoration: Decoration.Bold);

	/// <summary>
	/// Style for Error level logs
	/// </summary>
	public Style Error { get; init; } = new Style(Color.Red, decoration: Decoration.Bold);

	/// <summary>
	/// Style for Critical level logs
	/// </summary>
	public Style Critical { get; init; } = new Style(Color.White, Color.Red, Decoration.Bold);

	/// <summary>
	/// Style for timestamp
	/// </summary>
	public Style Timestamp { get; init; } = new Style(Color.Grey);

	/// <summary>
	/// Style for category name
	/// </summary>
	public Style Category { get; init; } = new Style(Color.Grey, decoration: Decoration.Italic);

	/// <summary>
	/// Style for scope information
	/// </summary>
	public Style Scopes { get; init; } = new Style(Color.Blue);

	/// <summary>
	/// Style for Message details
	/// </summary>
	public Style Message { get; init; } = Style.Plain;

	/// <summary>
	/// Style for Exception details
	/// </summary>
	public Style Exception { get; init; } = Color.Red;

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

	/// <summary>
	/// Gets a styled text representation for the specified log level using the provided labels.
	/// </summary>
	/// <param name="logLevel">The log level to get the styled text for.</param>
	/// <param name="labels">The labels to use for the log levels.</param>
	/// <returns>A styled <see cref="Text"/> object for the specified log level.</returns>
	public Text GetTextForLevel(LogLevel logLevel, LogLevelLabels labels)
	{
		ArgumentNullException.ThrowIfNull(labels);
		return _labelStyles.GetOrAdd(labels.GetLabelForLevel(logLevel), k => new(k, GetStyleForLevel(logLevel)));
	}

	/// <summary>
	/// The default theme instance.
	/// </summary>
	/// <remarks>
	/// The default theme uses standard terminal colors for maximum compatibility with all terminal types
	/// including legacy terminals with limited color support.
	/// </remarks>
	public static readonly SpectreConsoleLogTheme Default = new();
}
