using Microsoft.Extensions.Logging;

namespace Open.Logging.Extensions;

/// <summary>
/// Extension helpers for logging.
/// </summary>
public static class LoggingExtensions
{
	/// <summary>
	/// Captures the current scope of the logger and returns it as a list of objects.
	/// </summary>
	/// <param name="provider">The logger provider to capture the scope from.</param>
	/// <returns>A list of objects representing the current scope. Empty if none.</returns>
	public static IReadOnlyList<object> CaptureScope(this IExternalScopeProvider? provider)
	{
		if (provider is null) return [];

		var scopes = new List<object>();
		provider.ForEachScope(static (scope, list) =>
		{
			if (scope is null) return;
			list.Add(scope);
		}, scopes);

		return scopes.Count > 0 ? scopes : [];
	}

	/// <summary>
	/// Wraps a logger in a thread-safe buffered logger that processes log messages in the background
	/// </summary>
	/// <inheritdoc cref="BufferedLogger(ILogger, int, bool)"/>
	public static BufferedLogger AsBuffered(
		this ILogger logger, int maxQueueSize = 10000,
		bool allowSynchronousContinuations = false)
		=> new(logger, maxQueueSize, allowSynchronousContinuations);
}
