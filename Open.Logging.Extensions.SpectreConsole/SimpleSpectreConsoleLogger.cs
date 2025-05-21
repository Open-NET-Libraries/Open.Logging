using Microsoft.Extensions.Logging;
using Open.Logging.Extensions.SpectreConsole.Formatters;
using Spectre.Console;

namespace Open.Logging.Extensions.SpectreConsole;

/// <summary>
/// A logger implementation that uses Spectre.Console for enhanced console output.
/// </summary>
/// <param name="level">The minimum log level to display. Defaults to the value in <see cref="Defaults.LogLevel"/>.</param>
/// <param name="category">The optional category name for the logger.</param>
/// <param name="timestamp">The optional timestamp to use for log entries. Defaults to current time.</param>
/// <param name="labels">The optional custom labels for log levels. Defaults to <see cref="Defaults.LevelLabels"/>.</param>
/// <param name="theme">The optional custom theme for console output. Defaults to <see cref="SpectreConsoleLogTheme.Default"/>.</param>
/// <param name="newLine">A boolean that indicates whether to add a newline after each log entry.</param>
/// <param name="console">The optional <see cref="IAnsiConsole"/> instance to use for writing output. Defaults to <see cref="AnsiConsole.Console"/>.</param>
/// <param name="scoped">Whether to enable log scopes. Defaults to <see langword="true"/>.</param>
public class SimpleSpectreConsoleLogger(
	LogLevel level = Defaults.LogLevel,
	string? category = null,
	DateTimeOffset? timestamp = null,
	LogLevelLabels? labels = null,
	SpectreConsoleLogTheme? theme = null,
	bool newLine = false,
	IAnsiConsole? console = null,
	bool scoped = true)
	: ScopedLoggerBase(level, category, timestamp ?? DateTimeOffset.Now, scoped ? new LoggerExternalScopeProvider() : null)
{
	private readonly SimpleSpectreConsoleFormatter formatter
		= new(theme, labels, newLine, console);

	/// <inheritdoc />
	protected override void WriteLog(PreparedLogEntry entry)
		=> formatter.WriteSynchronized(entry);
}
