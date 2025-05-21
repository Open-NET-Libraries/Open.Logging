using Microsoft.Extensions.Logging;

namespace Open.Logging.Extensions;

/// <summary>
/// A base class for logging that handles log-levels.
/// </summary>
public abstract class LoggerBase : ILogger
{
	/// <inheritdoc />
	public abstract IDisposable? BeginScope<TState>(TState state)
		where TState : notnull;

	/// <inheritdoc />
	public abstract bool IsEnabled(LogLevel logLevel);

	/// <inheritdoc />
	public void Log<TState>(
		LogLevel logLevel,
		EventId eventId,
		TState state,
		Exception? exception,
		Func<TState, Exception?, string> formatter)
	{
		if (!IsEnabled(logLevel) || formatter is null)
			return;

		WriteLog( logLevel, eventId, state, exception, formatter);
	}

	/// <inheritdoc />
	protected abstract void WriteLog<TState>(
		LogLevel logLevel,
		EventId eventId,
		TState state,
		Exception? exception,
		Func<TState, Exception?, string> formatter);
}
