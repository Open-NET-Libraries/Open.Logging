using Spectre.Console;

namespace Open.Logging.Extensions.SpectreConsole;

public partial record SpectreConsoleLogTheme
{
	/// <summary>
	/// A theme using standard terminal colors optimized for light backgrounds.
	/// Provides good contrast on white/light terminal backgrounds.
	/// </summary>
	public static readonly SpectreConsoleLogTheme LightBackground = new()
	{
		// Log level styles based on colors that work well on light backgrounds
		Trace = new Style(Color.Grey, decoration: Decoration.Dim),           // Grey (dim) for lowest level
		Debug = new Style(Color.Blue, decoration: Decoration.Dim),           // Blue (dim) for debug messages
		Information = Color.Green,                                           // Green for standard info
		Warning = new Style(Color.Olive, decoration: Decoration.Bold),       // Darker yellow for warnings
		Error = new Style(Color.Maroon, decoration: Decoration.Bold),        // Darker red for errors
		Critical = new Style(Color.White, Color.Maroon, Decoration.Bold),    // White on dark red for critical

		// Component styles
		Timestamp = new Style(Color.Grey, decoration: Decoration.Dim),       // Grey for timestamps
		Category = new Style(Color.Navy, decoration: Decoration.Italic),     // Dark blue for categories
		Scopes = new Style(Color.Teal, decoration: Decoration.Dim),          // Teal for scopes
		Message = Color.Black,                                               // Black for messages
		Exception = Color.Maroon                                             // Dark red for exceptions
	};
}