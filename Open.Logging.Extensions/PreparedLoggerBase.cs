using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Open.Logging.Extensions;

/// <summary>
/// A base class for logging that handles log-levels and prepares log entries for writing.
/// </summary>
public abstract class PreparedLoggerBase(
	string? category,
	LogLevel minLogLevel,
	IExternalScopeProvider? scopeProvider,
	DateTimeOffset startTime)
	: LoggerBase(category, minLogLevel, scopeProvider)
{
	/// <inheritdoc />
	protected override void WriteLog<TState>(
		LogLevel logLevel,
		EventId eventId,
		TState state,
		Exception? exception,
		Func<TState, Exception?, string> formatter)
	{
		Debug.Assert(formatter is not null);
		var message = formatter(state, exception);
		if (string.IsNullOrWhiteSpace(message) && exception is null)
			return; // Nothing to log.

		try
		{
			WriteLog(new PreparedLogEntry
			{
				StartTime = startTime,
				Level = logLevel,
				EventId = eventId,
				Message = message,
				Exception = exception,
				Scopes = CaptureScope(),
				Category = Category
			});
		}
		catch
		{
#if DEBUG
			throw;
#else
            // Swallow exceptions in release to avoid logging failures crashing the app.
#endif
		}
	}

	/// <summary>
	/// Abstract method to be implemented by derived classes to handle the actual logging of the entry.
	/// </summary>
	protected abstract void WriteLog(PreparedLogEntry entry);
}
