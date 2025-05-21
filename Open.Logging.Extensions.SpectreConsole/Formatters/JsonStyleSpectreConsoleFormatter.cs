using Spectre.Console;

namespace Open.Logging.Extensions.SpectreConsole.Formatters;

/// <summary>
/// A formatter that outputs log entries in a JSON-like structure with Spectre.Console styling.
/// </summary>
/// <param name="theme">The theme to use for console output styling. If null, uses <see cref="SpectreConsoleLogTheme.Default"/>.</param>
/// <param name="labels">The labels to use for different log levels. If null, uses <see cref="Defaults.LevelLabels"/>.</param>
/// <param name="writer">The console writer to use. If null, uses <see cref="AnsiConsole.Console"/>.</param>
public sealed class JsonStyleSpectreConsoleFormatter(
	SpectreConsoleLogTheme? theme = null,
	LogLevelLabels? labels = null,
	IAnsiConsole? writer = null)
	: SpectreConsoleFormatterBase(theme, labels, writer)
	, ISpectreConsoleFormatter<JsonStyleSpectreConsoleFormatter>
{
	/// <inheritdoc />
	public static JsonStyleSpectreConsoleFormatter Create(
		SpectreConsoleLogTheme? theme = null,
		LogLevelLabels? labels = null,
		IAnsiConsole? writer = null)
		=> new(theme, labels, writer);

	/// <inheritdoc />
	public override void Write(PreparedLogEntry entry)
	{
		var levelStyle = Theme.GetStyleForLevel(entry.Level);
		Writer.Write("{ ");
		// Timestamp
		Writer.Write(new Text("time", Style.Parse("dim")));
		Writer.Write(": ");
		Writer.Write(new Text($"\"{DateTime.Now.ToString("HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture)}\"", Theme.Timestamp));
		Writer.Write(", ");

		// Log level
		Writer.Write(new Text("level", Style.Parse("dim")));
		Writer.Write(": ");
		Writer.Write(new Text($"\"{Labels.GetLabelForLevel(entry.Level)}\"", levelStyle));

		// Category
		if (!string.IsNullOrWhiteSpace(entry.Category))
		{
			Writer.Write(", ");
			Writer.Write(new Text("source", Style.Parse("dim")));
			Writer.Write(": ");
			Writer.Write(new Text($"\"{entry.Category}\"", Theme.Category));
		}

		// Scopes
		if (entry.Scopes.Count > 0)
		{
			Writer.Write(", ");
			Writer.Write(new Text("scopes", Style.Parse("dim")));
			Writer.Write(": [");

			for (var i = 0; i < entry.Scopes.Count; i++)
			{
				if (i > 0) Writer.Write(", ");
				Writer.Write(new Text($"\"{entry.Scopes[i]}\"", Theme.Scopes));
			}

			Writer.Write("]");
		}

		// Message
		if (!string.IsNullOrWhiteSpace(entry.Message))
		{
			Writer.Write(", ");
			Writer.Write(new Text("message", Style.Parse("dim")));
			Writer.Write(": ");
			Writer.Write(new Text($"\"{entry.Message}\"", Theme.Message));
		}

		// Elapsed time
		var elapsedSeconds = entry.Elapsed.TotalSeconds;
		Writer.Write(", ");
		Writer.Write(new Text("elapsed", Style.Parse("dim")));
		Writer.Write(": ");
		Writer.Write(new Text($"\"{elapsedSeconds:0.000}s\"", Theme.Timestamp));

		Writer.Write(" }");
		Writer.WriteLine();

		// Exception
		if (entry.Exception is not null)
		{
			var panel = new Panel(new ExceptionDisplay(entry.Exception))
			{
				Header = new PanelHeader("Exception"),
				Border = BoxBorder.Rounded,
				BorderStyle = levelStyle,
				Expand = false
			};

			Writer.Write(panel);
		}
	}
}
