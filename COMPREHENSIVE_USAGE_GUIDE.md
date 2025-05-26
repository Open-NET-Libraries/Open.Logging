# Open.Logging.Extensions - Comprehensive Usage Guide

## Overview

`Open.Logging.Extensions` is a production-ready logging system that provides **native Microsoft.Extensions.Logging integration** with enterprise features like file rolling, retention policies, rich console formatting, and high-performance async processing.

## Quick Start

### Basic File Logging

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Open.Logging.Extensions;

// Setup with dependency injection
var services = new ServiceCollection();
services.AddLogging(builder =>
{
    builder.AddFile(options =>
    {
        options.LogDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
        options.FileNamePattern = "app-{Timestamp:yyyyMMdd}.log";
        options.MinLogLevel = LogLevel.Information;
    });
});

var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<MyClass>>();

// Use standard ILogger interface
logger.LogInformation("Application started at {StartTime}", DateTime.Now);
logger.LogError(exception, "An error occurred processing {UserId}", userId);
```

### Rich Console Logging

```csharp
services.AddLogging(builder =>
{
    builder.AddSpectreConsole(theme: SpectreConsoleTheme.ModernColors);
});
```

## Core Features

### 1. Native Microsoft.Extensions.Logging Integration

✅ **Full Compatibility**: Works with `ILogger<T>`, `ILoggerFactory`, and dependency injection  
✅ **Standard Interface**: No vendor lock-in, uses Microsoft's logging abstractions  
✅ **Structured Logging**: Full support for structured data and message templates  

### 2. Multiple Output Destinations

| Destination | Description | Use Case |
|-------------|-------------|----------|
| **File** | Enterprise file logging with rolling | Production applications, audit trails |
| **Spectre.Console** | Rich console output with themes | Development, CLI applications |
| **Buffered** | Thread-safe wrapper for any logger | High-throughput scenarios |
| **Delegate** | Custom output via delegates | Testing, custom integrations |

### 3. File Logging with Enterprise Features

#### Automatic File Rolling
```csharp
builder.AddFile(options =>
{
    options.LogDirectory = "logs";
    options.FileNamePattern = "app-{Timestamp:yyyyMMdd_HHmmss}.log";
    options.MaxFileSize = 10 * 1024 * 1024; // 10MB
    options.MaxRetainedFiles = 5; // Keep 5 files
});
```

#### Retention Policies
- **Automatic cleanup** of old log files
- **Configurable retention count** via `MaxRetainedFiles`
- **Background processing** for non-blocking operation

#### Custom Formatting Templates
```csharp
options.Template = "{Elapsed} [{Level}] {Category}{Scopes}: {Message}{NewLine}{Exception}";
```

### 4. Thread Safety & High Performance

#### Channel-Based Async Processing
```csharp
// Internal implementation uses bounded channels
Channel.CreateBounded<PreparedLogEntry>(10000) // Non-blocking writes
```

#### Performance Features
- ✅ **Bounded queues** prevent memory issues under load
- ✅ **Single reader, multiple writer** design for optimal throughput  
- ✅ **Background processing** doesn't block application threads
- ✅ **Batch flushing** for I/O efficiency

### 5. Rich Console Output with Themes

```csharp
// Available themes
SpectreConsoleTheme.ModernColors     // Vibrant colors for modern terminals
SpectreConsoleTheme.TweakedDefaults  // Enhanced default theme
SpectreConsoleTheme.LightBackground  // Optimized for light backgrounds
SpectreConsoleTheme.Dracula          // Popular dark theme
SpectreConsoleTheme.Monokai          // Classic developer theme
SpectreConsoleTheme.SolarizedDark    // Solarized dark variant
SpectreConsoleTheme.OneDark          // VS Code-inspired theme
```

## Advanced Configuration

### Complete File Logger Setup

```csharp
services.AddLogging(builder =>
{
    builder.AddFile(options =>
    {
        // Directory and file naming
        options.LogDirectory = Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData), "MyApp", "Logs");
        options.FileNamePattern = "app-{Timestamp:yyyyMMdd_HHmmss}.log";
        
        // Log levels and behavior
        options.MinLogLevel = LogLevel.Debug;
        options.AppendToFile = false; // Create new file each run
        options.AutoFlush = true;     // Immediate persistence
        
        // Rolling and retention
        options.MaxFileSize = 50 * 1024 * 1024; // 50MB
        options.MaxRetainedFiles = 10;
        
        // Custom formatting
        options.Template = "[{Elapsed:mm\\:ss\\.fff}] {Category}{Scopes} [{Level}]: {Message}{NewLine}{Exception}";
        options.ScopesSeparator = " > ";
        
        // Custom level labels
        options.LevelLabels = new LogLevelLabels
        {
            Trace = "TRCE",
            Debug = "DBUG", 
            Information = "INFO",
            Warning = "WARN",
            Error = "ERR!",
            Critical = "CRIT"
        };
    });
});
```

### Configuration from appsettings.json

```json
{
  "Logging": {
    "File": {
      "LogDirectory": "logs",
      "FileNamePattern": "app-{Timestamp:yyyyMMdd}.log",
      "MinLogLevel": "Information",
      "MaxFileSize": 10485760,
      "MaxRetainedFiles": 5,
      "Template": "{Elapsed} [{Level}] {Category}: {Message}{NewLine}{Exception}"
    }
  }
}
```

```csharp
// Use configuration binding
services.AddLogging(builder =>
{
    builder.AddConfiguration(configuration.GetSection("Logging"));
    builder.AddFile(); // Options come from configuration
});
```

### Buffered Logging for High Throughput

```csharp
// Wrap any logger with buffering
var bufferedLogger = logger.AsBuffered(
    maxQueueSize: 50000,
    allowSynchronousContinuations: false
);

// Or configure buffered file logging
services.AddLogging(builder =>
{
    builder.AddFile(options => { /* config */ });
});

// Get buffered version in your service
public class MyService(ILogger<MyService> logger)
{
    private readonly BufferedLogger _bufferedLogger = logger.AsBuffered();
}
```

## Best Practices

### 1. Structured Logging

```csharp
// ✅ Good - structured with named parameters
logger.LogInformation("User {UserId} logged in from {IPAddress} at {LoginTime}", 
    userId, ipAddress, DateTime.UtcNow);

// ❌ Avoid - string interpolation loses structure
logger.LogInformation($"User {userId} logged in from {ipAddress}");
```

### 2. Exception Handling

```csharp
try
{
    // risky operation
}
catch (Exception ex)
{
    // ✅ Good - includes exception object and context
    logger.LogError(ex, "Failed to process order {OrderId} for customer {CustomerId}", 
        orderId, customerId);
    
    // ❌ Avoid - loses stack trace
    logger.LogError("Error: {Message}", ex.Message);
}
```

### 3. Log Levels

| Level | When to Use | Example |
|-------|-------------|---------|
| `Trace` | Detailed execution flow | Method entry/exit, loop iterations |
| `Debug` | Debugging information | Variable values, conditional branches |  
| `Information` | General application flow | User actions, business events |
| `Warning` | Recoverable issues | Retry attempts, fallback actions |
| `Error` | Handled exceptions | Failed operations with recovery |
| `Critical` | Unrecoverable failures | Application shutdown, data corruption |

### 4. Scoped Logging

```csharp
using (logger.BeginScope("Processing Order {OrderId}", orderId))
{
    logger.LogInformation("Validating order items");
    
    using (logger.BeginScope("Payment Processing"))
    {
        logger.LogInformation("Charging card ending in {LastFourDigits}", lastFour);
        // Logs will include both scopes: "Processing Order 12345 > Payment Processing"
    }
}
```

### 5. Performance Considerations

```csharp
// ✅ Good - check if logging is enabled
if (logger.IsEnabled(LogLevel.Debug))
{
    var expensiveData = CalculateExpensiveMetrics();
    logger.LogDebug("Metrics: {@Metrics}", expensiveData);
}

// ✅ Better - use message delegates for expensive operations
logger.Log(LogLevel.Debug, () => 
    $"Expensive calculation result: {CalculateExpensiveMetrics()}");
```

## Testing with Open.Logging.Extensions

### Unit Testing with ConsoleDelegateLogger

```csharp
[Test]
public void TestLogging()
{
    // Arrange
    var logEntries = new List<PreparedLogEntry>();
    var testLogger = new ConsoleDelegateLogger(
        entry => logEntries.Add(entry),
        LogLevel.Debug,
        "TestCategory");

    // Act
    testLogger.LogInformation("Test message with {Parameter}", "value");

    // Assert
    Assert.That(logEntries, Has.Count.EqualTo(1));
    Assert.That(logEntries[0].Message, Contains.Substring("Test message"));
    Assert.That(logEntries[0].Category, Is.EqualTo("TestCategory"));
}
```

### Integration Testing with In-Memory Files

```csharp
[Test]
public async Task TestFileLogging()
{
    // Arrange
    var tempDir = Path.GetTempPath();
    var logFile = Path.Combine(tempDir, $"test-{Guid.NewGuid()}.log");
    
    var options = new FileFormatterOptions
    {
        LogDirectory = tempDir,
        FileNamePattern = Path.GetFileName(logFile),
        MinLogLevel = LogLevel.Debug,
        AutoFlush = true
    };
    
    // Act
    using var provider = new FileLoggerProvider(Options.Create(options));
    var logger = provider.CreateLogger("TestCategory");
    
    logger.LogInformation("Test message");
    await provider.DisposeAsync(); // Ensure flush
    
    // Assert
    var content = await File.ReadAllTextAsync(logFile);
    Assert.That(content, Contains.Substring("Test message"));
}
```

## Troubleshooting

### Common Issues

**1. Logs not appearing in file**
- Check `MinLogLevel` configuration
- Ensure `AutoFlush = true` for immediate writes
- Verify directory permissions
- Call `await provider.DisposeAsync()` to flush buffers

**2. File rolling not working**
- Confirm `MaxFileSize` is set and > 0
- Check file name pattern includes timestamp: `{Timestamp:yyyyMMdd_HHmmss}`
- Verify retention policy: `MaxRetainedFiles > 0`

**3. Performance issues**
- Use buffered logging for high-throughput scenarios
- Increase channel buffer size if needed
- Consider async logging patterns
- Monitor memory usage with large retention counts

### Debugging Configuration

```csharp
// Enable debug output to see configuration issues
services.AddLogging(builder =>
{
    builder.SetMinimumLevel(LogLevel.Trace);
    builder.AddConsole(); // See internal logging messages
    builder.AddFile(options =>
    {
        // Your file config
    });
});
```

## Migration Guide

### From Built-in ConsoleLogger

```csharp
// Before
services.AddLogging(builder => builder.AddConsole());

// After - with rich formatting
services.AddLogging(builder => 
    builder.AddSpectreConsole(theme: SpectreConsoleTheme.ModernColors));
```

### From Serilog

```csharp
// Before (Serilog)
Log.Logger = new LoggerConfiguration()
    .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

// After (Open.Logging.Extensions)
services.AddLogging(builder =>
{
    builder.AddFile(options =>
    {
        options.LogDirectory = "logs";
        options.FileNamePattern = "app-{Timestamp:yyyyMMdd}.txt";
        options.MaxRetainedFiles = 7; // Keep 7 days
    });
});
```

### From NLog

```csharp
// Before (NLog)
var config = new NLog.Config.LoggingConfiguration();
var fileTarget = new NLog.Targets.FileTarget("logfile")
{
    FileName = "logs/app-${shortdate}.log"
};
config.AddRule(LogLevel.Info, LogLevel.Fatal, fileTarget);

// After (Open.Logging.Extensions)  
services.AddLogging(builder =>
{
    builder.AddFile(options =>
    {
        options.LogDirectory = "logs";
        options.FileNamePattern = "app-{Timestamp:yyyyMMdd}.log";
        options.MinLogLevel = LogLevel.Information;
    });
});
```

## Conclusion

`Open.Logging.Extensions` provides a **complete, enterprise-ready logging solution** that:

- ✅ **Integrates natively** with Microsoft.Extensions.Logging
- ✅ **Supports multiple destinations** (file, console, memory, custom)
- ✅ **Handles enterprise requirements** (rolling, retention, formatting)
- ✅ **Delivers high performance** through async processing
- ✅ **Provides thread safety** out of the box
- ✅ **Offers rich configuration** options and themes

The library is **production-tested**, **well-documented**, and **actively maintained**, making it an ideal choice for .NET applications requiring robust logging capabilities.
