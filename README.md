# Open.Logging

A lightweight set of .NET libraries that enhances the standard logging infrastructure with additional formatters and extensions.

## Features

- Seamless integration with Microsoft.Extensions.Logging
- Beautiful console output through Spectre Console integration
- Customizable log formatting and styling
- Support for log scopes and context
- Thread-safe logging operations
- Exception formatting and display

## Installation

```sh
dotnet add package Open.Logging.Extensions
```
or for more colorful output:
```sh
dotnet add package Open.Logging.Extensions.SpectreConsole
```


## Usage

### Basic Setup

```csharp
using Microsoft.Extensions.Logging;
using Open.Logging.Extensions.SpectreConsole;

// Add to your service collection
services.AddLogging(builder =>
{
    builder.AddSimpleSpectreConsole();
});

// Inject and use
public class MyService
{
    private readonly ILogger<MyService> _logger;
    
    public MyService(ILogger<MyService> logger)
    {
        _logger = logger;
    }
    
    public void DoSomething()
    {
        _logger.LogInformation("Operation started");
        // ...
    }
}
```

### Customizing Log Level Labels

```csharp
var customLabels = new LogLevelLabels
{
    Information = "INFO",
    Warning = "ATTENTION",
    Error = "PROBLEM"
};

services.AddLogging(builder =>
{
    builder.AddSimpleSpectreConsole(options =>
    {
        options.Labels = customLabels;
    });
});
```

### Custom Theming

```csharp
var theme = new SpectreConsoleLogTheme
{
    // Configure colors and styles
};

services.AddLogging(builder =>
{
    builder.AddSpectreConsole(options =>
    {
        options.Theme = theme;
    });
});
```

### Buffered Logging

For high-throughput applications:
```cs
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

## Requirements

These may expand in the future.  If anyone needs legacy support, please fill out an request in the repo on GitHub.

- .NET 9.0
- C# 13.0

## License

MIT License - see the LICENSE file for details.