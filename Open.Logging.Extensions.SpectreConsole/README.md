# Open.Logging.Extensions.SpectreConsole

A lightweight integration between Microsoft's logging infrastructure and [Spectre.Console](https://spectreconsole.net/) for enhanced console logging.

[![NuGet](https://img.shields.io/nuget/v/Open.Logging.Extensions.SpectreConsole.svg?label=NuGet)](https://www.nuget.org/packages/Open.Logging.Extensions.SpectreConsole/)

## Overview

This library bridges the gap between the standard Microsoft.Extensions.Logging framework and Spectre.Console's rich styling capabilities, making it easy to use Spectre.Console as a logging target in your .NET applications. It provides multiple formatter options to display your logs in various styles, from simple one-line outputs to structured multi-line formats.

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
    builder.AddSpectreConsole<SimpleSpectreConsoleFormatter>();
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

### Available Formatters

The library comes with several pre-built formatters that provide different styles of log output:

- **SimpleSpectreConsoleFormatter**: A compact single-line formatter (default)
- **MicrosoftStyleSpectreConsoleFormatter**: Multi-line format similar to Microsoft's default console logger
- **CompactSpectreConsoleFormatter**: Space-efficient format with icons for log levels
- **CallStackSpectreConsoleFormatter**: Structured format with visual frame indicators
- **StructuredMultilineFormatter**: Grid-based format with clear labeling

Usage example:

```cs
// Choose any of the pre-built formatters
services.AddLogging(builder =>
{
    builder.AddSpectreConsole<MicrosoftStyleSpectreConsoleFormatter>();
});
```

### Creating Your Own Formatter

You can easily create your own custom formatter by implementing the `ISpectreConsoleFormatter<T>` interface:

```cs
public sealed class MyCustomFormatter(
    SpectreConsoleLogTheme? theme = null,
    LogLevelLabels? labels = null,
    bool newLine = false,
    IAnsiConsole? writer = null)
    : SpectreConsoleFormatterBase(theme, labels, newLine, writer)
    , ISpectreConsoleFormatter<MyCustomFormatter>
{
    public static MyCustomFormatter Create(
        SpectreConsoleLogTheme? theme = null,
        LogLevelLabels? labels = null,
        bool newLine = false,
        IAnsiConsole? writer = null)
        => new(theme, labels, newLine, writer);
        
    public override void Write(PreparedLogEntry entry)
    {
        // Your custom formatting logic here
        Writer.WriteLine($"[{entry.Level}] {entry.Message}");
        
        // Handle exceptions
        if (entry.Exception is not null)
        {
            Writer.WriteException(entry.Exception);
        }
    }
}
```

After creating your formatter, register it with the DI container:

```cs
services.AddLogging(builder =>
{
    builder.AddSpectreConsole<MyCustomFormatter>();
});
```

### Interactive Demo

To explore the various formatters with different themes, run the demo application with the interactive flag:

```bash
cd Open.Logging.Extensions.Demo
dotnet run -- interactive
```

This will launch an interactive console application that allows you to select different formatters and themes to see how they look with sample log entries. It's a great way to experiment with different combinations before implementing them in your application.

### Built-in Themes

The library comes with several pre-configured themes inspired by popular coding editors that you can use immediately:

```cs
// In your Startup.cs or Program.cs
services.AddLogging(builder =>
{
    builder.AddSpectreConsole<SimpleSpectreConsoleFormatter>(options =>
    {
        // Choose one of the built-in themes:
        options.Theme = SpectreConsoleLogTheme.TweakedDefaults;
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
    builder.AddSpectreConsole<SimpleSpectreConsoleFormatter>(options =>
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
    builder.AddSpectreConsole<SimpleSpectreConsoleFormatter>(options =>
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
    builder.AddSpectreConsole<SimpleSpectreConsoleFormatter>(options =>
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

### Screenshot Examples

Below are examples of the different formatters in action:

#### SimpleSpectreConsoleFormatter

![SimpleSpectreConsoleFormatter example](https://raw.githubusercontent.com/Open-NET-Libraries/Open.Logging/main/Open.Logging.Extensions.SpectreConsole/docs/images/simple-formatter.png)

#### MicrosoftStyleSpectreConsoleFormatter

![MicrosoftStyleSpectreConsoleFormatter example](https://raw.githubusercontent.com/Open-NET-Libraries/Open.Logging/main/Open.Logging.Extensions.SpectreConsole/docs/images/microsoft-style-formatter.png)

#### CallStackSpectreConsoleFormatter

![CallStackSpectreConsoleFormatter example](https://raw.githubusercontent.com/Open-NET-Libraries/Open.Logging/main/Open.Logging.Extensions.SpectreConsole/docs/images/callstack-formatter.png)

#### StructuredMultilineFormatter

![StructuredMultilineFormatter example](https://raw.githubusercontent.com/Open-NET-Libraries/Open.Logging/main/Open.Logging.Extensions.SpectreConsole/docs/images/structured-multiline-formatter.png)

## Styling Reference

This integration leverages Spectre.Console's excellent styling system. You can use any style supported by Spectre.Console:

For a complete style reference, see the [Spectre.Console documentation](https://spectreconsole.net/markup).

## Requirements

- .NET 9.0+
- Microsoft.Extensions.Logging
- Spectre.Console

## License

MIT License - see the LICENSE file for details.
