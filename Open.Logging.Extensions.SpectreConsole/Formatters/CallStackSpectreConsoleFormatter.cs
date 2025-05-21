using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Globalization;

namespace Open.Logging.Extensions.SpectreConsole.Formatters;

/// <summary>
/// A formatter that outputs log entries in a call-stack style with hierarchical display of information
/// using Spectre.Console styling.
/// </summary>
/// <inheritdoc />
public sealed class CallStackSpectreConsoleFormatter(
	SpectreConsoleLogTheme? theme = null,
	LogLevelLabels? labels = null,
	bool newLine = false,
	IAnsiConsole? writer = null)
	: SpectreConsoleFormatterBase(theme, labels, newLine, writer)
	, ISpectreConsoleFormatter<CallStackSpectreConsoleFormatter>
{
	/// <inheritdoc />
	public static CallStackSpectreConsoleFormatter Create(
		SpectreConsoleLogTheme? theme = null,
		LogLevelLabels? labels = null,
		bool newLine = false,
		IAnsiConsole? writer = null)
		=> new(theme, labels, newLine, writer);

	/// <inheritdoc />
	public override void Write(PreparedLogEntry entry)
	{
		var levelStyle = Theme.GetStyleForLevel(entry.Level);
		var frameChar = GetFrameCharForLevel(entry.Level);

		// The top separator line
		Writer.Write("┌─── ");
		Writer.Write(new Text($"{frameChar}", levelStyle));
		Writer.Write(" ");

		// Timestamp
		var timestamp = entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
		Writer.Write(new Text(timestamp, Theme.Timestamp));
		Writer.Write(" ");

		// Log level
		Writer.Write(Theme.GetTextForLevel(entry.Level, Labels));

		// Finish the top line
		Writer.WriteLine(" ───────────");

		// Category line
		Writer.Write("│ ");
		Writer.Write(new Text("SOURCE:", Style.Parse("dim")));
		Writer.Write(" ");
		if (!string.IsNullOrWhiteSpace(entry.Category))
		{
			Writer.WriteStyled(entry.Category, Theme.Category, true);
		}
		else
		{
			Writer.Write(new Text("<unknown>", Style.Parse("dim")));
		}

		Writer.WriteLine();

		// Scopes line (if present)
		if (entry.Scopes.Count > 0)
		{
			Writer.Write("│ ");
			Writer.Write(new Text("CONTEXT:", Style.Parse("dim")));

			var style = Theme.Scopes;
			for (var i = 0; i < entry.Scopes.Count; i++)
			{
				Writer.Write(" ");
				if (i > 0)
				{
					Writer.WriteStyled("→ ", style);
				}

				Writer.WriteStyled(entry.Scopes[i]?.ToString() ?? "<null>", style);
			}

			Writer.WriteLine();
		}

		// Message line
		Writer.Write("│ ");
		Writer.Write(new Text("MESSAGE:", Style.Parse("dim")));
		Writer.Write(" ");
		if (!string.IsNullOrWhiteSpace(entry.Message))
		{
			Writer.WriteStyled(entry.Message, Theme.Message);
		}
		else
		{
			Writer.Write(new Text("<empty>", Style.Parse("dim")));
		}

		Writer.WriteLine();

		// Bottom line
		Writer.Write("└────");
		var elapsedSeconds = entry.Elapsed.TotalSeconds;
		Writer.Write(new Text($" Elapsed: {elapsedSeconds:0.000}s ", Theme.Timestamp));
		Writer.WriteLine("────────");

		// Exception handling
		if (entry.Exception is not null)
		{
			var panel = new Panel(new ExceptionDisplay(entry.Exception))
			{
				Border = BoxBorder.Rounded,
				BorderStyle = levelStyle,
				Header = new PanelHeader("Exception Details")
			};

			Writer.Write(panel);
			Writer.WriteLine();
		}

		// Add space after the entry
		if (NewLine) Writer.WriteLine();
	}

	private static char GetFrameCharForLevel(LogLevel level) => level switch
	{
		LogLevel.Trace => '⋯',    // Dotted line for trace
		LogLevel.Debug => '⋮',     // Vertical dots for debug
		LogLevel.Information => 'ℹ', // Info symbol
		LogLevel.Warning => '⚠',   // Warning symbol
		LogLevel.Error => '✗',     // Error cross
		LogLevel.Critical => '☠',  // Critical/fatal symbol
		_ => '?'                   // Unknown
	};
}
