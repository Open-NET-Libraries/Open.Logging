using Spectre.Console;
using System.Globalization;

namespace Open.Logging.Extensions.SpectreConsole.Formatters;

/// <summary>
/// A formatter that outputs log entries in a minimal multi-line format with Spectre.Console styling.
/// </summary>
/// <inheritdoc />
public sealed class MinimalMutliLineSpectreConsoleFormatter(
	SpectreConsoleLogTheme? theme = null,
	LogLevelLabels? labels = null,
	bool newLine = false,
	IAnsiConsole? writer = null)
	: SimpleSpectreConsoleFormatter(theme, labels, newLine, writer)
	, ISpectreConsoleFormatter<MinimalMutliLineSpectreConsoleFormatter>
{
	/// <inheritdoc />
	public static new MinimalMutliLineSpectreConsoleFormatter Create(
		SpectreConsoleLogTheme? theme = null,
		LogLevelLabels? labels = null,
		bool newLine = false,
		IAnsiConsole? writer = null)
		=> new(theme, labels, newLine, writer);

	/// <inheritdoc />
	protected override void WriteElapsed(TimeSpan elapsed, string format = "0.000s")
	{
		var elapsedSeconds = elapsed.TotalSeconds;
		var text = $" ({elapsedSeconds.ToString(format, CultureInfo.InvariantCulture)})";
		Write(text, Theme.Timestamp);
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
	public override void Write(PreparedLogEntry entry)
	{
		if (!NewLine) Write(HR);

		// First line: timestamp, elapsed, category and scopes
		WriteTimestamp(entry.Timestamp);
		WriteElapsed(entry.Elapsed);
		WriteCategory(entry.Category, Placement.Before);
		WriteScopes(entry.Scopes);
		WriteLine();

		// Second line: log level and message
		WriteLevel(entry.Level);
		WriteMessage(entry.Message, trim: true, whiteSpace: Placement.Before);
		WriteLine();

		// Exception if present
		WriteException(entry.Exception);

		if (NewLine) WriteLine();
	}
}
