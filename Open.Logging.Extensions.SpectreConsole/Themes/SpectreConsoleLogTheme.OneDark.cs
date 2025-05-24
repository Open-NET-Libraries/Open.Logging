using Spectre.Console;

namespace Open.Logging.Extensions.SpectreConsole;

public partial record SpectreConsoleLogTheme
{
	/// <summary>
	/// A theme inspired by the "One Dark Pro" theme popular in Visual Studio Code.
	/// </summary>
	public static readonly SpectreConsoleLogTheme OneDark = new()
	{
		// Log level styles based on One Dark theme colors
		Trace = new Style(new Color(92, 99, 112), decoration: Decoration.Dim),       // Comment gray
		Debug = new Style(new Color(86, 182, 194), decoration: Decoration.Dim),      // Cyan
		Information = new Color(152, 195, 121),                                      // Green
		Warning = new Style(new Color(229, 192, 123), decoration: Decoration.Bold),  // Yellow
		Error = new Style(new Color(224, 108, 117), decoration: Decoration.Bold),    // Red
		Critical = new Style(new Color(171, 178, 191), new Color(224, 108, 117), Decoration.Bold), // Red background with foreground text

		// Component styles
		Timestamp = new Style(new Color(92, 99, 112), decoration: Decoration.Dim),   // Comment gray
		Category = new Style(new Color(198, 120, 221), decoration: Decoration.Italic), // Purple for class names
		Scopes = new Style(new Color(209, 154, 102), decoration: Decoration.Dim),    // Orange
		Message = new Color(171, 178, 191),                                          // Default text color
		Exception = new Color(224, 108, 117)                                         // Red for exceptions
	};
}