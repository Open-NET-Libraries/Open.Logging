using System.Diagnostics;
using System.Threading.Channels;

namespace Open.Logging.Extensions.Writers;

/// <summary>
/// A thread-safe buffered wrapper for any ILogger implementation
/// </summary>
public sealed class BufferedLogWriter
	: IAsyncDisposable
{
	private readonly DateTimeOffset _startTime;
	// Channel for background processing of log messages
	private readonly Channel<PreparedLogEntry> _logChannel;

	// Background processing task
	private readonly Task _processingTask;
	private readonly Action<PreparedLogEntry> _handler;
	private readonly Func<ValueTask>? _onFlushComplete;

	/// <summary>
	/// Creates a new buffered logger that delegates to the given writer
	/// </summary>
	/// <param name="handler">The to delegate accept/write logs.</param>
	/// <param name="startTime">Optional start time for the writer. Defaults to current time.</param>
	/// <param name="bufferSize">Maximum size of the buffer.</param>
	/// <param name="allowSynchronousContinuations">Configures whether or not the calling thread may participate in processing the log entries.</param>
	/// <param name="onFlushComplete">Optional callback invoked when the buffer is flushed.</param>
	public BufferedLogWriter(
		Action<PreparedLogEntry> handler,
		DateTimeOffset? startTime = null,
		int bufferSize = 10000,
		bool allowSynchronousContinuations = false,
		Func<ValueTask>? onFlushComplete = null)
	{
		_handler = handler ?? throw new ArgumentNullException(nameof(handler));

		_startTime = startTime ?? DateTimeOffset.Now;

		// Create the bounded channel for message buffering
		_logChannel = ChannelFactory
			.Create<PreparedLogEntry>(
				bufferSize, singleWriter: false, singleReader: true,
				allowSynchronousContinuations);

		_onFlushComplete = onFlushComplete;

		// Start the background processing task
		_processingTask = Task.Run(ProcessLogsAsync);
	}

	/// <inheritdoc />
	public void Write(in PreparedLogEntry entry)
	{
		// Try to write to the channel.
		if (_logChannel.Writer.TryWrite(entry)) return;
		// If the channel is full:
		// Under the circumstance of a large number of backed up logs, we should create back pressure.
		// If the channel is closed, we throw as it signifies being disposed.
		_logChannel.Writer.WriteAsync(entry).AsTask().Wait();
	}

	/// <summary>
	/// Flushes the buffered log messages to the handler synchronously.
	/// </summary>
	public async ValueTask FlushAsync(CancellationToken cancellationToken = default)
	{
		if (cancellationToken.IsCancellationRequested)
			return;

		bool consumed = false;
		while (_logChannel.Reader.TryRead(out var entry))
		{
			consumed = true;
			try
			{
				_handler(entry);
			}
			catch (Exception ex)
			{
				Debug.Fail($"The log handler threw an exception: {ex}");
			}

			if (cancellationToken.IsCancellationRequested)
				break;
		}

		if (consumed && _onFlushComplete is not null)
			await _onFlushComplete().ConfigureAwait(false);
	}

	/// <summary>
	/// Processes log messages from the channel asynchronously
	/// </summary>
	private async Task ProcessLogsAsync()
	{
		while (await _logChannel.Reader.WaitToReadAsync().ConfigureAwait(false))
		{
			await FlushAsync().ConfigureAwait(false);
			await Task.Yield(); // Yield to allow other tasks to run
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
