using Spectre.Console;

namespace Open.Logging.Extensions.SpectreConsole;

/// <summary>
/// Configuration options for the Spectre Console logger.
/// </summary>
public class SpectreConsoleOptions
{
    /// <summary>
    /// Gets or sets the theme to use for console output styling. 
    /// If not specified, uses <see cref="SpectreConsoleLogTheme.Default"/>.
    /// </summary>
    public SpectreConsoleLogTheme? Theme { get; set; }
    
    /// <summary>
    /// Gets or sets the labels to use for different log levels.
    /// If not specified, uses <see cref="Defaults.LevelLabels"/>.
    /// </summary>
    public LogLevelLabels? Labels { get; set; }

    /// <summary>
    /// Gets or sets the console writer to use. 
    /// If not specified, uses <see cref="AnsiConsole.Console"/>.
    /// </summary>
    public IAnsiConsole? Writer { get; set; }
}
