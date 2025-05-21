using Microsoft.Extensions.Logging;

namespace Open.Logging.Extensions;

/// <summary>
/// A base class for logging that handles log-levels and scopes.
/// </summary>
public abstract class ScopedLoggerBase(
	LogLevel level,
	string? category,
	DateTimeOffset startTime,
	IExternalScopeProvider? scopeProvider)
	: LoggerBase
{
	private readonly string _category = category ?? string.Empty;

	private readonly IExternalScopeProvider? _scopeProvider = scopeProvider;

	/// <inheritdoc />
	public override IDisposable? BeginScope<TState>(TState state)
		=> _scopeProvider?.Push(state!);

	/// <inheritdoc />
	public override bool IsEnabled(LogLevel logLevel)
		=> logLevel >= level;

	/// <inheritdoc />
	protected override void WriteLog<TState>(
		LogLevel logLevel,
		EventId eventId,
		TState state,
		Exception? exception,
		Func<TState, Exception?, string> formatter)
	{
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
				Scopes = _scopeProvider.CaptureScope(),
				Category = _category
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
