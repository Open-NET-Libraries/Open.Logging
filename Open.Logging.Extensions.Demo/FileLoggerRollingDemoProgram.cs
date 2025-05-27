using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Open.Logging.Extensions.FileSystem;
using System.Globalization;
using System.Text;

namespace Open.Logging.Extensions.Demo;

/// <summary>
/// Demonstrates the file logger with file rolling and retention policy.
/// </summary>
internal static class FileLoggerRollingDemoProgram
{
	public static async Task RunAsync(string[] _)
	{
		System.Console.WriteLine("Running File Logger Rolling and Retention Demo");
		System.Console.WriteLine("=============================================");

		var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs", "rolling_demo");
		Directory.CreateDirectory(logDirectory);

		System.Console.WriteLine($"Log files will be saved to: {logDirectory}");

		// Clean previous logs for demo clarity
		foreach (var file in Directory.GetFiles(logDirectory))
		{
			try
			{
				File.Delete(file);
			}
			catch
			{
				// Ignore any deletion errors
			}
		}

		// Create service provider with file logger
		var services = new ServiceCollection();
		services.AddLogging(builder =>
		{
			// Configure file logger with small max entries to demonstrate rolling
			builder.AddFileLogger(options =>
			{
				options.LogDirectory = logDirectory;
				options.FileNamePattern = "rolling_demo_{Timestamp:yyyyMMdd_HHmmss}.log";
				options.MinLogLevel = LogLevel.Debug;
				options.MaxLogEntries = 5; // Small number to trigger rolling quickly
			});

			// Also log to console so we can see what's happening
			builder.AddConsole();
		}); var serviceProvider = services.BuildServiceProvider();
		var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
		var logger = loggerFactory.CreateLogger("FileLoggerRollingDemo");

		// Demonstrate file rolling by writing logs with increasing size
		System.Console.WriteLine("Writing logs to trigger file rolling...");

		for (int i = 1; i <= 10; i++)
		{
			using (logger.BeginScope("Rolling Demo Iteration {Iteration}", i))
			{
				logger.LogInformation("Starting log iteration {Iteration}", i);

				// Log with increasing data size to trigger rolling
				var builder = new StringBuilder();
				for (int j = 0; j < i * 100; j++)
				{
					builder.Append(CultureInfo.InvariantCulture, $"Data chunk {j} for iteration {i}. ");
				}

				logger.LogInformation("Large data: {Data}", builder.ToString());
				logger.LogWarning("Completed iteration {Iteration} with {Size} bytes of data", i, builder.Length);

				// Pause briefly to see the rolling in action
				await Task.Delay(500).ConfigureAwait(false);
			}
		}

		await Task.Delay(1000).ConfigureAwait(false); // Allow time for async operations to complete

		// Show number of log files after the demo
		var finalLogFiles = Directory.GetFiles(logDirectory, "*.log");
		System.Console.WriteLine($"Demo complete. {finalLogFiles.Length} log files created (max 3 retained):");

		foreach (var file in finalLogFiles)
		{
			var fileInfo = new FileInfo(file);
			System.Console.WriteLine($" - {Path.GetFileName(file)} ({fileInfo.Length:N0} bytes)");
		}
	}
}
