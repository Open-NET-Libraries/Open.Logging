using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Open.Logging.Extensions;
using System;
using System.IO;

namespace Open.Logging.Extensions.SpectreConsole;

/// <summary>
/// A specialized console formatter for Spectre Console that doesn't require constructor injection.
/// </summary>
public class SpectreDelegateFormatter : ConsoleFormatter
{
    private readonly Action<PreparedLogEntry> _logHandler;
    private readonly DateTimeOffset _timestamp;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpectreDelegateFormatter"/> class.
    /// </summary>
    /// <param name="name">The name of the formatter.</param>
    /// <param name="logHandler">The action to handle the log entry.</param>
    /// <param name="timestamp">The timestamp to use for logging. If null, the current time is used.</param>
    public SpectreDelegateFormatter(
        string name,
        Action<PreparedLogEntry> logHandler,
        DateTimeOffset? timestamp = null)
        : base(name)
    {
        _logHandler = logHandler ?? throw new ArgumentNullException(nameof(logHandler));
        _timestamp = timestamp ?? DateTimeOffset.Now;
    }

    /// <inheritdoc />
    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        string message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception) ?? "";

        if (message.AsSpan().Trim().Length == 0 && logEntry.Exception is null)
            return;

        _logHandler(new PreparedLogEntry
        {
            EventId = logEntry.EventId,
            StartTime = _timestamp,
            Level = logEntry.LogLevel,
            Category = logEntry.Category,
            Scopes = scopeProvider.CaptureScope(),
            Message = message,
            Exception = logEntry.Exception,
        });
    }
}
