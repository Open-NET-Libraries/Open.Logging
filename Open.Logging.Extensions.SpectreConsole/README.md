# Open.Logging.Extensions.SpectreConsole

A lightweight integration between Microsoft's logging infrastructure and [Spectre.Console](https://spectreconsole.net/) for enhanced console logging.

## Overview

This library bridges the gap between the standard Microsoft.Extensions.Logging framework and Spectre.Console's rich styling capabilities, making it easy to use Spectre.Console as a logging target in your .NET applications.

## Installation

```sh
dotnet add package Open.Logging.Extensions.SpectreConsole
```

## Basic Usage

Add the Spectre Console logger to your dependency injection container:

```csharp
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

```csharp
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

### Theme

Spectre.Console provides beautiful styling out of the box. If needed, you can customize colors and formatting:

```csharp
var customTheme = new SpectreConsoleLogTheme
{
    // Styles for log levels
    Information = new Style(foreground: Color.Cyan),
    Warning = new Style(foreground: Color.Yellow),
    Error = new Style(foreground: Color.Red),
    
    // Styles for components
    Timestamp = new Style(foreground: Color.Grey),
    Category = new Style(foreground: Color.Grey, decoration: Decoration.Italic),
    Message = Style.Plain
};

// Apply the custom theme
services.AddLogging(builder =>
{
    var logger = new SimpleSpectreConsoleLogger(theme: customTheme);
    builder.AddProvider(new SimpleSpectreConsoleLoggerProvider(logger));
});
```

### Log Level Labels

You can also customize the text displayed for different log levels:

```csharp
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
    var logger = new SimpleSpectreConsoleLogger(labels: customLabels);
    builder.AddProvider(new SimpleSpectreConsoleLoggerProvider(logger));
});
```

### Buffered Logging

For high-throughput applications:

```csharp
using Open.Logging.Extensions;

// Get logger from DI
ILogger logger = serviceProvider.GetRequiredService<ILogger<MyService>>();

// Create buffered logger
BufferedLogger bufferedLogger = logger.AsBuffered();

// Use with await using for automatic flushing
await using (bufferedLogger)
{
    bufferedLogger.LogInformation("This will be buffered");
}
```

## Styling Reference

This integration leverages Spectre.Console's excellent styling system. You can use any style supported by Spectre.Console:

```csharp
// Simple color names
Error = "red"

// With decorations
Warning = "bold yellow"

// With background
Critical = "white on red"

// Using Style constructor
Debug = new Style(Color.Blue, Color.Default, Decoration.Dim)
```

For a complete style reference, see the [Spectre.Console documentation](https://spectreconsole.net/markup).

## Requirements

- .NET 9.0+
- Microsoft.Extensions.Logging
- Spectre.Console

## License

MIT License - see the LICENSE file for details.
