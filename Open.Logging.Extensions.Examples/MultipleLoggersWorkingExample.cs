using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Open.Logging.Extensions.FileSystem;
using Open.Logging.Extensions.Memory;

namespace Open.Logging.Extensions.Examples;

/// <summary>
/// Demonstrates multiple logger providers working together.
/// This example shows File and Memory loggers configured with different log levels and templates.
/// </summary>
internal static class MultipleLoggersWorkingExample
{
	public static async Task RunAsync()
	{
		Console.WriteLine("=== Multiple Loggers Example ===");
		Console.WriteLine();

		// Create a temporary directory for this example
		var tempDir = Path.Combine(Path.GetTempPath(), "OpenLogging_MultipleExample");
		Directory.CreateDirectory(tempDir);

		try
		{
			// Setup configuration with different settings for each provider
			var configuration = new ConfigurationBuilder()
				.AddInMemoryCollection(new Dictionary<string, string?>
				{
					["Logging:LogLevel:Default"] = "Debug",
					
					// File Logger - Information level and above, detailed format
					["Logging:File:LogLevel:Default"] = "Information", 
					["Logging:File:Directory"] = tempDir,
					["Logging:File:FileNamePattern"] = "example-{Timestamp:yyyy-MM-dd-HH-mm-ss}.log",
					["Logging:File:Template"] = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] FILE {Level}: {Message}",
					
					// Memory Logger - Debug level and above, compact format  
					["Logging:Memory:LogLevel:Default"] = "Debug",
					["Logging:Memory:MaxCapacity"] = "100",
					["Logging:Memory:Template"] = "{Timestamp:HH:mm:ss} MEM[{Level:short}] {Message}"
				})
				.Build();

			var services = new ServiceCollection();
			services.AddSingleton<IConfiguration>(configuration);
			
			// Configure logging with both providers
			services.AddLogging(builder =>
			{
				builder.ClearProviders(); // Start clean
				builder.AddConfiguration(configuration.GetSection("Logging"));
				builder.SetMinimumLevel(LogLevel.Debug); // Allow all levels
				
				builder.AddFileLogger();   // Uses "Logging:File" configuration section
				builder.AddMemoryLogger(); // Uses "Logging:Memory" configuration section
			});

			using var serviceProvider = services.BuildServiceProvider();
			var logger = serviceProvider.GetRequiredService<ILogger<MultipleLoggersWorkingExample>>();
			var memoryProvider = serviceProvider.GetRequiredService<IMemoryLoggerProvider>();

			Console.WriteLine("Logging messages at different levels...");
			
			// Log messages at different levels
			logger.LogTrace("This is a TRACE message - likely filtered out");
			logger.LogDebug("This is a DEBUG message - only in memory");
			logger.LogInformation("This is an INFO message - in both file and memory");
			logger.LogWarning("This is a WARNING message - in both file and memory");
			logger.LogError("This is an ERROR message - in both file and memory");
			
			// Log with scopes
			using (logger.BeginScope("ExampleScope"))
			{
				logger.LogInformation("Message with scope context");
			}

			// Log with structured data
			logger.LogInformation("User {UserId} performed {Action} at {Timestamp}", 
				12345, "Login", DateTimeOffset.Now);

			Console.WriteLine();

			// Demonstrate memory logger access
			var memoryEntries = memoryProvider.Snapshot();
			Console.WriteLine($"Memory Logger captured {memoryEntries.Count} entries:");
			foreach (var entry in memoryEntries.Take(3)) // Show first 3
			{
				Console.WriteLine($"  [{entry.Level}] {entry.Message} (at {entry.Timestamp:HH:mm:ss.fff})");
			}
			if (memoryEntries.Count > 3)
			{
				Console.WriteLine($"  ... and {memoryEntries.Count - 3} more entries");
			}

			Console.WriteLine();

		} // Dispose serviceProvider to flush file logs

		// Give file system time to write
		await Task.Delay(100);

		// Show file logger results
		var logFiles = Directory.GetFiles(tempDir, "*.log");
		if (logFiles.Length > 0)
		{
			Console.WriteLine($"File Logger created: {Path.GetFileName(logFiles[0])}");
			var fileContent = await File.ReadAllTextAsync(logFiles[0]);
			var lines = fileContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
			
			Console.WriteLine($"File contains {lines.Length} log entries:");
			foreach (var line in lines.Take(3)) // Show first 3 lines
			{
				Console.WriteLine($"  {line.Trim()}");
			}
			if (lines.Length > 3)
			{
				Console.WriteLine($"  ... and {lines.Length - 3} more lines");
			}
		}
		else
		{
			Console.WriteLine("No log files were created (check permissions and log levels)");
		}

		Console.WriteLine();
		Console.WriteLine("=== Example Complete ===");
		Console.WriteLine();
		Console.WriteLine("Key Points Demonstrated:");
		Console.WriteLine("✅ File and Memory loggers work simultaneously");
		Console.WriteLine("✅ Different log levels per provider (File: Info+, Memory: Debug+)");
		Console.WriteLine("✅ Custom templates per provider");
		Console.WriteLine("✅ Memory logger provides immediate access to captured logs");
		Console.WriteLine("✅ File logger persists logs to disk with structured format");
		Console.WriteLine("✅ Both loggers handle scopes and structured logging");

		// Cleanup
		try
		{
			Directory.Delete(tempDir, true);
			Console.WriteLine($"✅ Cleaned up temporary directory: {tempDir}");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"⚠️  Could not clean up {tempDir}: {ex.Message}");
		}
	}
}
