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

	private void WriteIndent()
		=> Write("    ");

	/// <inheritdoc />
	protected override void WriteTimestamp(DateTimeOffset timestamp, string format = "yyyy-MM-dd HH:mm:ss.fff")
	{
		base.WriteTimestamp(timestamp, format);
	}

	/// <inheritdoc />
	protected override bool WriteCategory(string? category, Placement whiteSpace = Placement.None)
	{
		if (string.IsNullOrWhiteSpace(category))
			return false;

		WriteIndent();
		return base.WriteCategory(category);
	}

	/// <summary>
	/// Writes all scopes to the console.
	/// </summary>
	private bool WriteScopes(IReadOnlyList<object> scopes)
	{
		if (scopes.Count <= 0)
			return false;

		var style = Theme.Scopes;
		Write(" => ");
		for (var i = 0; i < scopes.Count; i++)
		{
			if (i > 0)
				Write(" > ", style);

			Write(scopes[i].ToString(), style);
		}

		return true;
	}

	/// <inheritdoc />
	protected override bool WriteMessage(string? message, bool trim = false, Placement whiteSpace = Placement.None)
	{
		if (string.IsNullOrWhiteSpace(message))
			return false;

		WriteIndent();
		return base.WriteMessage(message, trim);
	}

	/// <inheritdoc />
	protected override bool WriteException(Exception? exception)
	{
		if (exception is null)
			return false;

		// Exception details in a highlighted box
		var panel = new Panel(new ExceptionDisplay(exception))
		{
			Border = BoxBorder.Rounded,
			BorderStyle = Theme.GetStyleForLevel(LogLevel.Error)
		};

		Write(panel);
		return true;
	}

	/// <inheritdoc />
	public override void Write(PreparedLogEntry entry)
	{
		// First line: timestamp and log level
		WriteTimestamp(entry.Timestamp);
		WriteLevel(entry.Level, whiteSpace: Placement.Before);
		WriteLine();

		// Second line: category with optional scope
		WriteCategory(entry.Category);
		WriteScopes(entry.Scopes);
		WriteLine();

		// Third line: message
		WriteMessage(entry.Message);
		WriteLine();

		// Exception if present
		WriteException(entry.Exception);

		if (NewLine) WriteLine();
	}
}
