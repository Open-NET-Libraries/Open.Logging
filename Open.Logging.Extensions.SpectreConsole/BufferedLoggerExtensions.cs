using Microsoft.Extensions.Logging;

namespace Open.Logging.Extensions.SpectreConsole;

/// <summary>
/// Extension methods for <see cref="ILogger"/> to create buffered loggers.
/// </summary>
public static class BufferedLoggerExtensions
{
	/// <summary>
	/// Creates a buffered wrapper around the logger to ensure thread-safe logging.
	/// </summary>
	/// <param name="logger">The logger to buffer.</param>
	/// <param name="maxQueueSize">Maximum size of the buffer queue (default: 10000).</param>
	/// <returns>A buffered logger that can be used with await using.</returns>
	/// <remarks>
	/// Buffered loggers are useful for multi-threaded scenarios where you want to ensure
	/// log entries are properly sequenced and not interleaved.
	/// </remarks>
	public static BufferedLogger AsBuffered(this ILogger logger, int maxQueueSize = 10000)
		=> new(logger, maxQueueSize);
}