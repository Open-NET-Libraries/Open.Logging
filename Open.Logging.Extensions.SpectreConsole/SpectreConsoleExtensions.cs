using Spectre.Console;

namespace Open.Logging.Extensions.SpectreConsole;
public static class SpectreConsoleExtensions
{
	public static void WriteStyled(this IAnsiConsole console, string? text, Style style, bool trim = false)
	{
		if (trim ? string.IsNullOrWhiteSpace(text) : string.IsNullOrEmpty(text))
			return;

		if (trim) text = text.Trim();
		console.Write(new Text(text, style));
	}
}
