using Microsoft.Extensions.Logging;
using Open.Disposable;

namespace Open.Logging.Extensions.Memory;

/// <summary>
/// Interface for accessing memory logger functionality for testing.
/// </summary>
public interface IMemoryLoggerProvider : ILoggerProvider
{
	/// <summary>
	/// Gets a snapshot of the current log entries.
	/// </summary>
	/// <returns>A copy of the current log entries.</returns>
	IReadOnlyList<PreparedLogEntry> Snapshot();

	/// <summary>
	/// Atomically swaps the internal list with a new one, returning the old list to the caller.
	/// </summary>
	/// <returns>The complete list of log entries captured up to this point.</returns>
	IReadOnlyList<PreparedLogEntry> Drain();

	/// <summary>
	/// Clears all log entries from the memory logger.
	/// </summary>
	void Clear();
}

/// <summary>
/// A logger provider that stores log entries in memory for testing purposes.
/// </summary>
public sealed class MemoryLoggerProvider
	: DisposableBase
	, IMemoryLoggerProvider
{
	// Shared storage for all loggers created by this provider
	private List<PreparedLogEntry> _logEntries = [];

	// Lock object for thread safety
	private readonly Lock _sync = new();

	// Start time used for all loggers
	private readonly DateTimeOffset _startTime = DateTimeOffset.Now;

	/// <summary>
	/// Creates a new memory logger provider.
	/// </summary>
	public MemoryLoggerProvider()
	{
	}

	/// <inheritdoc/>
	public ILogger CreateLogger(string categoryName)
	{
		AssertIsAlive();
		return new MemoryLogger(categoryName, _logEntries, _sync, _startTime);
	}

	/// <inheritdoc/>
	public IReadOnlyList<PreparedLogEntry> Snapshot()
	{
		lock (_sync)
		{
			AssertIsAlive();
			return _logEntries.ToArray();
		}
	}

	/// <inheritdoc/>
	public IReadOnlyList<PreparedLogEntry> Drain()
	{
		lock (_sync)
		{
			AssertIsAlive();
			var result = _logEntries;
			_logEntries = [];
			return result;
		}
	}

	/// <inheritdoc/>
	public void Clear()
	{
		lock (_sync)
		{
			AssertIsAlive();
			_logEntries.Clear();
		}
	}

	/// <inheritdoc/>
	protected override void OnDispose()
	{
		// By locking we ensure that no log entries are left dangling.
		lock (_sync)
		{
			_logEntries = null!;
		}
	}

	/// <summary>
	/// Memory logger implementation that stores log entries in memory.
	/// </summary>
	private sealed class MemoryLogger(
		string category,
		List<PreparedLogEntry> logEntries,
		Lock sync,
		DateTimeOffset startTime)
		: PreparedLoggerBase(category, LogLevel.Trace, new LoggerExternalScopeProvider(), startTime)
	{
		/// <inheritdoc/>
		protected override void WriteLog(PreparedLogEntry entry)
		{
			lock (sync)
			{
				logEntries.Add(entry);
			}
		}
	}
}