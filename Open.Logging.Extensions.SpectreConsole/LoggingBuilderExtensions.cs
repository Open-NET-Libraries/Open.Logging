using Microsoft.Extensions.Logging;
using Open.Logging.Extensions.SpectreConsole.Formatters;

namespace Open.Logging.Extensions.SpectreConsole;

/// <summary>
/// Extension methods for adding Spectre.Console logging to Microsoft.Extensions.Logging.
/// </summary>
public static class LoggingBuilderExtensions
{
	/// <summary>
	/// Adds a Spectre.Console logger with the specified options.
	/// </summary>
	/// <param name="builder">The logging builder.</param>
	/// <param name="options">Options to configure the Spectre Console logger.</param>
	/// <param name="name">The formatter name. If not specified will use the type name.</param>
	/// <returns>The logging builder with the Spectre.Console logger added.</returns>
	public static ILoggingBuilder AddSpectreConsole<TFormatter>(
		this ILoggingBuilder builder,
		SpectreConsoleLogOptions? options = null,
		string? name = null)
		where TFormatter : ISpectreConsoleFormatter<TFormatter>
	{
		ArgumentNullException.ThrowIfNull(builder);
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1308 // Normalize strings to uppercase
		name ??= typeof(TFormatter).Name.ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase
#pragma warning restore IDE0079 // Remove unnecessary suppression

		var formatter = TFormatter.Create(
			options?.Theme,
			options?.Labels,
			options?.Writer);

		return builder.AddConsoleDelegateFormatter(name, formatter.Write, synchronize: true);
	}

	/// <param name="builder">The logging builder.</param>
	/// <param name="configure">The delegate to configure the Spectre Console logger.</param>
	/// <param name="name">The formatter name. If not specified will use the type name.</param>
	/// <inheritdoc cref="AddSpectreConsole{TFormatter}(ILoggingBuilder, SpectreConsoleLogOptions?, string?)"/>
	public static ILoggingBuilder AddSpectreConsole<TFormatter>(
		this ILoggingBuilder builder,
		Action<SpectreConsoleLogOptions> configure,
		string? name = null)
		where TFormatter : ISpectreConsoleFormatter<TFormatter>
	{
		var options = new SpectreConsoleLogOptions();
		configure?.Invoke(options);
		return builder.AddSpectreConsole<TFormatter>(options, name);
	}

	/// <inheritdoc cref="AddSpectreConsole{TFormatter}(ILoggingBuilder, SpectreConsoleLogOptions?, string?)"/>
	public static ILoggingBuilder AddSpectreConsole(
		this ILoggingBuilder builder,
		SpectreConsoleLogOptions? options = null,
		string? name = null)
		=> builder.AddSpectreConsole<SimpleSpectreConsoleFormatter>(options, name);

	/// <inheritdoc cref="AddSpectreConsole{TFormatter}(ILoggingBuilder, Action{SpectreConsoleLogOptions}, string?)"/>
	public static ILoggingBuilder AddSpectreConsole(
		this ILoggingBuilder builder,
		Action<SpectreConsoleLogOptions> configure,
		string? name = null)
	{
		var options = new SpectreConsoleLogOptions();
		configure?.Invoke(options);
		return builder.AddSpectreConsole(options, name);
	}
}