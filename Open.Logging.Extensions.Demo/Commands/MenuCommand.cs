using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Open.Logging.Extensions.SpectreConsole;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Open.Logging.Extensions.Demo.Commands;

/// <summary>
/// Settings for the menu command.
/// </summary>
internal sealed class MenuCommandSettings : CommandSettings
{
	// No specific settings needed for this demo
}

/// <summary>
/// Command to show the interactive menu (default command).
/// </summary>
internal sealed class MenuCommand : BaseCommand<MenuCommandSettings>
{
	/// <summary>
	/// Executes the menu command.
	/// </summary>
	/// <param name="context">The command context.</param>
	/// <param name="settings">The command settings.</param>
	/// <returns>Exit code (0 for success).</returns>
	protected override async Task<int> ExecuteCommandAsync(CommandContext context, MenuCommandSettings settings)
	{
		return await ShowMainMenuAsync().ConfigureAwait(false);
	}

	/// <summary>
	/// Helper function to show the main menu.
	/// </summary>
	private static async Task<int> ShowMainMenuAsync()
	{
		// Create empty args array for demos that need it
		var emptyArgs = Array.Empty<string>();
		// Define menu choices array to avoid CA1861 warning
		var menuChoices = new[]
		{
			"1. Interactive Spectre Console Formatter Demo",
			"2. File Logger Demo",
			"3. File Logger with Rolling & Retention Demo",
			"4. Test Console Logger Demo",
			"9. Exit"
		};

		while (true)
		{
			System.Console.Clear();
			AnsiConsole.Write(
				new FigletText("Open.Logging")
					.Color(Color.Green)
					.Centered());

			AnsiConsole.WriteLine();
			var choice = AnsiConsole.Prompt(
			  new SelectionPrompt<string>()
				  .Title("Choose a demo to run:")
				  .PageSize(10)
				  .AddChoices(menuChoices));

			switch (choice)
			{
				case "1. Interactive Spectre Console Formatter Demo":
					await FormatterDemoProgram.RunAsync().ConfigureAwait(false);
					PauseForUser();
					break;

				case "2. File Logger Demo":
					FileLoggerDemoProgram.RunDemo();
					PauseForUser();
					break;

				case "3. File Logger with Rolling & Retention Demo":
					await FileLoggerRollingDemoProgram.RunAsync(emptyArgs).ConfigureAwait(false);
					PauseForUser();
					break;

				case "4. Test Console Logger Demo":
					// This will continue to the test demo below
					return await RunTestDemoAsync().ConfigureAwait(false);

				case "9. Exit":
					return 0;
			}
		}
	}

	/// <summary>
	/// Pauses execution until user presses a key.
	/// </summary>
	private static void PauseForUser()
	{
		System.Console.WriteLine();
		System.Console.WriteLine("Press any key to return to the menu...");
		System.Console.ReadKey(true);
	}

	/// <summary>
	/// This wraps the original test demo.
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

		// Always pause when demoing
		PauseForUser();

		return 0;
	}
}
