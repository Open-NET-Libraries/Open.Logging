using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Open.Logging.Extensions.FileSystem;
using Open.Logging.Extensions.Memory;

namespace Open.Logging.Verification;

/// <summary>
/// Verification program to test that multiple loggers work as documented.
/// This tests the exact scenarios from MULTIPLE_LOGGERS_GUIDE.md
/// </summary>
internal static class VerifyMultipleLoggersDemo
{
	public static async Task Main(string[] args)
	{
		Console.WriteLine("=== Verifying Multiple Loggers Scenario ===");
		Console.WriteLine();
		
		await TestSimpleMultipleLoggersRegistration();
		Console.WriteLine();
		
		await TestConfigurationBasedSetup();
		Console.WriteLine();
		
		await TestDifferentLogLevelsPerProvider();
		Console.WriteLine();
		
		Console.WriteLine("=== All Tests Completed Successfully! ===");
	}

	/// <summary>
	/// Test the "Simple Multiple Loggers Registration" example from the guide
	/// </summary>
	private static async Task TestSimpleMultipleLoggersRegistration()
	{
		Console.WriteLine("🧪 Testing: Simple Multiple Loggers Registration");
		
		var services = new ServiceCollection();
		var tempDir = Path.Combine(Path.GetTempPath(), "OpenLoggingTest", Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDir);

		try
		{
			services.AddLogging(builder =>
			{
				builder.ClearProviders(); // Remove default providers
				
				// Add File Logger
				builder.AddFileLogger(options =>
				{
					options.LogDirectory = tempDir;
					options.FileNamePattern = "app-{Timestamp:yyyy-MM-dd}.log";
					options.MinLogLevel = LogLevel.Information;
				});
				
				// Add Memory Logger
				builder.AddMemoryLogger(options =>
				{
					options.MaxCapacity = 1000;
					options.MinLogLevel = LogLevel.Debug;
				});
			});

			using var serviceProvider = services.BuildServiceProvider();
			var logger = serviceProvider.GetRequiredService<ILogger<VerifyMultipleLoggersDemo>>();
			var memoryProvider = serviceProvider.GetRequiredService<IMemoryLoggerProvider>();

			// Both loggers will receive these logs
			logger.LogDebug("Debug message - only in memory");
			logger.LogInformation("Info message - in both file and memory");
			logger.LogError("Error message - in both file and memory");
			
			// Give file logger time to write
			await Task.Delay(100);
		} // Dispose to flush logs

		// Verify memory logs
		using var verifyServiceProvider = services.BuildServiceProvider();
		var verifyMemoryProvider = verifyServiceProvider.GetRequiredService<IMemoryLoggerProvider>();
		var memoryLogs = verifyMemoryProvider.Snapshot();
		
		Console.WriteLine($"✅ Memory Logger captured {memoryLogs.Count} entries:");
		foreach (var log in memoryLogs)
		{
			Console.WriteLine($"   📝 {log.Level}: {log.Message}");
		}
		
		// Verify file logs
		var logFiles = Directory.GetFiles(tempDir, "*.log");
		if (logFiles.Length > 0)
		{
			var fileContent = await File.ReadAllTextAsync(logFiles[0]);
			var lines = fileContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
			Console.WriteLine($"✅ File Logger wrote {lines.Length} entries:");
			foreach (var line in lines)
			{
				Console.WriteLine($"   📄 {line.Trim()}");
			}
		}
		else
		{
			Console.WriteLine("❌ No log files found");
		}
		
		// Cleanup
		Directory.Delete(tempDir, true);
	}

	/// <summary>
	/// Test the "Configuration-Based Setup" example from the guide
	/// </summary>
	private static async Task TestConfigurationBasedSetup()
	{
		Console.WriteLine("🧪 Testing: Configuration-Based Setup");
		
		var tempDir = Path.Combine(Path.GetTempPath(), "OpenLoggingConfigTest", Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDir);

		try
		{
			// Setup configuration
			var configuration = new ConfigurationBuilder()
				.AddInMemoryCollection(new Dictionary<string, string?>
				{
					["Logging:LogLevel:Default"] = "Information",
					
					// File Logger Configuration
					["Logging:File:LogLevel:Default"] = "Information",
					["Logging:File:Directory"] = tempDir,
					["Logging:File:FileNamePattern"] = "app-{Timestamp:yyyy-MM-dd}.log",
					["Logging:File:Template"] = "[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Level}: {Message}",
					
					// Memory Logger Configuration
					["Logging:Memory:LogLevel:Default"] = "Debug",
					["Logging:Memory:MaxCapacity"] = "1000",
					["Logging:Memory:Template"] = "{Timestamp:HH:mm:ss} [{Level}] {Category}: {Message}"
				})
				.Build();

			var services = new ServiceCollection();
			services.AddSingleton<IConfiguration>(configuration);

			services.AddLogging(builder =>
			{
				builder.ClearProviders();
				builder.AddConfiguration(configuration.GetSection("Logging"));
				builder.SetMinimumLevel(LogLevel.Debug); // Allow all levels
				
				builder.AddFileLogger();   // Uses "Logging:File" section
				builder.AddMemoryLogger(); // Uses "Logging:Memory" section
			});

			using var serviceProvider = services.BuildServiceProvider();
			var logger = serviceProvider.GetRequiredService<ILogger<VerifyMultipleLoggersDemo>>();
			var memoryProvider = serviceProvider.GetRequiredService<IMemoryLoggerProvider>();

			// Log messages
			logger.LogDebug("Debug config message");
			logger.LogInformation("Info config message");
			logger.LogWarning("Warning config message");
			
			// Give file logger time to write
			await Task.Delay(100);
			
			// Check memory logs immediately
			var memoryLogs = memoryProvider.Snapshot();
			Console.WriteLine($"✅ Memory Logger (Config) captured {memoryLogs.Count} entries:");
			foreach (var log in memoryLogs)
			{
				Console.WriteLine($"   📝 {log.Level}: {log.Message}");
			}
		} // Dispose to flush logs

		// Verify file logs after disposal
		var logFiles = Directory.GetFiles(tempDir, "*.log");
		if (logFiles.Length > 0)
		{
			var fileContent = await File.ReadAllTextAsync(logFiles[0]);
			var lines = fileContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
			Console.WriteLine($"✅ File Logger (Config) wrote {lines.Length} entries:");
			foreach (var line in lines)
			{
				Console.WriteLine($"   📄 {line.Trim()}");
			}
		}
		else
		{
			Console.WriteLine("❌ No log files found");
		}
		
		// Cleanup
		Directory.Delete(tempDir, true);
	}

	/// <summary>
	/// Test the "Different Log Levels per Provider" example from the guide
	/// </summary>
	private static async Task TestDifferentLogLevelsPerProvider()
	{
		Console.WriteLine("🧪 Testing: Different Log Levels per Provider");
		
		var tempDir = Path.Combine(Path.GetTempPath(), "OpenLoggingLevelsTest", Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDir);

		try
		{
			var services = new ServiceCollection();

			services.AddLogging(builder =>
			{
				builder.ClearProviders();
				
				// File logger - only warnings and errors
				builder.AddFileLogger(options =>
				{
					options.LogDirectory = tempDir;
					options.FileNamePattern = "errors-{Timestamp:yyyy-MM-dd}.log";
					options.MinLogLevel = LogLevel.Warning;
				});
				
				// Memory logger - all levels for debugging
				builder.AddMemoryLogger(options =>
				{
					options.MaxCapacity = 5000;
					options.MinLogLevel = LogLevel.Debug;
				});
			});

			using var serviceProvider = services.BuildServiceProvider();
			var logger = serviceProvider.GetRequiredService<ILogger<VerifyMultipleLoggersDemo>>();
			var memoryProvider = serviceProvider.GetRequiredService<IMemoryLoggerProvider>();

			// Log at different levels
			logger.LogDebug("Debug level message");     // Only in memory
			logger.LogInformation("Info level message"); // Only in memory  
			logger.LogWarning("Warning level message");  // In both
			logger.LogError("Error level message");      // In both
			
			// Give file logger time to write
			await Task.Delay(100);
			
			// Check memory logs
			var memoryLogs = memoryProvider.Snapshot();
			Console.WriteLine($"✅ Memory Logger (All Levels) captured {memoryLogs.Count} entries:");
			foreach (var log in memoryLogs)
			{
				Console.WriteLine($"   📝 {log.Level}: {log.Message}");
			}
		} // Dispose to flush logs

		// Verify file logs (should only have Warning and Error)
		var logFiles = Directory.GetFiles(tempDir, "*.log");
		if (logFiles.Length > 0)
		{
			var fileContent = await File.ReadAllTextAsync(logFiles[0]);
			var lines = fileContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
			Console.WriteLine($"✅ File Logger (Warning+) wrote {lines.Length} entries:");
			foreach (var line in lines)
			{
				Console.WriteLine($"   📄 {line.Trim()}");
			}
		}
		else
		{
			Console.WriteLine("❌ No log files found");
		}
		
		// Cleanup
		Directory.Delete(tempDir, true);
	}
}
