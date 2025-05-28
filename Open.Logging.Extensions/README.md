# Open.Logging.Extensions


## Quick Start

### File Logging

Add file logging to your application:

```csharp
services.AddLogging(builder =>
{
    builder.AddFileLogger(options =>
    {
        options.LogDirectory = "logs";
        options.FileNamePattern = "app_{Timestamp:yyyy-MM-dd}.log";
        options.MinLogLevel = LogLevel.Information;
        options.Template = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] {Category}: {Message}{Exception}";
        options.MaxLogEntries = 1000; // Rolling file after 1000 entries
    });
});
```

> **💡 Template Options**: The `Template` property supports rich formatting options. See the [Template Formatting](#-template-formatting) section for complete syntax and examples.

### Memory Logging

Capture logs in memory for unit testing:

```csharp
services.AddLogging(builder =>
{
    builder.AddMemoryLogger(options =>
    {
        options.MaxCapacity = 500;
        options.MinLogLevel = LogLevel.Debug;
        options.IncludeScopes = true;
    });
});

// In your test
var memoryProvider = serviceProvider.GetRequiredService<IMemoryLoggerProvider>();
var logs = memoryProvider.Snapshot(); // Get current logs
memoryProvider.Clear(); // Clear all logs
var drainedLogs = memoryProvider.Drain(); // Get and clear logs
```

### Custom Console Formatters

Create custom console formatting with delegates:

```csharp
services.AddLogging(builder =>
{
    builder.AddConsoleDelegateFormatter("custom", (entry, writer) =>
    {
        writer.WriteLine($"[{entry.Level}] {entry.Message}");
        if (entry.Exception != null)
            writer.WriteLine($"Exception: {entry.Exception.Message}");
    });
});
```

### Console Template Formatting

Use flexible template-based console formatting:

```csharp
services.AddLogging(builder =>
{
    // Simple template
    builder.AddConsoleTemplateFormatter("{Timestamp:HH:mm:ss} [{Level}] {Message}");
    
    // Or with custom configuration
    builder.AddConsoleTemplateFormatter(options =>
    {
        options.Template = "[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Level,-11} {Category}: {Message}";
        options.IncludeScopes = true;
    });
});
```

> **💡 Template Options**: Console template formatting supports rich syntax with placeholders, formatting, and alignment. See the [Template Formatting](#-template-formatting) section for complete documentation.

## Available Logging Providers

### 📁 File Logger (`AddFileLogger`)

**Features:**
- **Rolling Files**: Automatically roll files based on log entry count
- **Template Formatting**: Flexible message templates (see [Template Formatting](#-template-formatting) section)
- **Path Patterns**: Dynamic file naming with timestamp placeholders
- **Buffered Writing**: Configurable buffer sizes for performance
- **Encoding Support**: Configurable text encoding (UTF-8 default)

**Configuration Options:**
- `LogDirectory`: Directory for log files
- `FileNamePattern`: File naming pattern (supports `{Timestamp}` placeholders)
- `Template`: Log message template (see [Template Formatting](#-template-formatting) for full syntax)
- `MinLogLevel`: Minimum log level to capture
- `MaxLogEntries`: Maximum entries per file (0 = no rolling)
- `BufferSize`: Internal buffer size for performance
- `Encoding`: Text encoding for log files

**Key Features:**
- **Rolling Strategy**: Use `MaxLogEntries` to control when new files are created
- **Path Templates**: Include `{Timestamp}` in `FileNamePattern` for automatic file naming
- **Performance**: Configurable `BufferSize` for high-throughput scenarios

> See [Quick Start](#-quick-start) for usage examples and [Template Formatting](#-template-formatting) for complete template syntax.

### 🧠 Memory Logger (`AddMemoryLogger`)

**Features:**
- **In-Memory Storage**: Stores logs in memory for testing scenarios
- **Capacity Management**: Automatic overflow handling with configurable limits
- **Snapshot Operations**: Non-destructive log retrieval
- **Drain Operations**: Retrieve and clear logs atomically
- **Scope Support**: Optional logging scope inclusion

**Configuration Options:**
- `MaxCapacity`: Maximum number of log entries to store (default: 1000)
- `MinLogLevel`: Minimum log level to capture
- `IncludeScopes`: Whether to capture logging scopes

**Key Features:**
- **Testing Integration**: Ideal for unit tests and verification scenarios
- **Atomic Operations**: Thread-safe snapshot and drain operations
- **Overflow Handling**: Automatic capacity management with configurable limits

> See [Quick Start](#-quick-start) for usage examples.

### 🎨 Custom Console Formatters (`AddConsoleDelegateFormatter`)

**Features:**
- **Delegate-Based**: Use simple delegates for custom formatting logic
- **PreparedLogEntry**: Access to optimized log entry structure
- **Thread Safety**: Optional synchronization for thread-safe formatting
- **TextWriter Integration**: Direct writing to console streams

**Key Features:**
- **Delegate-Based**: Simple function-based formatting without complex classes
- **Thread Safety**: Optional synchronization for concurrent scenarios
- **Direct Access**: Full access to `PreparedLogEntry` structure for optimal performance

> See [Quick Start](#-quick-start) for usage examples.

### 📋 Console Template Formatter (`AddConsoleTemplateFormatter`)

**Features:**
- **Template-Based**: Use flexible template strings with property placeholders (see [Template Formatting](#-template-formatting) section)
- **Multiple Syntax**: Support for `{Property}`, `{Property:format}`, and `{Property,alignment:format}` patterns
- **Rich Placeholders**: Built-in support for Timestamp, Level, Category, Message, Exception, Scopes, and Elapsed time
- **Custom Formatting**: Configurable date/time formats, level labels, and separators
- **High Performance**: Optimized template compilation and formatting

**Key Features:**
- **Rich Templates**: Advanced placeholder syntax with formatting and alignment (see [Template Formatting](#-template-formatting))
- **Custom Labels**: Configurable level labels and scope separators
- **High Performance**: Optimized template compilation and minimal allocations

> See [Quick Start](#-quick-start) for usage examples and [Template Formatting](#-template-formatting) for complete template syntax.

## 📝 Template Formatting

Both the **File Logger** (`AddFileLogger`) and **Console Template Formatter** (`AddConsoleTemplateFormatter`) support flexible template-based formatting using a rich placeholder syntax.

### Template Syntax

Templates use placeholder syntax with optional formatting and alignment:

- **Basic**: `{Property}` - Simple property insertion
- **Formatted**: `{Property:format}` - Property with custom format specifier
- **Aligned**: `{Property,alignment}` - Property with field alignment
- **Combined**: `{Property,alignment:format}` - Property with both alignment and format

### Available Template Placeholders

| Placeholder | Description | Example Output |
|-------------|-------------|----------------|
| `{NewLine}` | Environment new line | Typically `\r\n` in Windows. |
| `{Timestamp}` | Log entry timestamp | `2024-01-15 14:30:25.123` |
| `{Timestamp:format}` | Formatted timestamp | `{Timestamp:HH:mm:ss}` → `14:30:25` |
| `{Elapsed}` | Time since logger started | `00:05:30.123` |
| `{Elapsed:format}` | Formatted elapsed time | `{Elapsed:mm:ss.fff}` → `05:30.123` |
| `{Level}` | Log level | `Information` |
| `{Category}` | Logger category | `MyApp.Services.UserService` |
| `{Message}` | Log message | `User login successful` |
| `{Scopes}` | Formatted scopes | ` → Request → User:123` |
| `{Exception}` | Exception details | `System.ArgumentException: Invalid input` |

### Template Examples

```csharp
// Compact format with elapsed time
"{Elapsed:mm:ss.fff} [{Level,4}] {Message}"

// Structured format with scopes
"[{Timestamp:HH:mm:ss}] {Level} {Category}{Scopes} → {Message}"

// JSON-like format
"{{\"timestamp\":\"{Timestamp:O}\",\"level\":\"{Level}\",\"message\":\"{Message}\"}}"

// Aligned columns
"{Timestamp:HH:mm:ss.fff} {Level,-11} {Category,30}: {Message}"

// Custom level formatting  
"{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}"
```

### Format Specifiers

**Timestamp Formats:**
- Standard .NET DateTime format strings apply
- `O` or `o`: ISO 8601 round-trip format
- `yyyy-MM-dd HH:mm:ss.fff`: Custom date/time format
- `HH:mm:ss`: Time only format

**Level Formatting:**
- Uses default labels: `TRACE`, `DEBUG`, `INFO`, `WARN`, `ERROR`, `CRITICAL`
- Customize labels using the `LogLevelLabels` class (see [Advanced Usage](#advanced-usage) section)
- No additional format specifiers - use custom labels for different casing

**Elapsed Time Formats:**
- Standard .NET TimeSpan format strings apply
- `hh:mm:ss.fff`: Hours, minutes, seconds with milliseconds
- `mm:ss.fff`: Minutes and seconds with milliseconds
- `ss.fff`: Seconds with milliseconds

### Alignment

Use positive numbers for right-alignment, negative for left-alignment:

```csharp
"{Level,10}"     // Right-aligned in 10-character field: "Information"
"{Level,-10}"    // Left-aligned in 10-character field:  "Information"
"{Category,30}"  // Right-aligned category name
```

### Advanced Usage

**Console Template Formatter Specific Options:**

When using `AddConsoleTemplateFormatter`, additional configuration options are available:

```csharp
services.AddLogging(builder =>
{
    builder.AddConsoleTemplateFormatter(options =>
    {
        options.Template = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {Level,-11} {Category}: {Message}";
        
        // Custom level labels - control the exact text output for each log level
        options.LevelLabels = new LogLevelLabels
        {
            Trace = "TRACE",      // Default: "TRACE"
            Debug = "DEBUG",      // Default: "DEBUG" 
            Information = "INFO", // Default: "INFO"
            Warning = "WARN",     // Default: "WARN"
            Error = "ERROR",      // Default: "ERROR"
            Critical = "FATAL"    // Default: "CRITICAL"
        };
        
        // Example: Use lowercase labels
        // options.LevelLabels = new LogLevelLabels
        // {
        //     Trace = "trace",
        //     Debug = "debug",
        //     Information = "info",
        //     Warning = "warn",
        //     Error = "error",
        //     Critical = "critical"
        // };
        
        // Example: Use short labels
        // options.LevelLabels = new LogLevelLabels
        // {
        //     Trace = "TRC",
        //     Debug = "DBG",
        //     Information = "INF",
        //     Warning = "WRN",
        //     Error = "ERR",
        //     Critical = "CRT"
        // };
        
        // Scope formatting
        options.ScopesSeparator = " → ";
        options.IncludeScopes = true;
    });
});
```

**File Logger Template Usage:**

```csharp
services.AddLogging(builder =>
{
    builder.AddFileLogger(options =>
    {
        options.Template = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] {Category}: {Message}{NewLine}{Exception}";
        // Other file-specific options...
    });
});
```

## Advanced Configuration

### File Logger Advanced Features

**File Rolling Strategies:**

The File Logger supports automatic file rolling based on entry count. Configure rolling behavior using the `MaxLogEntries` and `FileNamePattern` options:

```csharp
// Single file (no rolling)
services.AddLogging(builder =>
{
    builder.AddFileLogger(options =>
    {
        options.FileNamePattern = "application.log"; // No timestamp = single file
        options.MaxLogEntries = 0; // 0 = no rolling
    });
});

// Rolling files by entry count
services.AddLogging(builder =>
{
    builder.AddFileLogger(options =>
    {
        options.FileNamePattern = "app_{Timestamp:yyyy-MM-dd_HH-mm-ss}.log";
        options.MaxLogEntries = 1000; // New file every 1000 entries
    });
});
```

> **💡 Template Formatting**: For complete template syntax and placeholder documentation, see the [Template Formatting](#-template-formatting) section above.

### Configuration Integration

All loggers support Microsoft.Extensions.Configuration:

```json
{
  "Logging": {
    "File": {
      "LogDirectory": "logs",
      "FileNamePattern": "app_{Timestamp:yyyy-MM-dd}.log",
      "Template": "{Timestamp:HH:mm:ss.fff} [{Level}] {Category}: {Message}{Exception}",
      "MinLogLevel": "Information",
      "MaxLogEntries": 5000,
      "BufferSize": 8192
    },
    "Memory": {
      "MaxCapacity": 1000,
      "MinLogLevel": "Debug",
      "IncludeScopes": true
    }
  }
}
```

```csharp
services.AddLogging(builder =>
{
    builder.AddConfiguration(configuration.GetSection("Logging"));
    builder.AddFileLogger();   // Uses "Logging:File" section
    builder.AddMemoryLogger(); // Uses "Logging:Memory" section
});
```

## Performance Considerations

The writer-based approach typically outperforms the sink approach because:

1. It eliminates unnecessary string allocations
2. It reduces indirection and object creation
3. It enables more specialized output patterns (colorization, structured data, etc.)

**File Logger Performance:**
- Uses buffered writing for high-throughput scenarios
- Configurable buffer sizes (default: optimized for most use cases)
- Asynchronous file operations where possible

**Memory Logger Performance:**
- Thread-safe operations using lock-based synchronization
- Efficient capacity management with configurable limits
- Minimal allocations for log entry storage

For most applications, the default `AddFileLogger()`, `AddMemoryLogger()`, and `AddConsoleDelegateFormatter()` extension methods provide an optimal balance of performance and functionality.

## Architectural Design Decisions

### Moving Beyond the Sink Pattern

In early versions of this library, we implemented a "sink" architecture where log entries flowed through a pipeline into destination sinks (Console, File, Memory). While this pattern offered modularity, it created several challenges:

1. **Duplicate Abstractions**: The sink pattern operates parallel to Microsoft's `ILogger`/`ILoggerProvider` abstractions, creating redundant concepts.
2. **Integration Challenges**: Integration with Microsoft's logging ecosystem required adapters between their abstractions and our sink architecture.
3. **Performance Overhead**: Additional layers of indirection and string allocations impacted performance.

Instead, we've evolved the architecture to align more directly with Microsoft's logging patterns while adding high-performance enhancements:

- Writers that directly output to destinations without intermediate string allocations
- Generic `ILogEntryWriter<TWriter>` interface that allows for type-safe writer implementations
- Implementation of standard Microsoft interfaces (`ILoggerProvider`, `ILogger`) for seamless integration

### Custom Logger Provider Implementations

You might notice that we provide our own `ConsoleLoggerProvider`, `FileLoggerProvider`, etc., despite Microsoft having some similar implementations. Here's why:

1. **Performance Optimizations**: Our implementations use `TextWriter` directly without intermediate string allocations where possible
2. **Enhanced Features**: Our implementations offer additional capabilities like:
   - Memory capture for testing scenarios
   - More flexible templating
   - Custom formatting options
   - Better integration with specialized writers (Spectre.Console, etc.)
3. **Composition Over Inheritance**: Our design favors composition, allowing writers to be mixed and matched with providers
4. **Type-safe Writer Pattern**: Using our generic `ILogEntryWriter<TWriter>` pattern enables specialized writers for different output formats beyond just text
5. **Unified API**: Our providers all follow the same consistent patterns, making it easier to switch between different logging destinations without changing your code

While Microsoft's own providers are perfectly suitable for basic logging, our implementations are optimized for high-performance, advanced formatting, and specialized output scenarios. They're built on the same Microsoft interfaces, so you can use them alongside the built-in providers or as complete replacements.

### TextWriter-Based Architecture

The core of our new design is the `ILogEntryWriter<TWriter>` interface, which:

1. Eliminates unnecessary string allocations by writing directly to the output
2. Provides generic type safety for different writer implementations
3. Supports both Microsoft's `LogEntry<TState>` and our optimized `PreparedLogEntry`