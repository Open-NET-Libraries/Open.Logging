using Microsoft.Extensions.Logging;

namespace Open.Logging.Extensions.Console;

/// <summary>
/// Utility class for creating a logger that uses a delegate to handle log entries.
/// </summary>
public class ConsoleDelegateLogger(
	Action<PreparedLogEntry> handler,
	LogLevel level = Defaults.LogLevel,
	string? category = null,
	DateTimeOffset? timestamp = null,
	IExternalScopeProvider? scopeProvider = null)
	: PreparedLoggerBase(category, level, scopeProvider, timestamp ?? DateTimeOffset.Now)
{
	private readonly Action<PreparedLogEntry> _handler
		= handler ?? throw new ArgumentNullException(nameof(handler));

	/// <inheritdoc />
	protected override void WriteLog(PreparedLogEntry entry)
		=> _handler(entry);
}
