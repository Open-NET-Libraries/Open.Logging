using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace Open.Logging.Extensions;

/// <summary>
/// A console formatter that uses a custom log handler to format and write log entries to a <see cref="TextWriter"/>.
/// </summary>
/// <remarks>This formatter allows for flexible log formatting by delegating the formatting logic to a
/// user-provided handler. The handler is invoked for each log entry, enabling custom templates or formatting logic to
/// be applied.</remarks>
/// <inheritdoc cref="ConsoleDelegateFormatter(string, Action{PreparedLogEntry}, DateTimeOffset?)"/>
public class ConsoleDelegateFormatter(
	string name,
	Action<TextWriter, PreparedLogEntry> logHandler,
	DateTimeOffset? timestamp = null)
	: ConsoleFormatter(name)
{
	/// <summary>
	/// Constructs a new instance of the <see cref="ConsoleDelegateFormatter"/> class.
	/// </summary>
	/// <param name="name">The name of the formatter. This is used to identify the formatter in logging configuration.</param>
	/// <param name="logHandler">The action that handles the formatting and writing of log entries.</param>
	/// <param name="timestamp">The beginning timestamp for the logs. If not provided, the current time is used.</param>
	public ConsoleDelegateFormatter(
		string name,
		Action<PreparedLogEntry> logHandler,
		DateTimeOffset? timestamp = null)
		: this(name, logHandler is null ? null! : (_, e) => logHandler(e), timestamp) { }

	private readonly DateTimeOffset _timestamp
		= timestamp ?? DateTimeOffset.Now;

	private readonly Action<TextWriter, PreparedLogEntry> _logHandler
		= logHandler ?? throw new ArgumentNullException(nameof(logHandler));

	/// <inheritdoc />
	public override void Write<TState>(
		in LogEntry<TState> logEntry,
		IExternalScopeProvider? scopeProvider,
		TextWriter textWriter)
	{
		string message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception) ?? "";

		if (message.AsSpan().Trim().Length == 0 && logEntry.Exception is null)
			return;

		_logHandler(textWriter, new PreparedLogEntry
		{
			EventId = logEntry.EventId,
			StartTime = _timestamp,
			Level = logEntry.LogLevel,
			Category = logEntry.Category,
			Scopes = scopeProvider.CaptureScope(),
			Message = message,
			Exception = logEntry.Exception,
		});
	}
}
