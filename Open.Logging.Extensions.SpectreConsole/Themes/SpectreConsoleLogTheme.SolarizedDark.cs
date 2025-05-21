using Spectre.Console;

namespace Open.Logging.Extensions.SpectreConsole;

public partial record SpectreConsoleLogTheme
{
    /// <summary>
    /// A Solarized Dark-inspired theme with the distinctive color palette familiar to users of the popular Solarized theme.
    /// </summary>
    public static readonly SpectreConsoleLogTheme SolarizedDark = new()
    {
        // Log level styles based on Solarized Dark theme colors
        Trace = new Style(new Color(88, 110, 117), decoration: Decoration.Dim),      // Base01 (emphasized content)
        Debug = new Style(new Color(38, 139, 210), decoration: Decoration.Dim),      // Blue
        Information = new Color(133, 153, 0),                                        // Green
        Warning = new Style(new Color(181, 137, 0), decoration: Decoration.Bold),    // Yellow
        Error = new Style(new Color(220, 50, 47), decoration: Decoration.Bold),      // Red
        Critical = new Style(new Color(253, 246, 227), new Color(220, 50, 47), Decoration.Bold), // Red background with base3 text
        
        // Component styles
        Timestamp = new Style(new Color(88, 110, 117), decoration: Decoration.Dim),  // Base01
        Category = new Style(new Color(108, 113, 196), decoration: Decoration.Italic), // Violet for class names
        Scopes = new Style(new Color(42, 161, 152), decoration: Decoration.Dim),     // Cyan
        Message = new Color(147, 161, 161),                                          // Base1 (body text)
        Exception = new Color(211, 54, 130)                                          // Magenta for exceptions
    };
}