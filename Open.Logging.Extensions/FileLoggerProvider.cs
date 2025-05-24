using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Threading.Channels;

namespace Open.Logging.Extensions;

/// <summary>
/// Provider for logging to files.
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider, IAsyncDisposable
{
    private readonly FileFormatterOptions _options;
    private readonly IExternalScopeProvider _scopeProvider;
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();
    private readonly Channel<PreparedLogEntry> _logChannel;
    private readonly Task _processingTask;
    private StreamWriter? _writer;
    private int _disposed;
    private long _currentFileSize;

    /// <summary>
    /// Initializes a new instance of <see cref="FileLoggerProvider"/>.
    /// </summary>
    /// <param name="options">The options for this provider.</param>
    public FileLoggerProvider(IOptions<FileFormatterOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _scopeProvider = new LoggerExternalScopeProvider();

        // Create the log directory if it doesn't exist
        Directory.CreateDirectory(_options.LogDirectory);

        // Create the file path with timestamp
        var timestamp = _options.Timestamp;
        FilePath = Path.Combine(
            _options.LogDirectory,
            string.Format(CultureInfo.InvariantCulture, _options.FileNameFormatString, timestamp));

        // Create the channel for log operations
        _logChannel = Channel.CreateBounded<PreparedLogEntry>(new BoundedChannelOptions(10000)
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

        // Start the background processing task
        _processingTask = Task.Run(ProcessLogsAsync);

        // Apply retention policy if configured
        if (_options.MaxRetainedFiles > 0)
        {
            Task.Run(ApplyRetentionPolicyAsync);
        }
    }

    /// <summary>
    /// Gets the path to the log file.
    /// </summary>
    public string FilePath { get; private set; }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, nameof(FileLoggerProvider));
        return _loggers.GetOrAdd(categoryName, name => new FileLogger(this, name, _options.MinLogLevel, _scopeProvider));
    }

    private Task<StreamWriter> EnsureWriterAsync()
    {
        if (_writer != null)
        {
            return Task.FromResult(_writer);
        }

        var fileMode = _options.AppendToFile && File.Exists(FilePath)
            ? FileMode.Append
            : FileMode.Create;

        var fileStream = new FileStream(
            FilePath,
            fileMode,
            FileAccess.Write,
            FileShare.Read);

        _writer = new StreamWriter(fileStream, Encoding.UTF8)
        {
            AutoFlush = false // We'll control flushing manually
        };

        // If we're appending, get the current file size
        _currentFileSize = fileMode == FileMode.Append
            ? new FileInfo(FilePath).Length : 0;

        return Task.FromResult(_writer);
    }

    internal ValueTask WriteLogAsync(PreparedLogEntry entry)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, nameof(FileLoggerProvider));

        return _logChannel.Writer.WriteAsync(entry);
    }

    internal void WriteLog(PreparedLogEntry entry)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, nameof(FileLoggerProvider));

        if (_logChannel.Writer.TryWrite(entry)) return;

        _logChannel.Writer.WriteAsync(entry).AsTask().Wait();
    }

    private async Task ProcessLogsAsync()
    {
        do
        {
            while (_logChannel.Reader.TryRead(out var entry))
            {
                await WriteEntryToFileAsync(entry).ConfigureAwait(false);
            }

            if (_writer is not null)
            {
                await _writer.FlushAsync().ConfigureAwait(false);
            }
        }
        while (await _logChannel.Reader.WaitToReadAsync().ConfigureAwait(false));
    }

    private async Task WriteEntryToFileAsync(PreparedLogEntry entry)
    {
        try
        {
            // Check if we need to roll over to a new file based on size
            if (_options.MaxFileSize > 0 && _currentFileSize > _options.MaxFileSize)
            {
                await RollToNewFileAsync().ConfigureAwait(false);
            }

            var writer = await EnsureWriterAsync().ConfigureAwait(false);

            // Format the log entry
            var output = FormatLogEntry(entry);

            // Write to file
            await writer.WriteLineAsync(output).ConfigureAwait(false);

            // Track the size of what we wrote
            _currentFileSize += Encoding.UTF8.GetByteCount(output) + Encoding.UTF8.GetByteCount(Environment.NewLine);

            // We don't flush after each write anymore - the channel processing handles flushing
        }
        catch (Exception ex)
        {
#if DEBUG
            await Console.Error.WriteLineAsync($"Error writing to log file: {ex}").ConfigureAwait(false);
#endif
        }
    }

    private async Task RollToNewFileAsync()
    {
        // Flush and dispose the current writer
        if (_writer != null)
        {
            await _writer.FlushAsync().ConfigureAwait(false);
            await _writer.DisposeAsync().ConfigureAwait(false);
            _writer = null;
        }

        // Create a new file path with a new timestamp
        var timestamp = DateTimeOffset.Now;
        FilePath = Path.Combine(
            _options.LogDirectory,
            string.Format(CultureInfo.InvariantCulture, _options.FileNameFormatString, timestamp));

        // Reset file size for the new file
        _currentFileSize = 0;

        // The writer will be created when needed via EnsureWriterAsync
    }

    private async Task ApplyRetentionPolicyAsync()
    {
        try
        {
            if (_options.MaxRetainedFiles <= 0)
                return;

            // Get all log files in the directory
            var directory = new DirectoryInfo(_options.LogDirectory);
            var logFiles = directory.GetFiles("*.log")
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .ToList();

            if (logFiles.Count <= _options.MaxRetainedFiles)
                return;

            // Delete old files that exceed the retention limit
            foreach (var file in logFiles.Skip(_options.MaxRetainedFiles))
            {
                try
                {
                    file.Delete();
                }
                catch (Exception ex)
                {
#if DEBUG
                    await Console.Error.WriteLineAsync($"Error deleting log file {file.Name}: {ex}").ConfigureAwait(false);
#endif
                }
            }
        }
        catch (Exception ex)
        {
#if DEBUG
            await Console.Error.WriteLineAsync($"Error applying retention policy: {ex}").ConfigureAwait(false);
#endif
        }
    }
    private string FormatLogEntry(PreparedLogEntry entry)
    {
        try
        {
            // Format the elapsed time manually to avoid formatting issues with TimeSpan
            var elapsedString = entry.Elapsed.ToString("c", CultureInfo.InvariantCulture);            // Format the exception part - only include if there's an actual exception
            var exceptionPart = string.IsNullOrWhiteSpace(entry.Exception?.ToString())
                ? string.Empty
                : Environment.NewLine + FormatException(entry.Exception, entry.Category);

            // Format the entry using string.Format with the template format string
            var formatted = string.Format(
                CultureInfo.InvariantCulture,
                _options.TemplateFormatString,
                Environment.NewLine,
                elapsedString,
                entry.Category,
                FormatScopes(entry.Scopes),
                _options.LevelLabels?.GetLabelForLevel(entry.Level) ?? entry.Level.ToString(),
                entry.Message,
                exceptionPart);

            // Trim any trailing whitespace and add consistent spacing between entries
            return formatted.TrimEnd(' ', '\r', '\n') + Environment.NewLine;
        }
        catch (Exception ex)
        {
            // Fall-back in case of formatting errors
            return $"[Error formatting log entry: {ex.Message}] {entry.Level}: {entry.Message}";
        }
    }
    private string FormatScopes(IReadOnlyList<object> scopes)
    {
        if (scopes.Count == 0) return string.Empty;

        var result = new StringBuilder();
        foreach (var scope in scopes.Reverse())
        {
            result.Append(_options.ScopesSeparator);
            result.Append(scope);
        }

        return result.ToString();
    }

    private static string FormatException(Exception exception, string? category = null)
    {
        if (exception == null) return string.Empty;

        var exceptionString = exception.ToString();
        var lines = exceptionString.Split([Environment.NewLine], StringSplitOptions.None);
        var result = new StringBuilder();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            // Check if this line contains " in " (indicating a file path in stack trace)
            if (line.Contains(" in ", StringComparison.Ordinal) && line.TrimStart().StartsWith("at ", StringComparison.Ordinal))
            {
                // Find the " in " part and split there
                var inIndex = line.LastIndexOf(" in ", StringComparison.Ordinal);
                if (inIndex > 0)
                {
                    var beforeIn = line.Substring(0, inIndex);
                    var afterIn = line.Substring(inIndex);

                    // Optimize: if category matches the beginning of the stack trace, simplify it
                    if (!string.IsNullOrEmpty(category) && beforeIn.Contains(category, StringComparison.Ordinal))
                    {
                        // Extract just the class name from the category (last segment)
                        var lastDotIndex = category.LastIndexOf('.');
                        var className = lastDotIndex >= 0 ? category.Substring(lastDotIndex + 1) : category;

                        // Replace the full namespace with just the class name
                        beforeIn = beforeIn.Replace(category, className, StringComparison.Ordinal);
                    }
                    // Add the "at ..." part first
                    result.AppendLine(beforeIn);
                    // Add the "in ..." part with additional indentation and ensure quotes around file path
                    var formattedAfterIn = EnsureFilePathQuoted(afterIn);
                    result.Append("     ").Append(formattedAfterIn);
                }
                else
                {
                    result.Append(line);
                }
            }
            else
            {
                result.Append(line);
            }

            // Add newline for all but the last line
            if (i < lines.Length - 1)
            {
                result.AppendLine();
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Ensures that a file path in a stack trace "in" portion is properly quoted.
    /// </summary>
    /// <param name="inPortion">The "in" portion of a stack trace line (e.g., " in D:\path\file.cs:line 123")</param>
    /// <returns>The properly quoted "in" portion</returns>
    private static string EnsureFilePathQuoted(string inPortion)
    {
        if (string.IsNullOrEmpty(inPortion))
            return inPortion;

        // Look for pattern: " in [optional quote]path[optional quote]:line number"
        const string inPrefix = " in ";

        if (!inPortion.StartsWith(inPrefix, StringComparison.Ordinal))
            return inPortion;

        var pathPart = inPortion.Substring(inPrefix.Length);

        // If already properly quoted (starts and ends with quotes around path+line), return as-is
        if (pathPart.StartsWith('"') && pathPart.EndsWith('"'))
            return inPortion;

        // Find the last colon that's followed by "line " (case insensitive)
        var lineIndex = pathPart.LastIndexOf(":line ", StringComparison.OrdinalIgnoreCase);
        if (lineIndex < 0)
        {
            // No ":line" found, quote the entire path part
            return $"{inPrefix}\"{pathPart}\"";
        }

        // Split into path and line number parts
        var filePath = pathPart.Substring(0, lineIndex);
        var lineNumberPart = pathPart.Substring(lineIndex);

        // Remove any existing quotes from the file path
        filePath = filePath.Trim('"');

        // Return with properly quoted path + line number
        return $"{inPrefix}\"{filePath}{lineNumberPart}\"";
    }

    private async ValueTask DisposeAsyncCore()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0) return;

        // Complete the channel and wait for all logs to be processed
        _logChannel.Writer.TryComplete();
        await _processingTask.ConfigureAwait(false);

        // Dispose the writer
        if (_writer is not null)
        {
            await _writer.FlushAsync().ConfigureAwait(false);
            await _writer.DisposeAsync().ConfigureAwait(false);
            _writer = null;
        }

        Interlocked.Increment(ref _disposed); // 1 = disposing, 2 = disposed.
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DisposeAsyncCore().AsTask().GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
    }
}