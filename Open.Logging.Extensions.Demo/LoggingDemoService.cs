using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Open.Logging.Extensions.Demo;

/// <summary>
/// A demo service that showcases logging at different log levels
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="LoggingDemoService"/> class.
/// </remarks>
/// <param name="logger">The logger to use for logging.</param>
internal sealed class LoggingDemoService(ILogger<LoggingDemoService> logger)
{
	/// <summary>
	/// Runs the logging demo asynchronously.
	/// </summary>
	/// <returns>A task representing the asynchronous operation.</returns>
	public async Task RunAsync()
	{
		var rule = new Rule("[bold]Open.Logging.Extensions.SpectreConsole Demo[/]")
		{
			Style = Style.Parse("blue")
		};
		AnsiConsole.Write(rule);
		AnsiConsole.WriteLine();

		// Log at each log level
		logger.LogTrace("This is a Trace level log message");
		logger.LogDebug("This is a Debug level log message");
		logger.LogInformation("This is an Information level log message");
		logger.LogWarning("This is a Warning level log message");
		logger.LogError("This is an Error level log message");
		logger.LogCritical("This is a Critical level log message");

		// Demonstrate exception logging
		try
		{
			throw new InvalidOperationException("This is a demo exception");
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "An error occurred during demo execution");
		}

		// Demonstrate logging with scope
		using (logger.BeginScope("Demo Scope"))
		{
			logger.LogInformation("This log message is within a scope");

			// Nested scope
			using (logger.BeginScope("Nested Scope"))
			{
				logger.LogInformation("This log message is within a nested scope");
			}
		}

		// Demonstrate async logging
		await Task.Delay(100).ConfigureAwait(false);
		logger.LogInformation("This is a log message after an async operation");

		AnsiConsole.WriteLine();
		var endRule = new Rule("[bold]Demo Complete[/]")
		{
			Style = Style.Parse("blue")
		};
		AnsiConsole.Write(endRule);
	}
}
