using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Open.Logging.Extensions.SpectreConsole.Formatters;

/// <summary>
/// A compact formatter that outputs log entries in a single line with minimal decoration.
/// </summary>
/// <inheritdoc />
public sealed class CompactSpectreConsoleFormatter(
	SpectreConsoleLogTheme? theme = null,
	LogLevelLabels? labels = null,
	bool newLine = false,
	IAnsiConsole? writer = null)
	: SpectreConsoleFormatterBase(theme, labels, newLine, writer)
	, ISpectreConsoleFormatter<CompactSpectreConsoleFormatter>
{
	/// <inheritdoc />
	public static CompactSpectreConsoleFormatter Create(
		SpectreConsoleLogTheme? theme = null,
		LogLevelLabels? labels = null,
		bool newLine = false,
		IAnsiConsole? writer = null)
		=> new(theme, labels, newLine, writer);

	/// <inheritdoc />
	protected override void WriteLevel(LogLevel level, Placement whiteSpace = Placement.None)
	{
		string levelIcon = GetLogLevelIcon(level);
		Write(levelIcon, Theme.GetStyleForLevel(level));
		Write(" ");
	}

	/// <inheritdoc />
	protected override bool WriteCategory(string? category, Placement whiteSpace = Placement.None)
	{
		if (string.IsNullOrWhiteSpace(category))
			return false;

		string shortCategory = GetShortCategoryName(category);
		Write(shortCategory, Theme.Category, true);
		Write(" ");
		return true;
	}

	/// <summary>
	/// Writes all scopes to the console.
	/// </summary>
	private bool WriteScopes(IReadOnlyList<object> scopes)
	{
		if (scopes.Count <= 0)
			return false;

		Write(" ");
		var style = Theme.Scopes;
		Write("(", style);

		for (var i = 0; i < scopes.Count; i++)
		{
			if (i > 0)
				Write("→", style);

			Write(scopes[i].ToString(), style);
		}

		Write(")", style);
		return true;
	}

	/// <inheritdoc />
	public override void Write(PreparedLogEntry entry)
	{
		// Write log level as colored icon
		WriteLevel(entry.Level);

		// Category (short form)
		WriteCategory(entry.Category);

		// Message
		WriteMessage(entry.Message);

		// Scopes (if any)
		WriteScopes(entry.Scopes);

		Writer.WriteLine();

		// Exception handling
		WriteException(entry.Exception, hrs: Placement.Both);

		if (NewLine)
			Writer.WriteLine();
	}

	private static string GetLogLevelIcon(LogLevel level) => level switch
	{
		LogLevel.Trace => "·",
		LogLevel.Debug => "•",
		LogLevel.Information => "ℹ",
		LogLevel.Warning => "⚠",
		LogLevel.Error => "✗",
		LogLevel.Critical => "☠",
		_ => "?"
	};

	private static string GetShortCategoryName(string category)
	{
		// Get the last part of the namespace for brevity
		int lastDot = category.LastIndexOf('.');
		return lastDot >= 0 ? category[(lastDot + 1)..] : category;
	}
}
