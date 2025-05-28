using Microsoft.Extensions.Logging;

namespace Open.Logging.Extensions.Memory;

/// <summary>
/// Options for configuring the memory logger.
/// </summary>
public record MemoryLoggerOptions
{
	/// <summary>
	/// Gets or sets the maximum capacity of log entries to store.
	/// When exceeded, older entries will be dropped.
	/// </summary>
	/// <remarks>
	/// Set to 0 for unlimited capacity (use with caution as this can lead to memory leaks).
	/// </remarks>
	public int MaxCapacity { get; set; } = 1000;

	/// <summary>
	/// Gets or sets the minimum log level to capture.
	/// </summary>
	public LogLevel MinLogLevel { get; set; } = LogLevel.Trace;

	/// <summary>
	/// Gets or sets a value indicating whether to include scopes in the memory logger.
	/// </summary>
	public bool IncludeScopes { get; set; } = true;
}