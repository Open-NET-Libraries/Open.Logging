using System.Diagnostics;
using System.Threading.Channels;

namespace Open.Logging.Extensions.Writers;

/// <summary>
/// A thread-safe buffered wrapper for any ILogger implementation
/// </summary>
public sealed class BufferedLogWriter<TWriter>
	: LogEntryWriterBase<TWriter>
	, IAsyncDisposable
{
	// Channel for background processing of log messages
	private readonly Channel<(PreparedLogEntry entry, TWriter writer)> _logChannel;

	// Background processing task
	private readonly Task _processingTask;
	private readonly Action<PreparedLogEntry, TWriter> _handler;

	/// <summary>
	/// Creates a new buffered logger that delegates to the given writer
	/// </summary>
	/// <param name="handler">The to delegate accept/write logs.</param>
	/// <param name="startTime">Optional start time for the writer. Defaults to current time.</param>
	/// <param name="bufferSize">Maximum size of the buffer.</param>
	/// <param name="allowSynchronousContinuations">Configures whether or not the calling thread may participate in processing the log entries.</param>
	public BufferedLogWriter(
		Action<PreparedLogEntry, TWriter> handler,
		DateTimeOffset? startTime = null,
		int bufferSize = 10000,
		bool allowSynchronousContinuations = false)
		: base(startTime)
	{
		_handler = handler ?? throw new ArgumentNullException(nameof(handler));

		// Create the bounded channel for message buffering
		_logChannel = ChannelFactory
			.Create<(PreparedLogEntry entry, TWriter writer)>(
				bufferSize, singleWriter: false, singleReader: true,
				allowSynchronousContinuations);

		// Start the background processing task
		_processingTask = Task.Run(ProcessLogsAsync);
	}

	/// <inheritdoc />
	public override void Write(in PreparedLogEntry entry, TWriter writer)
	{
		var e = (entry, writer);
		// Try to write to the channel.
		if (_logChannel.Writer.TryWrite(e)) return;
		// If the channel is full:
		// Under the circumstance of a large number of backed up logs, we should create back pressure.
		// If the channel is closed, we throw as it signifies being disposed.
		_logChannel.Writer.WriteAsync(e).AsTask().Wait();
	}

	/// <summary>
	/// Processes log messages from the channel asynchronously
	/// </summary>
	private async Task ProcessLogsAsync()
	{
		await foreach (var (entry, writer) in _logChannel.Reader.ReadAllAsync().ConfigureAwait(false))
		{
			try
			{
				_handler(entry, writer);
			}
			catch (Exception ex)
			{
				Debug.Fail($"The log handler threw an exception: {ex}");
			}
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
