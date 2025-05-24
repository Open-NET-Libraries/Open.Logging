using Microsoft.Extensions.Logging;

namespace Open.Logging.Extensions;

/// <summary>
/// A lightweight logger that writes log messages to a file through the <see cref="FileLoggerProvider"/>.
/// </summary>
internal sealed class FileLogger : ScopedLoggerBase
{
	private readonly FileLoggerProvider _provider;

	/// <summary>
	/// Initializes a new instance of the <see cref="FileLogger"/> class.
	/// </summary>
	/// <param name="provider">The provider that manages file access.</param>
	/// <param name="category">The category name for messages produced by the logger.</param>
	/// <param name="minLogLevel">The minimum log level.</param>
	/// <param name="scopeProvider">The provider of scoping functionality.</param>
	internal FileLogger(
		FileLoggerProvider provider,
		string category,
		LogLevel minLogLevel,
		IExternalScopeProvider? scopeProvider)
		: base(minLogLevel, category, DateTimeOffset.Now, scopeProvider)
	{
		_provider = provider ?? throw new ArgumentNullException(nameof(provider));
	}

	/// <inheritdoc/>
	protected override void WriteLog(PreparedLogEntry entry)
	{
		// Delegate to the provider for actual file writing
		_provider.WriteLogAsync(entry).AsTask().GetAwaiter().GetResult();
	}
}