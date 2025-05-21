using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System;

namespace Open.Logging.Extensions.SpectreConsole;

/// <summary>
/// Extension methods for adding Spectre.Console logging to Microsoft.Extensions.Logging.
/// </summary>
public static class LoggingBuilderExtensions
{
	/// <summary>
	/// Adds a Spectre.Console logger with the specified options.
	/// </summary>
	/// <param name="logging">The logging builder.</param>
	/// <param name="name">The formatter name.</param>
	/// <returns>The logging builder with the Spectre.Console logger added.</returns>
	public static ILoggingBuilder AddSimpleSpectreConsole(
		this ILoggingBuilder logging, string name = "simple-spectre-console-default")
	{
		if (logging == null)
			throw new ArgumentNullException(nameof(logging));

		var formatter = new SimpleSpectreConsoleFormatter();
		
		// Add the formatter directly
		logging.AddConsole(options => options.FormatterName = name);
		logging.Services.AddSingleton<ConsoleFormatter>(
			new SpectreDelegateFormatter(name, formatter.WriteSynchronized));

		return logging;
	}

	/// <summary>
	/// Adds a Spectre.Console logger with the specified options.
	/// </summary>
	/// <param name="logging">The logging builder.</param>
	/// <param name="configure">A delegate to configure the Spectre Console options.</param>
	/// <param name="name">The formatter name.</param>
	/// <returns>The logging builder with the Spectre.Console logger added.</returns>
	public static ILoggingBuilder AddSpectreConsole(
		this ILoggingBuilder logging,
		Action<SpectreConsoleOptions> configure,
		string name = "spectre-console-default")
	{
		if (logging == null)
			throw new ArgumentNullException(nameof(logging));
		if (configure == null)
			throw new ArgumentNullException(nameof(configure));

		var options = new SpectreConsoleOptions();
		configure(options);

		var formatter = new SimpleSpectreConsoleFormatter(
			options.Theme,
			options.Labels,
			options.Writer);

		// Add the formatter directly
		logging.AddConsole(options => options.FormatterName = name);
		logging.Services.AddSingleton<ConsoleFormatter>(
			new SpectreDelegateFormatter(name, formatter.WriteSynchronized));

		return logging;
	}

	/// <summary>
	/// Adds a Spectre.Console logger with default options.
	/// </summary>
	/// <param name="logging">The logging builder.</param>
	/// <param name="name">The formatter name.</param>
	/// <returns>The logging builder with the Spectre.Console logger added.</returns>
	public static ILoggingBuilder AddSpectreConsole(
		this ILoggingBuilder logging,
		string name = "spectre-console-default")
	{
		if (logging == null)
			throw new ArgumentNullException(nameof(logging));

		var formatter = new SimpleSpectreConsoleFormatter();
		
		// Add the formatter directly
		logging.AddConsole(options => options.FormatterName = name);
		logging.Services.AddSingleton<ConsoleFormatter>(
			new SpectreDelegateFormatter(name, formatter.WriteSynchronized));

		return logging;
	}
}