using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Open.Logging.Extensions.SpectreConsole;

public class SimpleSpectreConsoleLogger(
	LogLevel level = Default.LogLevel,
	string? category = null,
	DateTimeOffset? timestamp = null,
	LogLevelLabels? labels = null,
	SpectreConsoleLogTheme? theme = null,
	IAnsiConsole? console = null,
	bool scoped = true)
	: ScopedLoggerBase(level, category, timestamp ?? DateTimeOffset.Now, scoped ? new LoggerExternalScopeProvider() : null)
{
	private readonly SimpleSpectreConsoleFormatter formatter
		= new(theme, labels, console);

	/// <inheritdoc />
	protected override void WriteLog(PreparedLogEntry entry)
		=> formatter.WriteSynchronized(entry);
}
