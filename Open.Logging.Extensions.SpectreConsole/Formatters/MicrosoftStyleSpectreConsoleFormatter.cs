using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Open.Logging.Extensions.SpectreConsole.Formatters;

/// <summary>
/// A formatter that outputs log entries in the style of Microsoft's default console logger with Spectre.Console styling.
/// </summary>
/// <inheritdoc />
public sealed class MicrosoftStyleSpectreConsoleFormatter(
	SpectreConsoleLogTheme? theme = null,
	LogLevelLabels? labels = null,
	bool newLine = false,
	IAnsiConsole? writer = null)
	: SpectreConsoleFormatterBase(theme, labels, newLine, writer)
	, ISpectreConsoleFormatter<MicrosoftStyleSpectreConsoleFormatter>
{
	/// <inheritdoc />
	public static MicrosoftStyleSpectreConsoleFormatter Create(
		SpectreConsoleLogTheme? theme = null,
		LogLevelLabels? labels = null,
		bool newLine = false,
		IAnsiConsole? writer = null)
		=> new(theme, labels, newLine, writer);
	/// <inheritdoc />
	public override void Write(PreparedLogEntry entry)
	{
		// Format timestamp as full DateTime similar to Microsoft default
		var timestamp = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);

		// First line: timestamp and log level
		Writer.Write(new Text(timestamp, Theme.Timestamp));
		Writer.Write(" ");
		Writer.Write(Theme.GetTextForLevel(entry.Level, Labels));
		Writer.WriteLine();

		// Second line: category with optional scope
		Writer.Write("      ");
		if (!string.IsNullOrWhiteSpace(entry.Category))
			Writer.WriteStyled(entry.Category, Theme.Category, true);

		// Add the scope information if it exists.
		if (entry.Scopes.Count > 0)
		{
			var style = Theme.Scopes;
			Writer.Write(" => ");
			for (var i = 0; i < entry.Scopes.Count; i++)
			{
				if (i > 0)
					Writer.WriteStyled(" > ", style);

				Writer.WriteStyled(entry.Scopes[i].ToString(), style);
			}
		}

		Writer.WriteLine();

		// Third line: message
		if (!string.IsNullOrWhiteSpace(entry.Message))
		{
			Writer.Write("      ");
			Writer.WriteStyled(entry.Message, Theme.Message);
			Writer.WriteLine();
		}

		if (entry.Exception is not null)
		{
			// Exception details in a highlighted box
			var panel = new Panel(new ExceptionDisplay(entry.Exception))
			{
				Border = BoxBorder.Rounded,
				BorderStyle = Theme.GetStyleForLevel(LogLevel.Error)
			};

			Writer.Write(panel);
		}

		if (NewLine)
			Writer.WriteLine();
	}
}
