using Spectre.Console;

namespace Open.Logging.Extensions.SpectreConsole;

public partial record SpectreConsoleLogTheme
{
	/// <summary>
	/// A modern theme with vibrant colors for terminals that support rich color display.
	/// </summary>
	public static readonly SpectreConsoleLogTheme ModernColors = new()
	{
		// Log level styles based on vibrant colors for modern terminals
		Trace = new Style(Color.Silver, decoration: Decoration.Dim),
		Debug = new Style(Color.Blue, decoration: Decoration.Dim),
		Information = new Style(Color.Teal),
		Warning = new Style(Color.Gold1, decoration: Decoration.Bold),
		Error = new Style(Color.Red3, decoration: Decoration.Bold),
		Critical = new Style(Color.Grey, Color.Red3, Decoration.Bold),

		// Component styles
		Timestamp = new Style(Color.Grey, decoration: Decoration.Dim),
		Category = new Style(Color.Grey, decoration: Decoration.Italic),
		Scopes = new Style(Color.Green, decoration: Decoration.Dim),
		Message = Style.Plain,
		Exception = new Style(Color.Grey)
	};
}