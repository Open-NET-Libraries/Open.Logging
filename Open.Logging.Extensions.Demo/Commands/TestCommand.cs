using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Open.Logging.Extensions.SpectreConsole;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Open.Logging.Extensions.Demo.Commands;

/// <summary>
/// Settings for the test demo command.
/// </summary>
internal sealed class TestCommandSettings : CommandSettings
{
	// No specific settings needed for this demo
}

/// <summary>
/// Command to run the test demo with theme demonstrations.
/// </summary>
internal sealed class TestCommand : BaseCommand<TestCommandSettings>
{    /// <summary>
	 /// Executes the test demo command.
	 /// </summary>
	 /// <param name="context">The command context.</param>
	 /// <param name="settings">The command settings.</param>
	 /// <returns>Exit code (0 for success).</returns>
	protected override async Task<int> ExecuteCommandAsync(CommandContext context, TestCommandSettings settings)
	{
		// Extract the test demo logic from Program.cs and run it directly
		return await RunTestDemoAsync().ConfigureAwait(false);
	}

	/// <summary>
	/// Runs the test demo with theme demonstrations.
	/// </summary>
	private static async Task<int> RunTestDemoAsync()
	{
		// Create a service collection for DI
		var services = new ServiceCollection();

		// DO NOT REMOVE THIS SECTION: It verifies the DI Configuration
		#region DI Configuration Test
		// Add logging with our Spectre Console formatter
		services.AddLogging(logging =>
		{
			// Clear default providers
			logging.ClearProviders();

			// Add Spectre console logger with options
			logging.AddSpectreConsole(options =>
			{
				options.Labels = new()
				{
					Information = "INFO-",
					Warning = "WARN!",
					Error = "ERROR",
					Critical = "CRIT!",
				};
				options.Theme = SpectreConsoleLogTheme.Default;
			});

			// Set minimum log level to Trace to see all log levels
			logging.SetMinimumLevel(LogLevel.Trace);
		});

		// Add our demo service
		services.AddTransient<LoggingDemoService>();

		// Build the service provider
		var serviceProvider = services.BuildServiceProvider();

		try
		{
			// Get the demo service from DI and run it
			var demoService = serviceProvider.GetRequiredService<LoggingDemoService>();

			var rule = new Rule("[bold]Open.Logging.Extensions.SpectreConsole Demo[/]")
			{
				Style = Style.Parse("blue")
			};
			AnsiConsole.Write(rule);
			AnsiConsole.WriteLine();
			await demoService.RunAsync().ConfigureAwait(false);
			AnsiConsole.WriteLine();
			var endRule = new Rule("[bold]Demo Complete[/]")
			{
				Style = Style.Parse("blue")
			};
			AnsiConsole.Write(endRule);

		}
		catch (Exception ex)
		{
			AnsiConsole.WriteException(ex);
			return 1;
		}
		#endregion

		// -------------------- Theme Demonstrations --------------------

		// Display a heading for the theme demos
		AnsiConsole.WriteLine();
		var themeDemoRule = new Rule("[bold yellow]Theme Demonstrations[/]")
		{
			Style = Style.Parse("yellow")
		};
		AnsiConsole.Write(themeDemoRule);
		AnsiConsole.WriteLine();

		// Get available themes
		var themes = new[]
		{
			("ModernColors", SpectreConsoleLogTheme.ModernColors),
			("TweakedDefaults", SpectreConsoleLogTheme.TweakedDefaults),
			("LightBackground", SpectreConsoleLogTheme.LightBackground),
			("Dracula", SpectreConsoleLogTheme.Dracula),
			("Monokai", SpectreConsoleLogTheme.Monokai),
			("SolarizedDark", SpectreConsoleLogTheme.SolarizedDark),
			("OneDark", SpectreConsoleLogTheme.OneDark)
		};

		// Demonstrate each theme
		foreach (var (themeName, theme) in themes)
		{
			// Display theme name
			AnsiConsole.WriteLine();
			var themeRule = new Rule($"[bold]Theme: {themeName}[/]")
			{
				Style = Style.Parse("cyan")
			};
			AnsiConsole.Write(themeRule);
			AnsiConsole.WriteLine();

			// Create a logger factory with the current theme
			using var loggerFactory = LoggerFactory.Create(builder =>
			{
				builder.ClearProviders();
				builder.AddSpectreConsole(options =>
				{
					options.Theme = theme;
					// Keep the same custom labels for consistency
					options.Labels = new()
					{
						Information = "INFO-",
						Warning = "WARN!",
						Error = "ERROR",
						Critical = "CRIT!",
					};
				});
				builder.SetMinimumLevel(LogLevel.Trace);
			});

			// Create logger and demo service
			var themeLogger = loggerFactory.CreateLogger<LoggingDemoService>();
			var themeDemo = new LoggingDemoService(themeLogger);

			// Run the demo with this theme
			await themeDemo.RunAsync().ConfigureAwait(false);
		}

		// Final message
		AnsiConsole.WriteLine();
		var finalRule = new Rule("[bold green]All Themes Demonstrated[/]")
		{
			Style = Style.Parse("green")
		};

		AnsiConsole.Write(finalRule);

		return 0;
	}
}
