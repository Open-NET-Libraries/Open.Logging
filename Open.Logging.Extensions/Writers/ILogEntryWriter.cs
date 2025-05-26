using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Open.Logging.Extensions.Writers;

/// <summary>
/// Defines a generic interface for components that write log entries to a specific writer type.
/// This provides type safety and flexibility for different output mechanisms.
/// </summary>
/// <typeparam name="TWriter">The type of writer to use.</typeparam>
public interface ILogEntryWriter<in TWriter>
{
	/// <summary>
	/// Writes a log entry to the provided writer.
	/// </summary>
	/// <typeparam name="TState">The type of the state object.</typeparam>
	/// <param name="logEntry">The log entry to write.</param>
	/// <param name="scopeProvider">An optional provider for log scopes.</param>
	/// <param name="writer">The writer to write the formatted log entry to.</param>
	void Write<TState>(
		in LogEntry<TState> logEntry,
		IExternalScopeProvider? scopeProvider,
		TWriter writer);
}