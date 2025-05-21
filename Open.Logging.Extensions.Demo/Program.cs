using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Open.Logging.Extensions.SpectreConsole;
using Spectre.Console;

namespace Open.Logging.Extensions.Demo;

internal class Program
{    
    private static async Task Main(string[] _)
    {
        // Create a service collection for DI
        var services = new ServiceCollection();
    
        
        // Add logging with our Spectre Console formatter
        services.AddLogging(logging => 
        {
            // Clear default providers
            logging.ClearProviders();
            
            // Add simple Spectre console logger
            logging.AddSimpleSpectreConsole();
            
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
            await demoService.RunAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
        }
    }
}


/// <summary>
/// A demo service that showcases logging at different log levels
/// </summary>
internal class LoggingDemoService
{
    private readonly ILogger<LoggingDemoService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingDemoService"/> class.
    /// </summary>
    /// <param name="logger">The logger to use for logging.</param>
    public LoggingDemoService(ILogger<LoggingDemoService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Runs the logging demo asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RunAsync()
    {
        var rule = new Rule("[bold]Open.Logging.Extensions.SpectreConsole Demo[/]");
        rule.Style = Style.Parse("blue");
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();
        
#pragma warning disable CA1848 // Use the LoggerMessage delegates
        // Log at each log level
        _logger.LogTrace("This is a Trace level log message");
        _logger.LogDebug("This is a Debug level log message");
        _logger.LogInformation("This is an Information level log message");
        _logger.LogWarning("This is a Warning level log message");
        _logger.LogError("This is an Error level log message");
        _logger.LogCritical("This is a Critical level log message");
        
        // Demonstrate exception logging
        try
        {
            throw new InvalidOperationException("This is a demo exception");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during demo execution");
        }
        
        // Demonstrate logging with scope
        using (_logger.BeginScope("Demo Scope"))
        {
            _logger.LogInformation("This log message is within a scope");
            
            // Nested scope
            using (_logger.BeginScope("Nested Scope"))
            {
                _logger.LogInformation("This log message is within a nested scope");
            }
        }
        
        // Demonstrate async logging
        await Task.Delay(100).ConfigureAwait(false);
        _logger.LogInformation("This is a log message after an async operation");
#pragma warning restore CA1848 // Use the LoggerMessage delegates
        
        AnsiConsole.WriteLine();
        var endRule = new Rule("[bold]Demo Complete[/]");
        endRule.Style = Style.Parse("blue");
        AnsiConsole.Write(endRule);
    }
}
