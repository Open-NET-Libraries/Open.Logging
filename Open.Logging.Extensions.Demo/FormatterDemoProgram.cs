using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Open.Logging.Extensions.SpectreConsole;
using Open.Logging.Extensions.SpectreConsole.Formatters;
using Spectre.Console;

namespace Open.Logging.Extensions.Demo;

/// <summary>
/// Provides interactive demo functionality for the Spectre Console formatters.
/// </summary>
internal static class FormatterDemoProgram
{
	/// <summary>
	/// Runs the interactive formatter demo.
	/// </summary>
	/// <returns>A task representing the asynchronous operation, with a result value of 0 on success.</returns>
	public static async Task<int> RunAsync()
	{
		// Show welcome banner
		AnsiConsole.Write(new FigletText("Formatter Demo")
			.Centered()
			.Color(Color.Green));

		AnsiConsole.WriteLine();

		// Get user choices
		var formatter = GetFormatterChoice();
		var (Name, Theme) = GetThemeChoice();

		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine($"[bold]Selected Formatter:[/] {formatter}");
		AnsiConsole.MarkupLine($"[bold]Selected Theme:[/] {Name}");
		AnsiConsole.WriteLine();

		// Create a service provider with the selected formatter and theme
		var serviceProvider = ConfigureServices(formatter, Theme);        // Run the demo
		var rule = new Rule($"[bold]{formatter} Demo with {Name} Theme[/]")
		{
			Style = Style.Parse("cyan")
		};
		AnsiConsole.Write(rule);
		AnsiConsole.WriteLine();

		await RunDemoWithServiceProvider(serviceProvider).ConfigureAwait(false);

		AnsiConsole.WriteLine();
		var endRule = new Rule("[bold]Demo Complete[/]")
		{
			Style = Style.Parse("green")
		}; AnsiConsole.Write(endRule);

		// Ask if the user wants to try another formatter
		if (await AnsiConsole.ConfirmAsync("Would you like to try another formatter?", defaultValue: true).ConfigureAwait(false))
		{
			AnsiConsole.Clear();
			return await RunAsync().ConfigureAwait(false);
		}

		return 0;
	}

	/// <summary>
	/// Gets the user's choice of formatter.
	/// </summary>
	/// <returns>The selected formatter name.</returns>
	private static string GetFormatterChoice()
	{        // Available formatters
		var formatters = new[]
		{
			"Simple",
			"Minimal Multi-Line",
			"Microsoft-Style",
			"Compact",
			"CallStack",
			"Structured Multi-line"
		};

		return AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("Which [green]formatter[/] would you like to use?")
				.PageSize(10)
				.MoreChoicesText("[grey](Move up and down to reveal more formatters)[/]")
				.AddChoices(formatters));
	}

	/// <summary>
	/// Gets the user's choice of theme.
	/// </summary>
	/// <returns>A tuple containing the theme name and the theme instance.</returns>
	private static (string Name, SpectreConsoleLogTheme Theme) GetThemeChoice()
	{
		// Available themes
		var themes = new[]
		{
			("Default", SpectreConsoleLogTheme.Default),
			("ModernColors", SpectreConsoleLogTheme.ModernColors),
			("TweakedDefaults", SpectreConsoleLogTheme.TweakedDefaults),
			("LightBackground", SpectreConsoleLogTheme.LightBackground),
			("Dracula", SpectreConsoleLogTheme.Dracula),
			("Monokai", SpectreConsoleLogTheme.Monokai),
			("SolarizedDark", SpectreConsoleLogTheme.SolarizedDark),
			("OneDark", SpectreConsoleLogTheme.OneDark)
		};

		var themeNames = new List<string>();
		foreach (var (name, _) in themes)
		{
			themeNames.Add(name);
		}

		var selectedThemeName = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("Which [green]theme[/] would you like to use?")
				.PageSize(10)
				.MoreChoicesText("[grey](Move up and down to reveal more themes)[/]")
				.AddChoices(themeNames));

		foreach (var theme in themes)
		{
			if (theme.Item1 == selectedThemeName)
				return theme;
		}

		return ("Default", SpectreConsoleLogTheme.Default);
	}

	/// <summary>
	/// Configures the DI services with the selected formatter and theme.
	/// </summary>
	/// <param name="formatter">The name of the formatter to use.</param>
	/// <param name="theme">The theme to apply to the formatter.</param>
	/// <returns>A configured service provider.</returns>
	private static ServiceProvider ConfigureServices(string formatter, SpectreConsoleLogTheme theme)
	{
		var services = new ServiceCollection();

		// Add logging with the selected formatter and theme
		services.AddLogging(logging =>
		{
			// Clear default providers
			logging.ClearProviders();

			var options = new SpectreConsoleLogOptions
			{
				Theme = theme
			};

			// Configure the selected formatter
			switch (formatter)
			{
				case "Simple":
					logging.AddSpectreConsole<SimpleSpectreConsoleFormatter>(options);
					break;

				case "Minimal Multi-Line":
					logging.AddSpectreConsole<MinimalMutliLineSpectreConsoleFormatter>(options);
					break;

				case "Microsoft-Style":
					logging.AddSpectreConsole<MicrosoftStyleSpectreConsoleFormatter>(options);
					break;

				case "Compact":
					logging.AddSpectreConsole<CompactSpectreConsoleFormatter>(options);
					break;

				case "CallStack":
					logging.AddSpectreConsole<CallStackSpectreConsoleFormatter>(options);
					break;

				case "Structured Multi-line":
					logging.AddSpectreConsole<StructuredMultilineFormatter>(options);
					break;

				default:
					logging.AddSpectreConsole<SimpleSpectreConsoleFormatter>(options);
					break;
			}

			// Set minimum log level to Trace to see all log levels
			logging.SetMinimumLevel(LogLevel.Trace);
		});

		// Add the demo service
		services.AddTransient<LoggingDemoService>();

		return services.BuildServiceProvider();
	}    /// <summary>
		 /// Runs the demo with the provided service provider.
		 /// </summary>
		 /// <param name="serviceProvider">The service provider containing the configured logger.</param>
		 /// <returns>A task representing the asynchronous operation.</returns>
	private static async Task RunDemoWithServiceProvider(ServiceProvider serviceProvider)
	{
		try
		{
			// Get the demo service and run it
			var demoService = serviceProvider.GetRequiredService<LoggingDemoService>();
			await demoService.RunAsync().ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			AnsiConsole.WriteException(ex);
		}
	}
}
