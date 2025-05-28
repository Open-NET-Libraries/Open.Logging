using Open.Logging.Extensions.Demo.Commands;
using Spectre.Console.Cli;

namespace Open.Logging.Extensions.Demo;

/// <summary>
/// Main program entry point for the logging demos.
/// </summary>
internal static class Program
{
	/// <summary>
	/// Main entry point for the application.
	/// </summary>
	/// <param name="args">Command line arguments.</param>
	public static async Task<int> Main(string[] args)
	{
		// Create the command app with MenuCommand as the default
		var app = new CommandApp<MenuCommand>();

		// Configure commands
		app.Configure(config =>
		{
			// Add individual demo commands
			config.AddCommand<FileLoggerCommand>("file")
				.WithDescription("Run the file logger demo");

			config.AddCommand<RollingFileLoggerCommand>("rolling")
				.WithDescription("Run the file logger with rolling & retention demo");

			config.AddCommand<FormatterCommand>("formatter")
				.WithDescription("Run the interactive Spectre Console formatter demo");

			config.AddCommand<TestCommand>("test")
				.WithDescription("Run the test console logger demo with theme demonstrations");

			config.AddCommand<ConfigTestCommand>("config")
				.WithDescription("Run the configuration test demo");

			config.AddCommand<MenuCommand>("menu")
				.WithDescription("Show the interactive menu (default)");

#if DEBUG
			config.PropagateExceptions();
			config.ValidateExamples();
#endif
		});

		// Run the command app
		return await app.RunAsync(args).ConfigureAwait(false);
	}
}