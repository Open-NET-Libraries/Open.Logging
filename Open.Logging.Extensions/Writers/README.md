# Writers - Core Logging Architecture

The Writers namespace contains the foundational architecture for the Open.Logging.Extensions library. All logging components are built around the writer pattern defined here.

## 🏗️ Architecture Overview

The writer architecture provides a clean, type-safe, and composable approach to log formatting and output. Instead of tightly coupling formatters to specific output mechanisms, writers can work with any compatible output type.

## 🔑 Core Interfaces

### `ILogEntryWriter<TWriter>`
```csharp
public interface ILogEntryWriter<in TWriter>
{
    void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TWriter writer);
}
```

**Purpose**: Generic interface that provides type safety for different output mechanisms.
- `TWriter` can be `TextWriter`, `IAnsiConsole`, `Stream`, etc.
- Ensures compile-time safety between writers and their target outputs
- Enables flexible composition of logging components

### `ITextLogEntryWriter`
```csharp
public interface ITextLogEntryWriter : ILogEntryWriter<TextWriter>
{
}
```

**Purpose**: Specialized interface for the most common case - text-based logging.
- Inherits from `ILogEntryWriter<TextWriter>`
- Used for console, file, string, and most other text-based outputs
- Simplifies type declarations for text-based scenarios

## 📦 Base Implementations

### `TextLogEntryWriterBase`
Abstract base class for text-based writers that provides:
- Common infrastructure for text output
- Standardized handling of `PreparedLogEntry`
- Base timing and formatting utilities

### `LogEntryWriterBase`
Generic base class for any writer type that provides:
- Common patterns for log entry processing
- Standardized exception handling
- Base implementation scaffolding

## 🎯 Concrete Implementations

### `TemplateTextLogEntryWriter`
A complete implementation that formats log entries using configurable templates.

**Key Features**:
- Template-based formatting with placeholders
- Configurable log level labels
- Cultural formatting support
- Exception formatting integration

**Usage Pattern**:
```csharp
var writer = new TemplateTextLogEntryWriter(options);
writer.Write(logEntry, scopeProvider, textWriter);
```

## 💡 Design Principles

### 1. **Type Safety**
The generic `TWriter` parameter ensures that writers can only be used with compatible output types:
```csharp
ILogEntryWriter<TextWriter> textWriter = new MyTextWriter();
ILogEntryWriter<IAnsiConsole> consoleWriter = new MyConsoleWriter();
// textWriter.Write(..., ansiConsole); // ❌ Compile error!
```

### 2. **Composability**
Writers can be easily composed, wrapped, and combined:
```csharp
var baseWriter = new TemplateTextLogEntryWriter(options);
var colorWriter = new ColoredConsoleWriter(baseWriter);
var bufferedWriter = new BufferedWriter(colorWriter);
```

### 3. **Separation of Concerns**
- **Writers**: Handle formatting and output logic
- **Providers**: Handle lifetime, configuration, and DI integration
- **Loggers**: Handle filtering, scoping, and Microsoft.Extensions.Logging integration

### 4. **Flexibility**
The same writer can work with different output targets:
```csharp
var writer = new TemplateTextLogEntryWriter(options);

// Write to console
writer.Write(entry, scopes, Console.Out);

// Write to file
using var file = File.CreateText("log.txt");
writer.Write(entry, scopes, file);

// Write to string
using var stringWriter = new StringWriter();
writer.Write(entry, scopes, stringWriter);
```

## 🔧 Implementation Guidelines

### Creating a New Writer

1. **Choose the right interface**:
   - Use `ILogEntryWriter<TWriter>` for use with any target.
   - Use `ITextLogEntryWriter` for use with a `TextWriter`

2. **Inherit from base classes**:
   - `TextLogEntryWriterBase` for text writers
   - `LogEntryWriterBase` for other types

3. **Follow the pattern**:
   ```csharp
   public class MyWriter : TextLogEntryWriterBase
   {
       public override void Write(in PreparedLogEntry entry, TextWriter writer)
       {
           // Your custom formatting logic or writer handling here
           writer.WriteLine(FormatEntry(entry));
       }
   }
   ```

### Thread Safety
- Writers themselves don't need to be thread-safe
- Thread safety is handled at the provider/logger level
- Focus on correct, efficient formatting logic

### Performance Considerations
- Minimize allocations in write paths
- Use `ReadOnlySpan<char>` and `Memory<T>` where appropriate
- Cache expensive formatting operations
- Consider pooling for frequently used objects

## ⚠️ Stability Notice

**This namespace is considered stable and foundational.** 

Changes to these interfaces and base classes should be made carefully as they affect the entire logging ecosystem. The writer pattern is the architectural foundation that all other components build upon.
