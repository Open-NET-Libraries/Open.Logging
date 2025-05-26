namespace Open.Logging.Extensions.Writers;

/// <summary>
/// Base implementation of ITextLogEntryWriter that handles common writer functionality.
/// </summary>
public abstract class TextLogEntryWriterBase(DateTimeOffset? startTime)
	: LogEntryWriterBase<TextWriter>(startTime)
	, ITextLogEntryWriter
{
}
