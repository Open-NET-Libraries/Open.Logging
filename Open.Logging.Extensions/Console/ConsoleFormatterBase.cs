using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Open.Logging.Extensions.Writers;

namespace Open.Logging.Extensions.Console;

/// <summary>
/// Base class for console formatters that write log entries to a <see cref="TextWriter"/>.
/// </summary>
public abstract class ConsoleFormatterBase(string name, DateTimeOffset? startTime)
	: ConsoleFormatter(name)
	, ITextLogEntryWriter
	, IPreparedLogEntryWriter<TextWriter>
{
	private readonly DateTimeOffset _startTime
		= startTime ?? DateTimeOffset.Now;

	/// <inheritdoc />
	public override void Write<TState>(
		in LogEntry<TState> logEntry,
		IExternalScopeProvider? scopeProvider,
		TextWriter textWriter)
	{
		var entry = PreparedLogEntry.From(logEntry, _startTime, scopeProvider);
		if (entry.HasContent) Write(entry, textWriter);
	}

	/// <inheritdoc />
	public abstract void Write(in PreparedLogEntry entry, TextWriter writer);
}
