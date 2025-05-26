namespace Open.Logging.Extensions.Writers;

/// <summary>
/// Specialized interface for TextWriter-based log entry writers.
/// This is the most common implementation for text-based logging.
/// </summary>
public interface ITextLogEntryWriter
	: ILogEntryWriter<TextWriter>
{
}
