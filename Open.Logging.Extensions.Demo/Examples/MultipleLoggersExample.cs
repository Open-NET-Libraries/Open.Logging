using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Open.Logging.Extensions.FileSystem;
using Open.Logging.Extensions.Memory;

namespace Open.Logging.Extensions.Demo.Examples;

/// <summary>
/// Demonstrates using both File and Memory loggers simultaneously.
/// </summary>
internal static class MultipleLoggersExample
{
	/// <summary>
	/// Runs the multiple loggers demonstration.
	/// </summary>
	public static async Task RunAsync()
	{
		// Setup configuration for both loggers
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Logging:LogLevel:Default"] = "Information",

				// File logger configuration
				["Logging:File:LogLevel:Default"] = "Debug",
				["Logging:File:Directory"] = "logs",
				["Logging:File:FileNamePattern"] = "app-{Timestamp:yyyy-MM-dd}.log",
				["Logging:File:Template"] = "[{Timestamp:HH:mm:ss.fff}] {Level,-11} {Category}: {Message}{NewLine}{Exception}",

				// Memory logger configuration  
				["Logging:Memory:LogLevel:Default"] = "Information",
				["Logging:Memory:MaxCapacity"] = "1000",
				["Logging:Memory:Template"] = "MEM [{Level}] {Message}"
			})
			.Build();

		// Setup dependency injection
		var services = new ServiceCollection();
		services.AddSingleton<IConfiguration>(configuration);

		// Add both File and Memory loggers - they both receive the same logs!
		services.AddLogging(builder =>
		{
			builder.AddConfiguration(configuration.GetSection("Logging"));
			builder.AddFileLogger();   // Uses "Logging:File" section
			builder.AddMemoryLogger(); // Uses "Logging:Memory" section
		});

		using var serviceProvider = services.BuildServiceProvider();
		// Get logger and memory provider for demonstration
		var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("MultipleLoggersDemo");
		var memoryLoggerProvider = serviceProvider.GetServices<ILoggerProvider>()
			.OfType<MemoryLoggerProvider>()
			.FirstOrDefault();

		System.Console.WriteLine("=== Multiple Loggers Demo ===");
		System.Console.WriteLine("Both File and Memory loggers will receive the same log entries.");
		System.Console.WriteLine();

		// Log various messages
		logger.LogDebug("This debug message goes to file only (due to different log levels)");
		logger.LogInformation("This info message goes to both file and memory");
		logger.LogWarning("This warning goes to both destinations");

		// Log with scope
		using (logger.BeginScope("DemoScope"))
		{
			logger.LogInformation("This message includes scope information");

			using (logger.BeginScope("NestedScope"))
			{
				logger.LogError("Error message with nested scopes");
			}
		}

		// Log with exception
		try
		{
			throw new InvalidOperationException("Demo exception for logging");
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Caught an exception during demo");
		}
		// Give file logger time to write
		await Task.Delay(100).ConfigureAwait(false);

		// Show memory logger contents
		System.Console.WriteLine("=== Memory Logger Contents ===");
		if (memoryLoggerProvider != null)
		{
			var entries = memoryLoggerProvider.Snapshot(); System.Console.WriteLine($"Memory logger captured {entries.Count} entries:");
			foreach (var entry in entries)
			{
				System.Console.WriteLine($"  [{entry.Level}] {entry.Category}: {entry.Message.Trim()}");
			}
		}

		// Show file logger results
		System.Console.WriteLine();
		System.Console.WriteLine("=== File Logger Results ===");
		var logsDirectory = "logs";
		if (Directory.Exists(logsDirectory))
		{
			var logFiles = Directory.GetFiles(logsDirectory, "*.log");
			if (logFiles.Length > 0)
			{
				System.Console.WriteLine($"Log file created: {logFiles[0]}");
				var content = await File.ReadAllTextAsync(logFiles[0]).ConfigureAwait(false);
				System.Console.WriteLine("File contents:");
				System.Console.WriteLine(content);
			}
			else
			{
				System.Console.WriteLine("No log files found.");
			}
		}
		else
		{
			System.Console.WriteLine("Logs directory not found.");
		}

		System.Console.WriteLine();
		System.Console.WriteLine("Demo completed! Both loggers received the same log entries but formatted them differently.");
	}
}
