using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Channels;

namespace Open.Logging.Extensions;

/// <summary>
/// A thread-safe buffered wrapper for any ILogger implementation
/// </summary>
public sealed class BufferedLogger
	: LoggerBase, ILogger, IAsyncDisposable
{
	// The underlying logger to delegate to
	private readonly ILogger _innerLogger;

	private interface ILogEntry
	{
		void WriteTo(ILogger logger);
	}

	// Channel for background processing of log messages
	private readonly Channel<ILogEntry> _logChannel;

	// Background processing task
	private readonly Task _processingTask;

	/// <summary>
	/// Creates a new buffered logger that delegates to the given logger
	/// </summary>
	/// <param name="innerLogger">The logger to delegate to</param>
	/// <param name="maxQueueSize">Maximum size of the buffer queue</param>
	/// <param name="allowSynchronousContinuations">Configures whether or not the calling thread may participate in processing the log entries.</param>
	public BufferedLogger(
		ILogger innerLogger,
		int maxQueueSize = 10000,
		bool allowSynchronousContinuations = false)
	{
		_innerLogger = innerLogger;

		// Create the bounded channel for message buffering
		_logChannel = Channel.CreateBounded<ILogEntry>(new BoundedChannelOptions(maxQueueSize)
		{
			AllowSynchronousContinuations = allowSynchronousContinuations,
			SingleReader = true,
			SingleWriter = false
		});

		// Start the background processing task
		_processingTask = Task.Run(ProcessLogsAsync);
	}

	/// <inheritdoc />
	public override bool IsEnabled(LogLevel logLevel)
		// Check with the inner logger if this level is enabled
		=> _innerLogger.IsEnabled(logLevel);

	/// <inheritdoc />
	public override IDisposable? BeginScope<TState>(TState state)
		=> _innerLogger.BeginScope(state);

	/// <inheritdoc />
	protected sealed override void WriteLog<TState>(
		LogLevel logLevel, EventId eventId, TState state, Exception? exception,
		Func<TState, Exception?, string> formatter)
	{
		// Create a log entry and queue it for processing
		LogEntry<TState> entry = new(logLevel, eventId, state, exception, formatter);

		// Try to write to the channel, if it fails (e.g., if channel is full and in DropOldest mode) log a warning
		if (!_logChannel.Writer.TryWrite(entry))
			// If the channel is full:
			// Under the circumstance of a large number of backed up logs, we should create back pressure.
			// If the channel is closed, we throw as it signifies being disposed.
			_logChannel.Writer.WriteAsync(entry).AsTask().Wait();
	}

	private sealed class LogEntry<TState>(
		LogLevel logLevel,
		EventId eventId,
		TState state,
		Exception? exception,
		Func<TState, Exception?, string> formatter) : ILogEntry
	{
		public void WriteTo(ILogger logger)
		{
			// Replay the log call to the given logger
			logger.Log(logLevel, eventId, state, exception, formatter);
		}
	}

	/// <summary>
	/// Processes log messages from the channel asynchronously
	/// </summary>
	private async Task ProcessLogsAsync()
	{
		try
		{
			await foreach (var logEntry in _logChannel.Reader.ReadAllAsync().ConfigureAwait(false))
			{
				// Process each log entry by delegating to the inner logger
				logEntry.WriteTo(_innerLogger);
			}
		}
		catch (Exception ex)
		{
			Debug.Fail("Underlying logger threw an exception.");
			_logChannel.Writer.TryComplete(ex);
		}
	}

	/// <summary>
	/// Disposes of the buffered logger, ensuring all queued messages are processed.
	/// </summary>
	public async ValueTask DisposeAsync()
	{
		// Complete the channel
		_logChannel.Writer.TryComplete();
		await _processingTask.ConfigureAwait(false);
	}
}
