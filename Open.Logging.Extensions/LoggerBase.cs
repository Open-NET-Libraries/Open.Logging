using Microsoft.Extensions.Logging;

namespace Open.Logging.Extensions;

/// <summary>
/// A base class for logging that handles log-levels.
/// </summary>
public abstract class LoggerBase(
	string? category,
	LogLevel minLogLevel,
	IExternalScopeProvider? scopeProvider) : ILogger
{
	/// <summary>
	/// The category of the logger.
	/// </summary>
	public string Category { get; } = category ?? string.Empty;

	private readonly IExternalScopeProvider? _scopeProvider = scopeProvider;

	/// <inheritdoc />
	public virtual IDisposable? BeginScope<TState>(TState state)
		where TState : notnull
		=> _scopeProvider?.Push(state!);

	/// <summary>
	/// Captures the current scope for logging.
	/// </summary>
	protected IReadOnlyList<object> CaptureScope()
		=> _scopeProvider.CaptureScope();

	/// <inheritdoc />
	public virtual bool IsEnabled(LogLevel logLevel)
		=> logLevel >= minLogLevel;

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

		WriteLog(logLevel, eventId, state, exception, formatter);
	}

	/// <inheritdoc />
	protected abstract void WriteLog<TState>(
		LogLevel logLevel,
		EventId eventId,
		TState state,
		Exception? exception,
		Func<TState, Exception?, string> formatter);
}
