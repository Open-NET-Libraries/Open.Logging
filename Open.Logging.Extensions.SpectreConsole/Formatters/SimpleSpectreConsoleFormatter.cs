using Spectre.Console;

namespace Open.Logging.Extensions.SpectreConsole.Formatters;

/// <summary>
/// A formatter that outputs log entries to the console using Spectre.Console for enhanced visual styling.
/// </summary>
/// <param name="theme">The theme to use for console output styling. If null, uses <see cref="SpectreConsoleLogTheme.Default"/>.</param>
/// <param name="labels">The labels to use for different log levels. If null, uses <see cref="Defaults.LevelLabels"/>.</param>
/// <param name="writer">The console writer to use. If null, uses <see cref="AnsiConsole.Console"/>.</param>
public sealed class SimpleSpectreConsoleFormatter(
	SpectreConsoleLogTheme? theme = null,
	LogLevelLabels? labels = null,
	IAnsiConsole? writer = null)
	: SpectreConsoleFormatterBase(theme, labels, writer)
	, ISpectreConsoleFormatter<SimpleSpectreConsoleFormatter>
{
	/// <inheritdoc />
	public static SimpleSpectreConsoleFormatter Create(
		SpectreConsoleLogTheme? theme = null,
		LogLevelLabels? labels = null,
		IAnsiConsole? writer = null)
		=> new(theme, labels, writer);

	/// <inheritdoc />
	public override void Write(PreparedLogEntry entry)
	{
		// Timestamp/
		var elapsedSeconds = entry.Elapsed.TotalSeconds;
		Writer.Write(new Text($"{elapsedSeconds:000.000}s", Theme.Timestamp));

		// Level
		Writer.Write(" [");
		Writer.Write(Theme.GetTextForLevel(entry.Level, Labels));
		Writer.Write("]"); // Brackets [xxxx] are easier to search for in logs.

		// Add the potential category name.
		if (!string.IsNullOrWhiteSpace(entry.Category))
		{
			Writer.Write(" ");
			Writer.WriteStyled(entry.Category, Theme.Category, true);
		}

		// Add the separator between the category and the scope.
		Writer.Write(":");

		// Add the scope information if it exists.
		if (entry.Scopes.Count > 0)
		{
			var style = Theme.Scopes;
			Writer.Write(" ");
			Writer.WriteStyled("(", style);
			for (var i = 0; i < entry.Scopes.Count; i++)
			{
				if (i > 0)
					Writer.WriteStyled(" > ", style);

				Writer.WriteStyled(entry.Scopes[i].ToString(), style);
			}

			Writer.WriteStyled(")", style);
		}

		// Add the message text.
		if (!string.IsNullOrWhiteSpace(entry.Message))
		{
			Writer.Write(" ");
			Writer.WriteStyled(entry.Message, Theme.Message);
		}

		Writer.WriteLine();

		if (entry.Exception is null)
			return;

		// Add the exception details if they exist.
		var rule = new Rule() { Style = Color.Grey };
		Writer.Write(rule);
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

		Writer.Write(rule);
	}
}
