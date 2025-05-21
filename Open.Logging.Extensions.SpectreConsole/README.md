# Open.Logging.Extensions.SpectreConsole

A lightweight integration between Microsoft's logging infrastructure and [Spectre.Console](https://spectreconsole.net/) for enhanced console logging.

[![NuGet](https://img.shields.io/nuget/v/Open.Logging.Extensions.SpectreConsole.svg?label=NuGet)](https://www.nuget.org/packages/Open.Logging.Extensions.SpectreConsole/)

## Overview

This library bridges the gap between the standard Microsoft.Extensions.Logging framework and Spectre.Console's rich styling capabilities, making it easy to use Spectre.Console as a logging target in your .NET applications.

## Installation
dotnet add package Open.Logging.Extensions.SpectreConsole
## Basic Usage

Add the Spectre Console logger to your dependency injection container:

```cs
using Microsoft.Extensions.Logging;
using Open.Logging.Extensions.SpectreConsole;

// In your Startup.cs or Program.cs
services.AddLogging(builder =>
{
    builder.AddSimpleSpectreConsole();
});
```

### Inject and Use

Use the logger as you would any standard ILogger:

```cs
public class WeatherService
{
    private readonly ILogger<WeatherService> _logger;
    
    public WeatherService(ILogger<WeatherService> logger)
    {
        _logger = logger;
    }
    
    public void GetForecastAsync()
    {
        using (_logger.BeginScope("Location: {Location}", "Seattle"))
        {
            _logger.LogInformation("Retrieving weather forecast");
            
            try
            {
                // Your code here
                _logger.LogDebug("API request details: {Url}", "api/weather?city=Seattle");
                
                // Success case
                _logger.LogInformation("Forecast: {Temperature}°C", 22.5);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve forecast");
            }
        }
    }
}
```

## Customization

### Built-in Themes

The library comes with several pre-configured themes inspired by popular coding editors that you can use immediately:

```cs
// In your Startup.cs or Program.cs
services.AddLogging(builder =>
{
    builder.AddSimpleSpectreConsole(options =>
    {
        // Choose one of the built-in themes:
        options.Theme
            = SpectreConsoleLogTheme.TweakedDefaults;
    });
});
```

Available themes:

- **Default**: Uses standard console colors for compatibility with all terminal types
- **ModernColors**: Vibrant colors for modern terminals with rich color support
- **TweakedDefaults**: Enhanced default theme with improved contrast and readability
- **LightBackground**: Colors optimized for light background terminals
- **Dracula**: Inspired by the popular Dracula code editor theme
- **Monokai**: Inspired by the Monokai code editor theme
- **SolarizedDark**: Inspired by the Solarized Dark theme
- **OneDark**: Inspired by VS Code's One Dark Pro theme

### Custom Theme

If you prefer, you can create your own theme by customizing colors and formatting:
```cs
var customTheme = new SpectreConsoleLogTheme
{
    // Styles for log levels
    Trace = new Style(Color.Silver, decoration: Decoration.Dim),
    Debug = new Style(Color.Blue, decoration: Decoration.Dim),
    Information = Color.Green,
    Warning = new Style(Color.Yellow, decoration: Decoration.Bold),
    Error = new Style(Color.Red, decoration: Decoration.Bold),
    Critical = new Style(Color.White, Color.Red, Decoration.Bold),
    
    // Styles for components
    Timestamp = new Style(Color.Grey, decoration: Decoration.Dim),
    Category = new Style(Color.Grey, decoration: Decoration.Italic),
    Scopes = new Style(Color.Blue, decoration: Decoration.Dim),
    Message = Color.Silver,
    Exception = Color.Red
};

// Apply the custom theme
services.AddLogging(builder =>
{
    builder.AddSimpleSpectreConsole(options =>
    {
        options.Theme = customTheme;
    });
});
```

### Log Level Labels

You can also customize the text displayed for different log levels:
```cs
var customLabels = new LogLevelLabels
{
    Trace = "TRACE",
    Debug = "DEBUG",
    Information = "INFO",
    Warning = "WARN",
    Error = "ERROR",
    Critical = "FATAL"
};

// Apply custom labels
services.AddLogging(builder =>
{
    builder.AddSimpleSpectreConsole(options =>
    {
        options.Labels = customLabels;
    });
});

```

### Combined Configuration

You can combine theme and label customization in a single configuration:
```cs
services.AddLogging(builder =>
{
    builder.AddSimpleSpectreConsole(options =>
    {
        options.Theme = SpectreConsoleLogTheme.Dracula;
        options.Labels = new LogLevelLabels
        {
            Trace = "trace",
            Debug = "debug",
            Information = "info-",
            Warning = "warn!",
            Error = "ERROR",
            Critical = "FATAL"
        };
    });
});
```

## Styling Reference

This integration leverages Spectre.Console's excellent styling system. You can use any style supported by Spectre.Console:

For a complete style reference, see the [Spectre.Console documentation](https://spectreconsole.net/markup).

## Requirements

- .NET 9.0+
- Microsoft.Extensions.Logging
- Spectre.Console

## License

MIT License - see the LICENSE file for details.
