using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Open.Logging.Extensions.Demo;
using Open.Logging.Extensions.SpectreConsole;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Open.Logging.Extensions.FormatterDemo;

public static class FormatterDemoProgram
{
    public static async Task<int> RunAsync()
    {
        // Show welcome banner
        AnsiConsole.Write(new FigletText("Formatter Demo")
            .Centered()
            .Color(Color.Green));
        
        AnsiConsole.WriteLine();

        // Get user choices
        var formatter = GetFormatterChoice();
        var theme = GetThemeChoice();

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]Selected Formatter:[/] {formatter}");
        AnsiConsole.MarkupLine($"[bold]Selected Theme:[/] {theme.Name}");
        AnsiConsole.WriteLine();

        // Create a service provider with the selected formatter and theme
        var serviceProvider = ConfigureServices(formatter, theme.Theme);        // Run the demo
        var rule = new Rule($"[bold]{formatter} Demo with {theme.Name} Theme[/]")
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
        };
        AnsiConsole.Write(endRule);

        // Ask if the user wants to try another formatter
        if (AnsiConsole.Confirm("Would you like to try another formatter?", defaultValue: true))
        {
            AnsiConsole.Clear();
            return await RunAsync().ConfigureAwait(false);
        }

        return 0;
    }

    private static string GetFormatterChoice()
    {
        // Available formatters
        var formatters = new[]
        {
            "Simple",
            "MicrosoftStyle",
            "Compact",
            "JsonStyle",
            "Table",
            "Tree"
        };

        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Which [green]formatter[/] would you like to use?")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more formatters)[/]")
                .AddChoices(formatters));
    }

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
        }        var selectedThemeName = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Which [green]theme[/] would you like to use?")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more themes)[/]")
                .AddChoices(themeNames));

        foreach (var theme in themes)
        {
            if (theme.Item1 == selectedThemeName)
            {
                return theme;
            }
        }

        return ("Default", SpectreConsoleLogTheme.Default);
    }

    private static ServiceProvider ConfigureServices(string formatter, SpectreConsoleLogTheme theme)
    {
        var services = new ServiceCollection();

        // Add logging with the selected formatter and theme
        services.AddLogging(logging =>
        {
            // Clear default providers
            logging.ClearProviders();

            // Custom labels for consistency 
            var labels = new LogLevelLabels
            {
                Information = "INFO-",
                Warning = "WARN!",
                Error = "ERROR",
                Critical = "CRIT!",
            };

            // Configure the selected formatter
            switch (formatter)
            {
                case "Simple":
                    logging.AddSpectreConsole(options =>
                    {
                        options.Theme = theme;
                        options.Labels = labels;
                    });
                    break;
                case "MicrosoftStyle":
                    logging.AddMicrosoftStyleSpectreConsole(options =>
                    {
                        options.Theme = theme;
                        options.Labels = labels;
                    });
                    break;
                case "Compact":
                    logging.AddCompactSpectreConsole(options =>
                    {
                        options.Theme = theme;
                        options.Labels = labels;
                    });
                    break;
                case "JsonStyle":
                    logging.AddJsonStyleSpectreConsole(options =>
                    {
                        options.Theme = theme;
                        options.Labels = labels;
                    });
                    break;
                case "Table":
                    logging.AddTableSpectreConsole(options =>
                    {
                        options.Theme = theme;
                        options.Labels = labels;
                    });
                    break;
                case "Tree":
                    logging.AddTreeSpectreConsole(options =>
                    {
                        options.Theme = theme;
                        options.Labels = labels;
                    });
                    break;
                default:
                    logging.AddSpectreConsole(options =>
                    {
                        options.Theme = theme;
                        options.Labels = labels;
                    });
                    break;
            }

            // Set minimum log level to Trace to see all log levels
            logging.SetMinimumLevel(LogLevel.Trace);
        });

        // Add the demo service
        services.AddTransient<LoggingDemoService>();

        return services.BuildServiceProvider();
    }    private static async Task RunDemoWithServiceProvider(ServiceProvider serviceProvider)
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
