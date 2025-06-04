using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Globalization;

namespace Open.Logging.Extensions.SpectreConsole.Formatters;

/// <summary>
/// A formatter that outputs log entries in a structured multi-line format with clear visual organization.
/// </summary>
/// <inheritdoc />
public sealed class StructuredMultilineFormatter(
	SpectreConsoleLogTheme? theme = null,
	LogLevelLabels? labels = null,
	bool newLine = false,
	IAnsiConsole? writer = null)
	: SpectreConsoleFormatterBase(theme, labels, newLine, writer)
	, ISpectreConsoleFormatter<StructuredMultilineFormatter>
{
	/// <inheritdoc />
	public static StructuredMultilineFormatter Create(
		SpectreConsoleLogTheme? theme = null,
		LogLevelLabels? labels = null,
		bool newLine = false,
		IAnsiConsole? writer = null)
		=> new(theme, labels, newLine, writer);

	/// <inheritdoc />
	protected override void WriteTimestamp(DateTimeOffset timestamp, string format = "HH:mm:ss.fff")
	{
		var text = timestamp.ToString(format, CultureInfo.InvariantCulture);
		Write(text, Theme.Timestamp);
	}

	/// <inheritdoc />
	protected override void WriteLevel(LogLevel level, Placement whiteSpace = Placement.None)
	{
		Write(" - ");
		Write(Labels.GetLabelForLevel(level), Theme.GetStyleForLevel(level));
	}

	/// <summary>
	/// Creates a header rule with timestamp and log level.
	/// </summary>
	private Rule CreateHeader(DateTimeOffset timestamp, LogLevel level)
	{
		// Simple string concatenation for the header
		var headerText = $"{timestamp.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)} - [[{Labels.GetLabelForLevel(level)}]]";

		var header = new Rule(headerText)
		{
			Style = Theme.GetStyleForLevel(level)
		};

		header.LeftJustified();
		return header;
	}

	private static readonly Style LabelStyle = Style.Parse("dim italic");

	private void WriteRow(string label, string value, Style style)
	{
		Write(new string(' ', 8 - label.Length));
		Write(label, LabelStyle);
		Write(": ");
		WriteLine(value, style);
	}

	/// <inheritdoc />
	protected override bool WriteException(Exception? exception, string? category)
	{
		if (exception is null)
			return false;

		var exceptionPanel = new Panel(new ExceptionDisplay(exception, category))
		{
			Header = new PanelHeader("Exception"),
			Border = BoxBorder.Rounded,
			BorderStyle = Theme.GetStyleForLevel(LogLevel.Error),
			Expand = true,
			Padding = new Padding(1, 0, 1, 0)
		};

		Write(exceptionPanel);
		return true;
	}

	private static readonly Rule DimHR = new()
	{
		Style = Style.Parse("dim")
	};

	/// <inheritdoc />
	public override void Write(PreparedLogEntry entry)
	{
		var levelStyle = Theme.GetStyleForLevel(entry.Level);

		// Create and render a header with timestamp and log level
		var header = CreateHeader(entry.Timestamp, entry.Level);
		Write(header);

		// Create and render a grid with details
		if (!string.IsNullOrWhiteSpace(entry.Category))
		{
			WriteRow("Category", entry.Category, Theme.Category);
		}

		// Scopes
		if (entry.Scopes.Count > 0)
		{
			var scopesText = string.Join(" → ", entry.Scopes);
			WriteRow("Scopes", scopesText, Theme.Scopes);
		}

		// Message
		if (!string.IsNullOrWhiteSpace(entry.Message))
		{
			WriteRow("Message", entry.Message, Theme.Message);
		}

		// Elapsed
		var elapsedSeconds = entry.Elapsed.TotalSeconds;
		WriteRow("Elapsed", $"{elapsedSeconds:0.000}s", Theme.Timestamp);

		// Exception
		WriteException(entry.Exception, entry.Category);
		Write(DimHR);

		if (NewLine)
			WriteLine();
	}
}
