using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Open.Logging.Extensions.Formatters;
using Open.Logging.Extensions.Writers;
using Open.Logging.Extensions.SpectreConsole.Formatters;
using Spectre.Console;
using System.IO;

namespace Open.Logging.Extensions.SpectreConsole;

/// <summary>
/// Adapts a Spectre console formatter to work with both the Microsoft formatter system and the writer architecture.
/// </summary>
public sealed class UnifiedSpectreFormatterAdapter : ConsoleFormatter, ITextLogEntryWriter
{
    private readonly ISpectreConsoleFormatter _spectreFormatter;
    private readonly IExternalScopeProvider? _scopeProvider;
    private readonly DateTimeOffset _startTime;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="UnifiedSpectreFormatterAdapter"/> class.
    /// </summary>
    /// <param name="name">The name of the formatter.</param>
    /// <param name="spectreFormatter">The Spectre console formatter to adapt.</param>
    /// <param name="scopeProvider">Optional scope provider to capture scopes.</param>
    public UnifiedSpectreFormatterAdapter(
        string name,
        ISpectreConsoleFormatter spectreFormatter,
        IExternalScopeProvider? scopeProvider = null)
        : base(name)
    {
        _spectreFormatter = spectreFormatter ?? throw new ArgumentNullException(nameof(spectreFormatter));
        _scopeProvider = scopeProvider;
        _startTime = DateTimeOffset.Now;
    }
    
    /// <inheritdoc />
    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter,
        ConsoleFormatterOptions options)
    {
        Write(in logEntry, scopeProvider, textWriter);
    }

    /// <inheritdoc />
    public void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        string message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception) ?? string.Empty;
        
        if (message.AsSpan().Trim().Length == 0 && logEntry.Exception is null)
            return;
            
        // Create a prepared log entry for the Spectre formatter
        var preparedEntry = new PreparedLogEntry
        {
            StartTime = _startTime,
            Timestamp = DateTimeOffset.Now,
            Level = logEntry.LogLevel,
            Category = logEntry.Category,
            Message = message,
            Exception = logEntry.Exception,
            EventId = logEntry.EventId,
            Scopes = (scopeProvider ?? _scopeProvider).CaptureScope()
        };

        Write(in preparedEntry, textWriter);
    }

    /// <inheritdoc />
    public void Write(in PreparedLogEntry entry, TextWriter textWriter)
    {
        // Use the Spectre formatter to write the entry
        // Since Spectre Console typically writes to the console,
        // we would need to adapt it to write to our TextWriter instead
        
        // This could involve temporarily redirecting Spectre Console output
        // or using a custom writer that forwards to our TextWriter
        
        // For now, we'll directly use the Spectre formatter
        _spectreFormatter.Write(entry);
    }
}
