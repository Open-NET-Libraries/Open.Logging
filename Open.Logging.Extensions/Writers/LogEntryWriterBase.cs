using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Open.Logging.Extensions.Writers;

/// <summary>
/// Base implementation for generic writer types.
/// </summary>
/// <typeparam name="TContext">The type of writer to use.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="TextLogEntryWriterBase"/> class.
/// </remarks>
public abstract class LogEntryWriterBase<TContext>(DateTimeOffset? startTime)
	: ILogEntryWriter<TContext>
	, IPreparedLogEntryWriter<TContext>
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
	/// <param name="context">The context to write the formatted log entry to.</param>
	public void Write<TState>(
		in LogEntry<TState> logEntry,
		IExternalScopeProvider? scopeProvider,
		TContext context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));

		var entry = PreparedLogEntry.From(logEntry, StartTime, scopeProvider);

		Write(in entry, context);
	}

	/// <summary>
	/// Writes a prepared log entry to the provided writer.
	/// Override this method to implement custom writing logic.
	/// </summary>
	/// <param name="entry">The prepared log entry to write.</param>
	/// <param name="context">The context to write the formatted log entry to.</param>
	public abstract void Write(in PreparedLogEntry entry, TContext context);
}
