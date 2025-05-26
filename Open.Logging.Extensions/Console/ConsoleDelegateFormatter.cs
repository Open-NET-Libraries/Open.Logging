using Open.Logging.Extensions.Writers;

namespace Open.Logging.Extensions.Console;

/// <summary>
/// A console formatter that uses a custom log handler to format and write log entries to a <see cref="TextWriter"/>.
/// </summary>
/// <remarks>This formatter allows for flexible log formatting by delegating the formatting logic to a
/// user-provided handler. The handler is invoked for each log entry, enabling custom templates or formatting logic to
/// be applied.</remarks>
/// <inheritdoc cref="ConsoleDelegateFormatter(string, Action{PreparedLogEntry}, DateTimeOffset?)"/>
public class ConsoleDelegateFormatter(
	string name,
	Action<PreparedLogEntry, TextWriter> logHandler,
	DateTimeOffset? timestamp = null)
	: ConsoleFormatterBase(name, timestamp)
	, ITextLogEntryWriter
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
		: this(name, logHandler is null ? null! : (e, _) => logHandler(e), timestamp) { }

	private readonly Action<PreparedLogEntry, TextWriter> _logHandler
		= logHandler ?? throw new ArgumentNullException(nameof(logHandler));

	/// <inheritdoc />
	public override void Write(in PreparedLogEntry entry, TextWriter writer)
		=> _logHandler(entry, writer);
}
