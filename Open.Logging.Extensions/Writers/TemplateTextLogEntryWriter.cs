namespace Open.Logging.Extensions.Writers;

/// <summary>
/// A writer that formats log entries using a template string.
/// </summary>
public class TemplateTextLogEntryWriter(
	TemplateFormatterOptions options)
	: TextLogEntryWriterBase(options?.StartTime)
{
	private readonly TemplateFormatterOptions _options = options with { ScopesSeparator = options.ScopesSeparator ?? "" };
	private readonly LogLevelLabels _levelLabels = options.LevelLabels ?? LogLevelLabels.Default;

	/// <summary>
	/// Writes a prepared log entry to the TextWriter using the template.
	/// </summary>
	public override void Write(in PreparedLogEntry entry, TextWriter writer)
	{
		if (writer is null)
			throw new ArgumentNullException(nameof(writer));

		var output = string.Format(
			CultureInfo.InvariantCulture,
			_options.TemplateFormatString,
			Environment.NewLine,
			entry.Timestamp,
			entry.Elapsed,
			entry.Category,
			options.FormatScopes(entry.Scopes),
			_levelLabels.GetLabelForLevel(entry.Level),
			entry.Message,
			entry.Exception?.ToLogString(entry.Category) ?? string.Empty)
			.AsSpan()
			.TrimEnd(" \r\n");

		if (output.Length == 0) return; // Nothing to log? Rare (overly simple) format strings might produce this occasionally.
		writer.WriteLine(output);

		var separator = _options.EntrySeparator;
		if (string.IsNullOrEmpty(_options.EntrySeparator)) return;
		if (separator == Environment.NewLine)
		{
			writer.WriteLine();
			return;
		}

		writer.WriteLine(separator.AsSpan().TrimEnd());
	}
}
