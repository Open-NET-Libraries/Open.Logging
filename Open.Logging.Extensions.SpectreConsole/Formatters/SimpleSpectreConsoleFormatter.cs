using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Open.Logging.Extensions.SpectreConsole.Formatters;

/// <inheritdoc />
public class SimpleSpectreConsoleFormatter(
	SpectreConsoleLogTheme? theme = null,
	LogLevelLabels? labels = null,
	bool newLine = false,
	IAnsiConsole? writer = null)
	: SpectreConsoleFormatterBase(theme, labels, newLine, writer)
	, ISpectreConsoleFormatter<SimpleSpectreConsoleFormatter>
{
	/// <inheritdoc />
	public static SimpleSpectreConsoleFormatter Create(
		SpectreConsoleLogTheme? theme = null,
		LogLevelLabels? labels = null,
		bool newLine = false,
		IAnsiConsole? writer = null)
		=> new(theme, labels, newLine, writer);

	/// <inheritdoc />
	protected override void WriteLevel(LogLevel level, Placement whiteSpace = Placement.None)
	{
		if (whiteSpace.HasFlag(Placement.Before)) Write(" ");
		Write("[");
		base.WriteLevel(level);
		Write("]"); // Brackets [xxxx] are easier to search for in logs.
		if (whiteSpace.HasFlag(Placement.Before)) Write(" ");
	}

	/// <inheritdoc />
	private bool WriteScopes(IReadOnlyList<object> scopes)
	{
		// Add the scope information if it exists.
		if (!(scopes?.Count > 0))
		{
			return false;
		}

		var style = Theme.Scopes;
		Write(" ");
		Write("(", style);
		for (var i = 0; i < scopes.Count; i++)
		{
			if (i > 0)
				Write(" > ", style);

			Write(scopes[i].ToString(), style);
		}

		Write(")", style);
		return true;
	}

	/// <inheritdoc />
	public override void Write(PreparedLogEntry entry)
	{
		WriteElapsed(entry.Elapsed);
		WriteLevel(entry.Level, whiteSpace: Placement.Before);
		WriteCategory(entry.Category, whiteSpace: Placement.Before);
		// Add the separator between the category and the scope.
		Write(":");

		WriteScopes(entry.Scopes);
		WriteMessage(entry.Message, whiteSpace: Placement.Before);

		Writer.WriteLine();
		WriteException(entry.Exception, hrs: Placement.Both);

		if (NewLine) Writer.WriteLine();
	}
}
