using Spectre.Console;

namespace Open.Logging.Extensions.SpectreConsole.Formatters;

/// <summary>
/// A formatter that outputs log entries in the style of Microsoft's default console logger with Spectre.Console styling.
/// </summary>
/// <inheritdoc />
public sealed class MinimalMutliLineSpectreConsoleFormatter(
	SpectreConsoleLogTheme? theme = null,
	LogLevelLabels? labels = null,
	bool newLine = false,
	IAnsiConsole? writer = null)
	: SpectreConsoleFormatterBase(theme, labels, newLine, writer)
	, ISpectreConsoleFormatter<MinimalMutliLineSpectreConsoleFormatter>
{
	/// <inheritdoc />
	public static MinimalMutliLineSpectreConsoleFormatter Create(
		SpectreConsoleLogTheme? theme = null,
		LogLevelLabels? labels = null,
		bool newLine = false,
		IAnsiConsole? writer = null)
		=> new(theme, labels, newLine, writer);

	// Add the exception details if they exist.
	private static readonly Rule HR = new() { Style = Color.Grey };

	/// <inheritdoc />
	public override void Write(PreparedLogEntry entry)
	{
		if (!NewLine)
			Writer.Write(HR);

		var timestamp = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
		Writer.Write(new Text(timestamp, Theme.Timestamp));

		var elapsedSeconds = entry.Elapsed.TotalSeconds;
		Writer.Write(new Text($" ({elapsedSeconds:0.000}s)", Theme.Timestamp));

		if (!string.IsNullOrWhiteSpace(entry.Category))
		{
			Writer.Write(" ");
			Writer.WriteStyled(entry.Category, Theme.Category, true);
		}

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
		Writer.Write("[");
		Writer.Write(Theme.GetTextForLevel(entry.Level, Labels));
		Writer.Write("]");

		if (!string.IsNullOrWhiteSpace(entry.Message))
		{
			Writer.Write(" ");
			Writer.WriteStyled(entry.Message, Theme.Message);
			Writer.WriteLine();
		}

		if (entry.Exception is not null)
		{
			try
			{
				Writer.WriteException(entry.Exception);
			}
			catch
			{
				// Fall-back if WriteException fails.  Not likely, but not a bad idea.
				Writer.WriteLine($"Exception: {entry.Exception.Message}");
				var st = entry.Exception.StackTrace;
				if (!string.IsNullOrWhiteSpace(st))
					Writer.WriteLine(st);
			}
		}

		if (NewLine)
			Writer.WriteLine();
	}
}
