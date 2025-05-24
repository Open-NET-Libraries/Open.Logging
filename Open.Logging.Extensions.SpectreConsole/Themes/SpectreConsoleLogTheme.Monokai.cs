using Spectre.Console;

namespace Open.Logging.Extensions.SpectreConsole;

public partial record SpectreConsoleLogTheme
{
	/// <summary>
	/// A Monokai-inspired theme with vibrant colors familiar to users of the popular code editor theme.
	/// </summary>
	public static readonly SpectreConsoleLogTheme Monokai = new()
	{
		// Log level styles based on Monokai theme colors
		Trace = new Style(new Color(117, 113, 94), decoration: Decoration.Dim),       // Comment color (gray/brown)
		Debug = new Style(new Color(102, 217, 239), decoration: Decoration.Dim),      // Cyan/Light Blue
		Information = new Color(166, 226, 46),                                        // Bright Green
		Warning = new Style(new Color(253, 151, 31), decoration: Decoration.Bold),    // Orange
		Error = new Style(new Color(249, 38, 114), decoration: Decoration.Bold),      // Pink/Magenta
		Critical = new Style(new Color(248, 248, 242), new Color(249, 38, 114), Decoration.Bold), // Pink background with white text

		// Component styles
		Timestamp = new Style(new Color(117, 113, 94), decoration: Decoration.Dim),   // Comment color
		Category = new Style(new Color(174, 129, 255), decoration: Decoration.Italic), // Purple for class names
		Scopes = new Style(new Color(230, 219, 116), decoration: Decoration.Dim),     // Yellow
		Message = new Color(248, 248, 242),                                           // Foreground (white)
		Exception = new Color(249, 38, 114)                                           // Pink/Magenta for exceptions
	};
}