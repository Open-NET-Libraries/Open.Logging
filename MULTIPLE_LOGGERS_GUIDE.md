# Multiple Loggers Configuration Guide

This guide demonstrates how to configure multiple logging providers using Microsoft.Extensions.Logging with the Open.Logging.Extensions library.

## Overview

The Open.Logging.Extensions library supports registering multiple logger providers simultaneously, allowing you to:
- Write logs to multiple destinations (files, memory, console)
- Apply different log levels per provider
- Use custom templates for each provider
- Maintain independent configuration for each provider

## Basic Setup

### 1. Simple Multiple Loggers Registration

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Open.Logging.Extensions.FileSystem;
using Open.Logging.Extensions.Memory;

var services = new ServiceCollection();

services.AddLogging(builder =>
{
    builder.ClearProviders(); // Remove default providers
    
    // Add File Logger
    builder.AddFileLogger(options =>
    {
        options.LogDirectory = @"C:\Logs";
        options.FileNamePattern = "app-{Timestamp:yyyy-MM-dd}.log";
        options.MinLogLevel = LogLevel.Information;
    });
    
    // Add Memory Logger
    builder.AddMemoryLogger(options =>
    {
        options.MaxCapacity = 1000;
        options.MinLogLevel = LogLevel.Debug;
    });
});

using var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

// Both loggers will receive these logs
logger.LogDebug("Debug message - only in memory");
logger.LogInformation("Info message - in both file and memory");
logger.LogError("Error message - in both file and memory");
```

### 2. Configuration-Based Setup

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Setup configuration
var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Logging:LogLevel:Default"] = "Information",
        
        // File Logger Configuration
        ["Logging:File:LogLevel:Default"] = "Information",
        ["Logging:File:Directory"] = @"C:\Logs",
        ["Logging:File:FileNamePattern"] = "app-{Timestamp:yyyy-MM-dd}.log",
        ["Logging:File:Template"] = "[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Level}: {Message}",
        
        // Memory Logger Configuration
        ["Logging:Memory:LogLevel:Default"] = "Debug",
        ["Logging:Memory:MaxCapacity"] = "1000",
        ["Logging:Memory:Template"] = "{Timestamp:HH:mm:ss} [{Level}] {Category}: {Message}"
    })
    .Build();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);

services.AddLogging(builder =>
{
    builder.ClearProviders();
    builder.AddConfiguration(configuration.GetSection("Logging"));
    builder.SetMinimumLevel(LogLevel.Debug); // Allow all levels
    
    builder.AddFileLogger();   // Uses "Logging:File" section
    builder.AddMemoryLogger(); // Uses "Logging:Memory" section
});

using var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
```

## Advanced Scenarios

### 1. Different Log Levels per Provider

```csharp
services.AddLogging(builder =>
{
    builder.ClearProviders();
    
    // File logger - only warnings and errors
    builder.AddFileLogger(options =>
    {
        options.LogDirectory = @"C:\Logs\Errors";
        options.FileNamePattern = "errors-{Timestamp:yyyy-MM-dd}.log";
        options.MinLogLevel = LogLevel.Warning;
    });
    
    // Memory logger - all levels for debugging
    builder.AddMemoryLogger(options =>
    {
        options.MaxCapacity = 5000;
        options.MinLogLevel = LogLevel.Debug;
    });
    
    // Console logger - info and above for user feedback
    builder.AddConsole();
    builder.AddFilter("Console", LogLevel.Information);
});
```

### 2. Custom Templates per Provider

```csharp
var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        // File logger with detailed format
        ["Logging:File:Template"] = 
            "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {Level} | {Category} | {Message}{NewLine}{Exception}",
        
        // Memory logger with compact format
        ["Logging:Memory:Template"] = 
            "{Timestamp:HH:mm:ss} [{Level:short}] {Message}",
            
        // Different log levels
        ["Logging:File:LogLevel:Default"] = "Information",
        ["Logging:Memory:LogLevel:Default"] = "Debug"
    })
    .Build();
```

### 3. Accessing Logger Providers

```csharp
// Get specific logger provider instances
using var serviceProvider = services.BuildServiceProvider();

// Get memory logger provider to access captured logs
var memoryProvider = serviceProvider.GetRequiredService<IMemoryLoggerProvider>();
var capturedLogs = memoryProvider.Snapshot();

// Get all logger providers
var allProviders = serviceProvider.GetServices<ILoggerProvider>();
```

## Best Practices

### 1. Dependency Injection Registration

```csharp
// ✅ CORRECT: Use single instance for both interfaces
services.AddLogging(builder =>
{
    builder.AddMemoryLogger(); // This registers both ILoggerProvider and IMemoryLoggerProvider
});

// Access through DI
var memoryProvider = serviceProvider.GetRequiredService<IMemoryLoggerProvider>();
```

### 2. Configuration Order Matters

```csharp
services.AddLogging(builder =>
{
    builder.ClearProviders();                    // 1. Clear defaults first
    builder.AddConfiguration(config);           // 2. Add configuration
    builder.SetMinimumLevel(LogLevel.Debug);     // 3. Set global minimum level
    builder.AddFileLogger();                     // 4. Add providers
    builder.AddMemoryLogger();
});
```

### 3. Log Level Hierarchy

The effective log level is determined by:
1. Global minimum level (`SetMinimumLevel`)
2. Configuration section level (`Logging:LogLevel:Default`)
3. Provider-specific level (`Logging:Memory:LogLevel:Default`)
4. Category-specific filters

```csharp
// Configuration example showing hierarchy
{
    "Logging": {
        "LogLevel": {
            "Default": "Information",           // Global default
            "Microsoft": "Warning",             // Category-specific
            "System": "Error"                   // Category-specific
        },
        "Memory": {
            "LogLevel": {
                "Default": "Debug"              // Provider-specific
            }
        }
    }
}
```

## Testing Multiple Loggers

### 1. Unit Testing

```csharp
[Fact]
public void BothLoggers_ReceiveSameLogs()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddLogging(builder =>
    {
        builder.ClearProviders();
        builder.AddFileLogger(options => {
            options.LogDirectory = testDirectory;
            options.FileNamePattern = "test.log";
        });
        builder.AddMemoryLogger();
    });

    using var serviceProvider = services.BuildServiceProvider();
    var logger = serviceProvider.GetRequiredService<ILogger<TestClass>>();
    var memoryProvider = serviceProvider.GetRequiredService<IMemoryLoggerProvider>();

    // Act
    logger.LogInformation("Test message");

    // Assert
    var memoryLogs = memoryProvider.Snapshot();
    Assert.Single(memoryLogs);
    Assert.Equal("Test message", memoryLogs[0].Message);
    
    // File assertions would require reading the file after disposal
}
```

### 2. Integration Testing

```csharp
[Fact]
public async Task Integration_MultipleLoggers_WorkTogether()
{
    // Use configuration-based setup for realistic testing
    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Logging:LogLevel:Default"] = "Debug",
            ["Logging:File:Directory"] = testDirectory,
            ["Logging:Memory:MaxCapacity"] = "100"
        })
        .Build();

    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(config);
    services.AddLogging(builder =>
    {
        builder.ClearProviders();
        builder.AddConfiguration(config.GetSection("Logging"));
        builder.SetMinimumLevel(LogLevel.Debug);
        builder.AddFileLogger();
        builder.AddMemoryLogger();
    });

    // Test logging and verification
    using var serviceProvider = services.BuildServiceProvider();
    var logger = serviceProvider.GetRequiredService<ILogger<IntegrationTest>>();
    
    logger.LogInformation("Integration test message");
    
    // Verify both providers received the log
    var memoryProvider = serviceProvider.GetRequiredService<IMemoryLoggerProvider>();
    var logs = memoryProvider.Snapshot();
    Assert.NotEmpty(logs);
}
```

## Common Issues and Solutions

### 1. Memory Logger Not Capturing Logs

**Problem**: Memory logger returns empty collection.

**Solutions**:
- Ensure `ClearProviders()` is called to remove interfering providers
- Set appropriate minimum log level with `SetMinimumLevel()`
- Use `GetRequiredService<IMemoryLoggerProvider>()` instead of `GetServices<ILoggerProvider>()`
- Check configuration hierarchy (global vs provider-specific levels)

### 2. File Logger Not Writing

**Problem**: Log files are not created or are empty.

**Solutions**:
- Ensure the log directory exists and is writable
- Dispose the service provider to flush pending writes
- Add delays in tests to allow file operations to complete
- Check file path patterns for validity

### 3. Configuration Not Applied

**Problem**: Logger configuration from appsettings.json is ignored.

**Solutions**:
- Call `AddConfiguration()` before adding providers
- Ensure configuration sections match provider names ("File", "Memory")
- Use correct configuration keys (see examples above)

## Example: Complete Console Application

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Open.Logging.Extensions.FileSystem;
using Open.Logging.Extensions.Memory;

class Program
{
    static async Task Main(string[] args)
    {
        // Setup configuration
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Setup DI
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddConfiguration(config.GetSection("Logging"));
            builder.SetMinimumLevel(LogLevel.Debug);
            
            builder.AddFileLogger();
            builder.AddMemoryLogger();
            builder.AddConsole();
        });

        // Use loggers
        using var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var memoryProvider = serviceProvider.GetRequiredService<IMemoryLoggerProvider>();

        logger.LogInformation("Application started");
        logger.LogDebug("Debug information");
        logger.LogWarning("Warning message");
        logger.LogError("Error occurred");

        // Access memory logs for debugging
        var memoryLogs = memoryProvider.Snapshot();
        Console.WriteLine($"Captured {memoryLogs.Count} logs in memory");

        logger.LogInformation("Application ending");
    }
}
```

## Configuration File Example (appsettings.json)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "System": "Warning"
    },
    "File": {
      "LogLevel": {
        "Default": "Information"
      },
      "Directory": "logs",
      "FileNamePattern": "app-{Timestamp:yyyy-MM-dd}.log",
      "Template": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {Level} {Category}: {Message}{NewLine}{Exception}",
      "MaxFileSizeBytes": 10485760,
      "RetainedFileCountLimit": 7
    },
    "Memory": {
      "LogLevel": {
        "Default": "Debug"
      },
      "MaxCapacity": 1000,
      "Template": "{Timestamp:HH:mm:ss.fff} [{Level:short}] {Message}"
    }
  }
}
```

This configuration will:
- Write Information+ logs to files with detailed formatting
- Capture Debug+ logs in memory with compact formatting
- Retain 7 days of log files, max 10MB each
- Store up to 1000 log entries in memory

## Summary

The Open.Logging.Extensions library provides robust support for multiple logging providers with:
- ✅ Independent configuration per provider
- ✅ Different log levels per destination  
- ✅ Custom templates and formatting
- ✅ Proper dependency injection integration
- ✅ Thread-safe operations
- ✅ High performance with minimal allocations

Use this guide as a reference for implementing comprehensive logging solutions in your applications.
