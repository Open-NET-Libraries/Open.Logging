using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Open.Logging.Extensions.Console;

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
		Action<PreparedLogEntry, TextWriter> handler,
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
			var unsynchronized = handler;
			var sync = new Lock();
			handler = (writer, entry) =>
			{
				lock (sync)
				{
					unsynchronized(writer, entry);
				}
			};
		}

		// Add the formatter directly
		builder.AddConsole(options => options.FormatterName = name);
		builder.Services.AddSingleton<ConsoleFormatter>(sp =>
			new ConsoleDelegateFormatter(name, handler, timestamp));

		return builder;
	}

	/// <inheritdoc cref="AddConsoleDelegateFormatter(ILoggingBuilder, string, Action{PreparedLogEntry, TextWriter}, DateTimeOffset?, bool)"/>
	public static ILoggingBuilder AddConsoleDelegateFormatter(
		this ILoggingBuilder builder,
		string name,
		Action<PreparedLogEntry> handler,
		DateTimeOffset? timestamp = null,
		bool synchronize = false)
		=> AddConsoleDelegateFormatter(
			builder, name,
			handler is null ? null! : (e, _) => handler(e),
			timestamp, synchronize);

	/// <summary>
	/// Adds a <see cref="ConsoleTemplateFormatter"/> to the logging builder with template-based formatting.
	/// </summary>
	/// <param name="builder">The <see cref="ILoggingBuilder"/> to add the formatter to.</param>
	/// <param name="options">The template formatter options to configure the formatter.</param>
	/// <param name="name">The name of the formatter. If not provided, uses a default name based on the template.</param>
	/// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="options"/> is null.</exception>
	public static ILoggingBuilder AddConsoleTemplateFormatter(
		this ILoggingBuilder builder,
		TemplateFormatterOptions options,
		string? name = null)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentNullException.ThrowIfNull(options);

		name ??= $"template-{Guid.NewGuid():N}";

		builder.AddConsole(consoleOptions => consoleOptions.FormatterName = name);
		builder.Services.AddSingleton<ConsoleFormatter>(_ =>
			new ConsoleTemplateFormatter(options, name));

		return builder;
	}

	/// <summary>
	/// Adds a <see cref="ConsoleTemplateFormatter"/> to the logging builder with template-based formatting.
	/// </summary>
	/// <param name="builder">The <see cref="ILoggingBuilder"/> to add the formatter to.</param>
	/// <param name="configure">A delegate to configure the template formatter options.</param>
	/// <param name="name">The name of the formatter. If not provided, uses a default name based on the template.</param>
	/// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="configure"/> is null.</exception>
	public static ILoggingBuilder AddConsoleTemplateFormatter(
		this ILoggingBuilder builder,
		Action<TemplateFormatterOptions> configure,
		string? name = null)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentNullException.ThrowIfNull(configure);

		var options = new TemplateFormatterOptions();
		configure(options);

		return builder.AddConsoleTemplateFormatter(options, name);
	}

	/// <summary>
	/// Adds a <see cref="ConsoleTemplateFormatter"/> to the logging builder with a simple template string.
	/// </summary>
	/// <param name="builder">The <see cref="ILoggingBuilder"/> to add the formatter to.</param>
	/// <param name="template">The template string to use for formatting log entries.</param>
	/// <param name="name">The name of the formatter. If not provided, uses a default name based on the template.</param>
	/// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="template"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="template"/> is empty or whitespace.</exception>
	public static ILoggingBuilder AddConsoleTemplateFormatter(
		this ILoggingBuilder builder,
		string template,
		string? name = null)
	{
		ArgumentNullException.ThrowIfNull(builder);
		
		if (string.IsNullOrWhiteSpace(template))
			throw new ArgumentException("Template must not be null or whitespace.", nameof(template));

		var options = new TemplateFormatterOptions { Template = template };
		return builder.AddConsoleTemplateFormatter(options, name);
	}
}