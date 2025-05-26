# Open.Logging.Extensions

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

## Migration Guide

If you're upgrading from a previous version that used the sink pattern:

### Old Pattern (Sink-based)
```csharp
services.AddLogging(builder => 
{
    builder.AddSink(new ConsoleSink(
        new TemplateFormatter("{Timestamp} {Message}")
    ));
});
```

### New Pattern (Writer-based)
```csharp
services.AddLogging(builder => 
{
    builder.AddConsoleLogger(template: "{Timestamp} {Message}");
});
```

## Legacy Support

For backward compatibility, we provide adapter classes:

- `FormatterToWriterAdapter`: Adapts the old `ILogEventFormatter` to the new `ILogEntryWriter<TWriter>`
- `WriterToFormatterAdapter`: Adapts the new `ILogEntryWriter<TWriter>` to the old `ILogEventFormatter`

However, we recommend migrating to the new architecture for better performance and maintainability.

## Performance Considerations

The writer-based approach typically outperforms the sink approach because:

1. It eliminates unnecessary string allocations
2. It reduces indirection and object creation
3. It enables more specialized output patterns (colorization, structured data, etc.)

For most applications, the default `AddConsoleLogger()`, `AddFileLogger()`, and `AddMemoryLogger()` extension methods provide an optimal balance of performance and functionality.
