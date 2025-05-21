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
	public override void Write(PreparedLogEntry entry)
	{
		// Write log level as colored icon
		string levelIcon = GetLogLevelIcon(entry.Level);
		Writer.Write(new Text(levelIcon, Theme.GetStyleForLevel(entry.Level)));
		Writer.Write(" ");

		// Category (short form)
		if (!string.IsNullOrWhiteSpace(entry.Category))
		{
			string shortCategory = GetShortCategoryName(entry.Category);
			Writer.WriteStyled(shortCategory, Theme.Category, true);
			Writer.Write(" ");
		}

		// Message
		if (!string.IsNullOrWhiteSpace(entry.Message))
			Writer.WriteStyled(entry.Message, Theme.Message);

		// Scopes (if any)
		if (entry.Scopes.Count > 0)
		{
			Writer.Write(" ");
			var style = Theme.Scopes;
			Writer.WriteStyled("(", style);

			for (var i = 0; i < entry.Scopes.Count; i++)
			{
				if (i > 0)
					Writer.WriteStyled("→", style);

				Writer.WriteStyled(entry.Scopes[i].ToString(), style);
			}

			Writer.WriteStyled(")", style);
		}

		Writer.WriteLine();

		// Exception handling
		if (entry.Exception is not null)
		{
			var rule = new Rule() { Style = Style.Parse("dim") };
			Writer.Write(rule);

			try
			{
				Writer.WriteException(entry.Exception);
			}
			catch
			{
				Writer.WriteLine($"Exception: {entry.Exception.Message}");
				var st = entry.Exception.StackTrace;
				if (!string.IsNullOrWhiteSpace(st))
					Writer.WriteLine(st);
			}

			Writer.Write(rule);
		}
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
