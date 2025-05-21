using Microsoft.Extensions.Logging;

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
		var formatter = new SimpleSpectreConsoleFormatter();
		return logging.AddSpecializedConsoleFormatter(name, formatter.WriteSynchronized);
	}
}