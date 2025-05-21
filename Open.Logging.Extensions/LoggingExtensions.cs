using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

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

	/// <summary>
	/// Adds a specialized console formatter to the logging builder.
	/// </summary>
	public static ILoggingBuilder AddSpecializedConsoleFormatter(
		this ILoggingBuilder builder,
		string name,
		Action<TextWriter, PreparedLogEntry> handler,
		Action<ConsoleFormatterOptions>? configureOptions = null,
		DateTimeOffset? timestamp = null,
		bool synchronize = false)
	{
		if (builder == null)
			throw new ArgumentNullException(nameof(builder));

		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException("Formatter name must be provided.", nameof(name));

		if (handler == null)
			throw new ArgumentNullException(nameof(handler));

		if (synchronize)
		{
			handler = (writer, entry) =>
			{
				lock (writer)
				{
					handler(writer, entry);
				}
			};
		}

		builder.Services.AddSingleton<ConsoleFormatter>(sp =>
			new ConsoleDelegateFormatter(name, handler, timestamp));

		builder.AddConsoleFormatter<ConsoleDelegateFormatter, ConsoleFormatterOptions>();

		if (configureOptions != null)
		{
			builder.Services.Configure(name, configureOptions);
		}

		builder.AddConsole(options => options.FormatterName = name);

		return builder;
	}

	/// <inheritdoc cref="AddSpecializedConsoleFormatter(ILoggingBuilder, string, Action{TextWriter, PreparedLogEntry}, Action{ConsoleFormatterOptions}?, DateTimeOffset?, bool)"/>
	public static ILoggingBuilder AddSpecializedConsoleFormatter(
		this ILoggingBuilder builder,
		string name,
		Action<PreparedLogEntry> handler,
		Action<ConsoleFormatterOptions>? configureOptions = null,
		DateTimeOffset? timestamp = null,
		bool synchronize = false)
		=> AddSpecializedConsoleFormatter(
			builder, name,
			handler is null ? null! : (_, e) => handler(e),
			configureOptions, timestamp, synchronize);
}
