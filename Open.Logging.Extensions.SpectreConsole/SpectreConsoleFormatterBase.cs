using Spectre.Console;

namespace Open.Logging.Extensions.SpectreConsole;

/// <summary>
/// A formatter that outputs log entries to the console using Spectre.Console for enhanced visual styling.
/// </summary>
/// <param name="theme">The theme to use for console output styling. If null, uses <see cref="SpectreConsoleLogTheme.Default"/>.</param>
/// <param name="labels">The labels to use for different log levels. If null, uses <see cref="Defaults.LevelLabels"/>.</param>
/// <param name="newLine">Whether to add a new line after each log entry. Defaults to <see langword="false"/>.</param>
/// <param name="writer">The console writer to use. If null, uses <see cref="AnsiConsole.Console"/>.</param>
public abstract class SpectreConsoleFormatterBase(
	SpectreConsoleLogTheme? theme = null,
	LogLevelLabels? labels = null,
	bool newLine = false,
	IAnsiConsole? writer = null)
	: ISpectreConsoleFormatter
{
	/// <summary>
	/// The theme to use for console output styling. If null, uses <see cref="SpectreConsoleLogTheme.Default"/>.
	/// </summary>
	protected SpectreConsoleLogTheme Theme { get; } = theme ?? SpectreConsoleLogTheme.Default;

	/// <summary>
	/// The labels to use for different log levels. If null, uses <see cref="Defaults.LevelLabels"/>.
	/// </summary>
	protected LogLevelLabels Labels { get; } = labels ?? Defaults.LevelLabels;

	/// <summary>
	/// The console writer to use. If null, uses <see cref="AnsiConsole.Console"/>.
	/// </summary>
	protected IAnsiConsole Writer { get; } = writer ?? AnsiConsole.Console;

	/// <summary>
	/// Gets a value indicating whether a new line should be added after the current operation.
	/// </summary>
	protected bool NewLine { get; } = newLine;

	/// <summary>
	/// Creates a new console formatter with the specified name and timestamp.
	/// </summary>
	public ConsoleDelegateFormatter GetConsoleFormatter(
		string name, DateTimeOffset? timestamp = null)
		=> new(name, Write, timestamp);

	/// <inheritdoc />
	public abstract void Write(PreparedLogEntry entry);

	/// <remarks>Uses a lock on the writer to ensure only one log entry at a time.</remarks>
	/// <inheritdoc cref="Write" />
	public void WriteSynchronized(PreparedLogEntry entry)
	{
		// By using the injected writer, other locks can be applied to the same writer.
		lock (Writer)
		{
			Write(entry);
		}
	}
}
