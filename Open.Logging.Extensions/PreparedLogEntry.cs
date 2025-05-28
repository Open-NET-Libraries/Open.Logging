using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Open.Logging.Extensions;

/// <summary>
/// A prepared log entry that is ready to be formatted and written to a log target.
/// This is the canonical representation of a log entry that contains all necessary information for formatting, processing, and structured logging.
/// </summary>
public readonly record struct PreparedLogEntry
{
	/// <summary>
	/// Creates a new instance of the <see cref="PreparedLogEntry"/> struct.
	/// </summary>
	public PreparedLogEntry() { }

	/// <summary>
	/// The event ID for the log entry.
	/// </summary>
	public EventId EventId { get; init; } = default;

	/// <summary>
	/// The time to potentially measure elapsed time from.
	/// </summary>
	public required DateTimeOffset StartTime { get; init; }

	/// <summary>
	/// The timestamp value for the log entry
	/// </summary>
	public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

	/// <summary>
	/// The log level for the log entry
	/// </summary>
	public required LogLevel Level { get; init; }

	/// <summary>
	/// The category for the log entry
	/// </summary>
	public string Category { get; init; } = string.Empty;

	/// <summary>
	/// The scope for the log entry
	/// </summary>
	public IReadOnlyList<object> Scopes { get; init; } = [];

	/// <summary>
	/// The message for the log entry
	/// </summary>
	public string Message { get; init; } = string.Empty;
	/// <summary>
	/// The exception details for the log entry, if any
	/// </summary>
	public Exception? Exception { get; init; }
	
	/// <summary>
	/// Gets the amount of time that has elapsed since the recorded <see cref="Timestamp"/> and the <see cref="StartTime"/>.
	/// </summary>
	/// <returns></returns>
	public TimeSpan Elapsed
		=> Timestamp - StartTime;

	/// <summary>
	/// An indication of whether this log entry is has any content that can be written.
	/// </summary>
	public bool HasContent
		=> !string.IsNullOrWhiteSpace(Message) || Exception is not null;

	/// <summary>
	/// Creates a <see cref="PreparedLogEntry"/> from a <see cref="LogEntry{TState}"/>.
	/// </summary>
	public static PreparedLogEntry From<TState>(LogEntry<TState> logEntry, DateTimeOffset startTime, IExternalScopeProvider? scopeProvider = null) => new()
	{
		StartTime = startTime,
		Timestamp = DateTimeOffset.Now,
		Level = logEntry.LogLevel,
		Category = logEntry.Category,
		EventId = logEntry.EventId,
		Scopes = scopeProvider.CaptureScope(),
		Message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception) ?? string.Empty,
		Exception = logEntry.Exception
	};
}
