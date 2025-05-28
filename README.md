# Open.Logging

A lightweight set of .NET libraries that enhances the standard logging infrastructure with additional formatters and extensions.

[![NuGet](https://img.shields.io/nuget/v/Open.Logging.Extensions.svg?label=NuGet)](https://www.nuget.org/packages/Open.Logging.Extensions/)

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

## Documentation & Releases

- **[Development History](dev-history/)** - Detailed release notes and migration guides
- **[CHANGELOG.md](CHANGELOG.md)** - Summary of all changes
- **[Multiple Loggers Guide](MULTIPLE_LOGGERS_GUIDE.md)** - Advanced multi-logger scenarios

## Requirements

These may expand in the future.  If anyone needs legacy support, please fill out an request in the repo on GitHub.

- .NET 9.0
- C# 13.0

## License

MIT License - see the LICENSE file for details.