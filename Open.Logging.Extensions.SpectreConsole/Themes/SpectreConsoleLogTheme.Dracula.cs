using Spectre.Console;

namespace Open.Logging.Extensions.SpectreConsole;

public partial record SpectreConsoleLogTheme
{
    /// <summary>
    /// A Dracula-inspired theme with colors familiar to users of the popular code editor theme.
    /// </summary>
    public static readonly SpectreConsoleLogTheme Dracula = new()
    {
        // Log level styles based on Dracula theme colors
        Trace = new Style(new Color(98, 114, 164), decoration: Decoration.Dim),         // Comment color (dim purple/blue)
        Debug = new Style(new Color(139, 233, 253), decoration: Decoration.Dim),        // Cyan
        Information = new Color(80, 250, 123),                                          // Green
        Warning = new Style(new Color(255, 184, 108), decoration: Decoration.Bold),     // Orange
        Error = new Style(new Color(255, 85, 85), decoration: Decoration.Bold),         // Red
        Critical = new Style(new Color(248, 248, 242), new Color(189, 147, 249), Decoration.Bold), // Purple background with white text
        
        // Component styles
        Timestamp = new Style(new Color(98, 114, 164), decoration: Decoration.Dim),     // Comment color (dim purple/blue)
        Category = new Style(new Color(189, 147, 249), decoration: Decoration.Italic),  // Purple for class names
        Scopes = new Style(new Color(241, 250, 140), decoration: Decoration.Dim),       // Yellow
        Message = new Color(248, 248, 242),                                             // Foreground (white)
        Exception = new Color(255, 85, 85)                                              // Red for exceptions
    };
}