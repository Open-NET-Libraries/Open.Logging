using Open.Logging.Extensions.Writers;

namespace Open.Logging.Extensions.Console;

/// <summary>
/// A console formatter that uses templates with property placeholders to format log messages.
/// </summary>
/// <remarks>This formatter allows for flexible log formatting using template patterns like 
/// {Timestamp}, {Level}, {Message}, etc.</remarks>
public class ConsoleTemplateFormatter(
	TemplateFormatterOptions options, string? name = null)
	: ConsoleFormatterBase(name ?? GetName(options), options?.StartTime)
{
	private static string GetName(TemplateFormatterOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);
		return $"[{nameof(ConsoleTemplateFormatter)}] {options?.Template}";
	}

	// Use the new template writer for formatting
	private readonly TemplateTextLogEntryWriter _writer = new(options);

	/// <inheritdoc />
	public override void Write(in PreparedLogEntry entry, TextWriter writer)
		=> _writer.Write(in entry, writer);
}
