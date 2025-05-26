namespace Open.Logging.Extensions.Writers;

/// <summary>
/// Defines a mechanism for writing prepared log entries to a specified writer.
/// </summary>
/// <typeparam name="TWriter">The type of the writer to which the log entry will be written.</typeparam>
public interface IPreparedLogEntryWriter<in TWriter>
	: ILogEntryWriter<TWriter>
{
	/// <summary>
	/// Writes a prepared log entry to the provided writer.
	/// </summary>
	/// <param name="entry">The prepared log entry to write.</param>
	/// <param name="writer">The writer to write the formatted log entry to.</param>
	void Write(
		in PreparedLogEntry entry,
		TWriter writer);
}
