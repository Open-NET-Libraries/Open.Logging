using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Open.Logging.Extensions.Writers;

/// <summary>
/// Base implementation for generic writer types.
/// </summary>
/// <typeparam name="TWriter">The type of writer to use.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="TextLogEntryWriterBase"/> class.
/// </remarks>
public abstract class LogEntryWriterBase<TWriter>(DateTimeOffset? startTime)
	: ILogEntryWriter<TWriter>
	, IPreparedLogEntryWriter<TWriter>
{
	/// <summary>
	/// Gets the start time for this writer instance.
	/// </summary>
	protected DateTimeOffset StartTime { get; }
		= startTime ?? DateTimeOffset.Now;

	/// <summary>
	/// Writes a log entry to the provided writer.
	/// </summary>
	/// <typeparam name="TState">The type of the state object.</typeparam>
	/// <param name="logEntry">The log entry to write.</param>
	/// <param name="scopeProvider">An optional provider for log scopes.</param>
	/// <param name="writer">The writer to write the formatted log entry to.</param>
	public void Write<TState>(
		in LogEntry<TState> logEntry,
		IExternalScopeProvider? scopeProvider,
		TWriter writer)
	{
		if (writer == null)
			throw new ArgumentNullException(nameof(writer));

		string message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception) ?? string.Empty;

		if (message.AsSpan().Trim().Length == 0 && logEntry.Exception is null)
			return;

		// Convert to PreparedLogEntry for consistent handling
		var entry = new PreparedLogEntry
		{
			StartTime = StartTime,
			Timestamp = DateTimeOffset.Now,
			Level = logEntry.LogLevel,
			Category = logEntry.Category,
			EventId = logEntry.EventId,
			Scopes = scopeProvider.CaptureScope(),
			Message = message,
			Exception = logEntry.Exception
		};

		Write(in entry, writer);
	}

	/// <summary>
	/// Writes a prepared log entry to the provided writer.
	/// Override this method to implement custom writing logic.
	/// </summary>
	/// <param name="entry">The prepared log entry to write.</param>
	/// <param name="writer">The writer to write the formatted log entry to.</param>
	public abstract void Write(in PreparedLogEntry entry, TWriter writer);
}
