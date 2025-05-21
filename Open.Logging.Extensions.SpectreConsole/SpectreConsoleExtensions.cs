using Spectre.Console;

namespace Open.Logging.Extensions.SpectreConsole;

/// <summary>
/// Extension methods for <see cref="IAnsiConsole"/> to enhance styling capabilities.
/// </summary>
public static class SpectreConsoleExtensions
{
	/// <summary>
	/// Writes styled text to the console, with optional trimming.
	/// </summary>
	/// <param name="console">The console to write to.</param>
	/// <param name="text">The text to write.</param>
	/// <param name="style">The style to apply to the text.</param>
	/// <param name="trim">Whether to trim whitespace from the text before writing. Default is false.</param>
	public static void WriteStyled(this IAnsiConsole console, string? text, Style style, bool trim = false)
	{
		if (trim ? string.IsNullOrWhiteSpace(text) : string.IsNullOrEmpty(text))
			return;

		if (trim) text = text.Trim();
		console.Write(new Text(text, style));
	}
}
