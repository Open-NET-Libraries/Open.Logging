using System.Threading.Channels;

namespace Open.Logging.Extensions.Writers;

/// <summary>
/// A thread-safe buffered wrapper for any ILogger implementation
/// </summary>
public sealed class BufferedLogEntryWriter
	: IAsyncDisposable
{
	// Channel for background processing of log messages
	private readonly Channel<PreparedLogEntry> _logChannel;

	// Background processing task
	private readonly Task _processingTask;
	private readonly Func<PreparedLogEntry, ValueTask> _handler;
	private readonly Func<ValueTask>? _onFlushComplete;

	/// <summary>
	/// Creates a new buffered logger that delegates to the given writer
	/// </summary>
	/// <param name="handler">The to delegate accept/write logs.</param>
	/// <param name="bufferSize">Maximum size of the buffer.</param>
	/// <param name="allowSynchronousContinuations">Configures whether or not the calling thread may participate in processing the log entries.</param>
	/// <param name="onFlushComplete">Optional callback invoked when the buffer is flushed.</param>
	public BufferedLogEntryWriter(
		Func<PreparedLogEntry, ValueTask> handler,
		int bufferSize = 10000,
		bool allowSynchronousContinuations = false,
		Func<ValueTask>? onFlushComplete = null)
	{
		_handler = handler ?? throw new ArgumentNullException(nameof(handler));

		// Create the bounded channel for message buffering
		_logChannel = ChannelFactory
			.Create<PreparedLogEntry>(
				bufferSize, singleWriter: false, singleReader: true,
				allowSynchronousContinuations);

		_onFlushComplete = onFlushComplete;

		// Start the background processing task
		_processingTask = Task.Run(ProcessLogsAsync);
	}

	/// <summary>
	/// Writes a log entry to the buffer asynchronously.
	/// </summary>
	public ValueTask WriteAsync(in PreparedLogEntry entry, CancellationToken cancellationToken = default)
		=> _logChannel.Writer.WriteAsync(entry, cancellationToken);

	/// <summary>
	/// Writes a log entry to the buffer synchronously.
	/// </summary>
	/// <remarks>
	/// Will block if the buffer is full until space is available.
	/// </remarks>
	public void Write(in PreparedLogEntry entry)
	{
		if (_logChannel.Writer.TryWrite(entry)) return;
		// If the channel is full, we block until we can write
		_logChannel.Writer.WriteAsync(entry).AsTask().Wait();
	}

	/// <summary>
	/// Flushes the buffered log messages to the handler synchronously.
	/// </summary>
	public async ValueTask FlushAsync(CancellationToken cancellationToken = default)
	{
		// Allow flushing during disposal - don't check IsAlive here
		if (cancellationToken.IsCancellationRequested)
			return;

		bool consumed = false;
		while (_logChannel.Reader.TryRead(out var entry))
		{
			consumed = true;
			await _handler(entry).ConfigureAwait(false);

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
			try
			{
				await FlushAsync().ConfigureAwait(false);
				await Task.Yield(); // Yield to allow other tasks to run
			}
			catch (Exception ex)
			{
				// An exception during flush will be fatal.
				_logChannel.Writer.TryComplete(ex);
			}
		}
	}

	/// <summary>
	/// Disposes of the buffered logger, ensuring all queued messages are processed.
	/// </summary>
	public async ValueTask DisposeAsync()
	{
		_logChannel.Writer.TryComplete();
		await _processingTask.ConfigureAwait(false);
	}
}
