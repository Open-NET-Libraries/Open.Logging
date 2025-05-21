using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Open.Logging.Extensions.Demo;
using Open.Logging.Extensions.SpectreConsole;
using Spectre.Console;

// Create a service collection for DI
var services = new ServiceCollection();

// Add logging with our Spectre Console formatter
services.AddLogging(logging =>
{
	// Clear default providers
	logging.ClearProviders();

	// Add Spectre console logger with options
	logging.AddSimpleSpectreConsole(options =>
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
	await demoService.RunAsync().ConfigureAwait(false);
}
catch (Exception ex)
{
	AnsiConsole.WriteException(ex);
}

