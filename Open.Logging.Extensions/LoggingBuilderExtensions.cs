using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Open.Logging.Extensions;

/// <summary>
/// Extension methods for adding Spectre.Console logging to Microsoft.Extensions.Logging.
/// </summary>
public static class LoggingBuilderExtensions
{
	/// <summary>
	/// Adds a <see cref="ConsoleDelegateFormatter"/> to the logging builder.
	/// </summary>
	public static ILoggingBuilder AddConsoleDelegateFormatter(
		this ILoggingBuilder builder,
		string name,
		Action<TextWriter, PreparedLogEntry> handler,
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
			var sync = new Lock();
			handler = (writer, entry) =>
			{
				lock (sync)
				{
					handler(writer, entry);
				}
			};
		}

		// Add the formatter directly
		builder.AddConsole(options => options.FormatterName = name);
		builder.Services.AddSingleton<ConsoleFormatter>(sp =>
			new ConsoleDelegateFormatter(name, handler, timestamp));

		return builder;
	}

	/// <inheritdoc cref="AddConsoleDelegateFormatter(ILoggingBuilder, string, Action{TextWriter, PreparedLogEntry}, DateTimeOffset?, bool)"/>
	public static ILoggingBuilder AddConsoleDelegateFormatter(
		this ILoggingBuilder builder,
		string name,
		Action<PreparedLogEntry> handler,
		DateTimeOffset? timestamp = null,
		bool synchronize = false)
		=> AddConsoleDelegateFormatter(
			builder, name,
			handler is null ? null! : (_, e) => handler(e),
			timestamp, synchronize);
}