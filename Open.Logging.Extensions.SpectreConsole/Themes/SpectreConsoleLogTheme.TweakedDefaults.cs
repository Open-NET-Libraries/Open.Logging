using Spectre.Console;

namespace Open.Logging.Extensions.SpectreConsole;

public partial record SpectreConsoleLogTheme
{
	/// <summary>
	/// A slightly enhanced version of the default theme with improved color contrast and readability.
	/// </summary>
	public static readonly SpectreConsoleLogTheme TweakedDefaults = new()
	{
		// Log level styles based on enhanced default colors
		Trace = new Style(new Color(190, 190, 200), decoration: Decoration.Dim),      // Enhanced Silver
		Debug = new Style(new Color(65, 135, 235), decoration: Decoration.Dim),       // Enhanced Blue
		Information = new Color(0, 170, 170),                                         // Enhanced Teal
		Warning = new Style(new Color(240, 180, 40), decoration: Decoration.Bold),    // Enhanced Gold
		Error = new Style(new Color(220, 60, 50), decoration: Decoration.Bold),       // Enhanced Red
		Critical = new Style(new Color(240, 240, 240), new Color(155, 25, 25), Decoration.Bold), // Enhanced Grey on Red

		// Component styles
		Timestamp = new Style(new Color(130, 130, 130), decoration: Decoration.Dim),  // Subtly enhanced Grey
		Category = new Style(new Color(180, 180, 180), decoration: Decoration.Italic), // Slightly brighter Grey
		Scopes = new Style(new Color(30, 180, 80), decoration: Decoration.Dim),       // Enhanced Green
		Message = new Color(230, 230, 230),                                           // Off-white instead of Plain
		Exception = new Color(205, 92, 92)                                            // Enhanced Grey for exceptions
	};
}