using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Open.Logging.Extensions.FileSystem;
using Spectre.Console;

namespace Open.Logging.Extensions.Demo;

/// <summary>
/// Demonstrates the file logger with default settings.
/// </summary>
internal sealed class SimpleFileLoggerDemo
{
	/// <summary>
	/// Runs a demonstration of the file logger with all default settings.
	/// </summary>
	public static void RunDemo()
	{
		System.Console.Clear();
		AnsiConsole.Write(
		  new Rule("[bold green]Simple File Logger Demo (Default Settings)[/]")
		  {
			  Style = Style.Parse("green")
		  });
		AnsiConsole.WriteLine();

		string actualLogFilePath = "Unknown";

		AnsiConsole.Status()
			.Spinner(Spinner.Known.Star)
			.SpinnerStyle(Style.Parse("green"))
			.Start("Setting up logging with default options...", ctx =>
			{
				// Configure services
				var services = new ServiceCollection();

				// Add logging with file logger using defaults
				services.AddLogging(
					// Just add the file logger with no configuration - all defaults
					builder => builder.AddFileLogger());

				ctx.Status("Creating service provider...");

				// Build the service provider
				using var serviceProvider = services.BuildServiceProvider();

				// Get the logger factory and create a logger
				var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
				var logger = loggerFactory.CreateLogger<SimpleFileLoggerDemo>();

				// Get expected log file path for display (using default settings)
				var options = new FileLoggerOptions();
				actualLogFilePath = options.GetFormattedFilePath();

				ctx.Status("Writing log entries with default configuration...");

				// Log some messages
				logger.LogInformation("This is a simple information message");
				logger.LogWarning("This is a warning message");

				// Log with a scope
				using (logger.BeginScope("Demo Scope"))
				{
					logger.LogInformation("This message is in a scope");

					try
					{
						throw new InvalidOperationException("This is a test exception");
					}
					catch (Exception ex)
					{
						logger.LogError(ex, "An error occurred");
					}
				}

				ctx.Status("Completing demo...");

				// Service provider disposal will ensure logs are flushed
			});

		// Display information about the log file
		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine("[bold]Default Log Settings:[/]");
		AnsiConsole.WriteLine();

		var options = new FileLoggerOptions();
		AnsiConsole.MarkupLine($"Log directory: [green]{options.LogDirectory}[/]");
		AnsiConsole.MarkupLine($"File pattern: [green]{options.FileNamePattern}[/]");
		AnsiConsole.WriteLine($"Template: {options.Template}");
		AnsiConsole.MarkupLine($"Actual log file: [green]{actualLogFilePath}[/]");

		// Check if the file exists and show its content
		if (File.Exists(actualLogFilePath))
		{
			AnsiConsole.WriteLine();
			AnsiConsole.MarkupLine("[bold]Log file content:[/]");
			AnsiConsole.WriteLine();

			try
			{
				// Display the first few lines of the log file content
				var content = File.ReadAllText(actualLogFilePath);
				AnsiConsole.Write(
					new Panel(content.EscapeMarkup())
					.Border(BoxBorder.Rounded)
					.Header("Log File Content")
				);
			}
			catch (Exception ex)
			{
				AnsiConsole.MarkupLine($"[red]Error reading log file: {ex.Message}[/]");
			}
		}
		else
		{
			AnsiConsole.MarkupLine($"[red]Log file not found at: {actualLogFilePath}[/]");

			// Show example of expected log format
			AnsiConsole.WriteLine();
			AnsiConsole.Markup("[bold]Expected log format with defaults:[/]");
			AnsiConsole.WriteLine();

			var sampleLogContent = """
                00:00:01.123 SimpleFileLoggerDemo
                [info] This is a simple information message

                00:00:01.456 SimpleFileLoggerDemo > Demo Scope
                [info] This message is in a scope

                00:00:01.789 SimpleFileLoggerDemo > Demo Scope
                [err!] An error occurred
                System.InvalidOperationException: This is a test exception
                   at Open.Logging.Extensions.Demo.SimpleFileLoggerDemo...
                """;

			AnsiConsole.Write(
				new Panel(sampleLogContent.EscapeMarkup())
				.Border(BoxBorder.Rounded)
				.Header("Expected Log Format")
			);
		}
	}
}