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

	private static readonly Style Dim = new(decoration: Decoration.Dim);

	/// <inheritdoc />
	protected override bool WriteCategory(string? category, Placement whiteSpace = Placement.None)
	{
		if (!string.IsNullOrWhiteSpace(category))
		{
			Write("│ ");
			Write("SOURCE:", Dim);
			Write(" ");
			Write(category, Theme.Category, true);
			return true;
		}
		else
		{
			return false;
		}
	}

	/// <summary>
	/// Writes all scopes to the console.
	/// </summary>
	private bool WriteScopes(IReadOnlyList<object> scopes)
	{
		if (scopes.Count <= 0)
			return false;

		Write("│ ");
		Write("CONTEXT:", Dim);

		var style = Theme.Scopes;
		for (var i = 0; i < scopes.Count; i++)
		{
			Write(" ");
			if (i > 0)
			{
				Write("→ ", style);
			}

			Write(scopes[i]?.ToString() ?? "<null>", style);
		}

		return true;
	}

	/// <inheritdoc />
	protected override bool WriteMessage(string? message, bool trim = false, Placement whiteSpace = Placement.None)
	{
		if (!string.IsNullOrWhiteSpace(message))
		{
			Write("│ ");
			Write("MESSAGE:", Dim);
			Write(" ");
			Write(message, Theme.Message, trim);
			return true;
		}
		else
		{
			return false;
		}
	}

	/// <inheritdoc />
	protected override void WriteElapsed(TimeSpan elapsed, string format = "0.000s")
	{
		var elapsedSeconds = elapsed.TotalSeconds;
		var text = $" Elapsed: {elapsedSeconds.ToString(format, CultureInfo.InvariantCulture)} ";
		Write(text, Theme.Timestamp);
	}

	/// <inheritdoc />
	protected override bool WriteException(Exception? exception)
	{
		if (exception is null)
			return false;

		var panel = new Panel(new ExceptionDisplay(exception))
		{
			Border = BoxBorder.Rounded,
			BorderStyle = Theme.GetStyleForLevel(LogLevel.Error),
			Header = new PanelHeader("Exception Details")
		};

		Write(panel);
		WriteLine();
		return true;
	}

	/// <inheritdoc />
	public override void Write(PreparedLogEntry entry)
	{
		var levelStyle = Theme.GetStyleForLevel(entry.Level);
		var frameChar = GetFrameCharForLevel(entry.Level);

		// The top separator line
		Write("┌─── ");
		Write(new Text($"{frameChar}", levelStyle));
		Write(" ");

		// Timestamp and log level
		WriteTimestamp(entry.Timestamp);
		WriteLevel(entry.Level, whiteSpace: Placement.Before);

		// Finish the top line
		WriteLine(" ───────────");

		// Category line
		WriteCategory(entry.Category);
		WriteLine();

		// Scopes line (if present)
		if (WriteScopes(entry.Scopes))
		{
			WriteLine();
		}

		// Message line
		WriteMessage(entry.Message);
		WriteLine();

		// Bottom line
		Write("└────");
		WriteElapsed(entry.Elapsed);
		WriteLine("────────");

		// Exception handling
		WriteException(entry.Exception);

		// Add space after the entry
		if (NewLine) WriteLine();
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
