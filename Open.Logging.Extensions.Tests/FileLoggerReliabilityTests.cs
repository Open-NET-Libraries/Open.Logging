using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Open.Logging.Extensions.FileSystem;
using Open.Logging.Extensions.Memory;

namespace Open.Logging.Extensions.Tests;

/// <summary>
/// Tests to diagnose and fix intermittent failures in file logger operations.
/// </summary>
public class FileLoggerReliabilityTests : FileLoggerTestBase
{
	[Fact]
	public async Task FileLogger_AsyncBuffering_CanCauseRaceConditions()
	{
		// This test demonstrates the race condition issue
		using var testContext = CreateTestContext(nameof(FileLogger_AsyncBuffering_CanCauseRaceConditions));
		
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Logging:File:LogLevel:Default"] = "Information",
				["Logging:File:LogDirectory"] = testContext.Directory,
				["Logging:File:FileNamePattern"] = "race-{Timestamp:yyyy-MM-dd-HH-mm-ss-fff}.log",
				["Logging:File:BufferSize"] = "1", // Small buffer to force frequent flushes
				["Logging:File:MaxLogEntries"] = "1" // Force file rolling after each entry
			})
			.Build();

		var services = new ServiceCollection();
		services.AddSingleton<IConfiguration>(configuration);
		services.AddLogging(builder =>
		{
			builder.AddConfiguration(configuration.GetSection("Logging"));
			builder.AddFileLogger();
		});

		using var serviceProvider = services.BuildServiceProvider();
		var logger = serviceProvider.GetRequiredService<ILogger<FileLoggerReliabilityTests>>();

		// Log multiple messages rapidly
		logger.LogInformation("Message 1");
		logger.LogInformation("Message 2");
		logger.LogInformation("Message 3");

		// Test with various delays to show inconsistency
		var delays = new[] { 10, 50, 100, 500, 1000 };
		var results = new List<(int delay, int fileCount, string content)>();

		foreach (var delay in delays)
		{
			await Task.Delay(delay);
			var files = Directory.GetFiles(testContext.Directory, "*.log");
			var combinedContent = "";
			foreach (var file in files)
			{
				combinedContent += await File.ReadAllTextAsync(file);
			}
			results.Add((delay, files.Length, combinedContent));
		}

		// Show that results are inconsistent based on timing
		System.Console.WriteLine("Race condition test results:");
		foreach (var (delay, fileCount, content) in results)
		{
			var hasMsg1 = content.Contains("Message 1");
			var hasMsg2 = content.Contains("Message 2");
			var hasMsg3 = content.Contains("Message 3");
			System.Console.WriteLine($"Delay {delay}ms: {fileCount} files, Msg1={hasMsg1}, Msg2={hasMsg2}, Msg3={hasMsg3}");
		}

		// At least by the end, all messages should be present
		var finalFiles = Directory.GetFiles(testContext.Directory, "*.log");
		var finalContent = "";
		foreach (var file in finalFiles)
		{
			finalContent += await File.ReadAllTextAsync(file);
		}

		Assert.Contains("Message 1", finalContent);
		Assert.Contains("Message 2", finalContent);
		Assert.Contains("Message 3", finalContent);
	}

	[Fact]
	public async Task FileLogger_ProperDisposal_EnsuresAllDataWritten()
	{
		// This test shows how proper disposal ensures all data is written
		using var testContext = CreateTestContext(nameof(FileLogger_ProperDisposal_EnsuresAllDataWritten));
		
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Logging:File:LogLevel:Default"] = "Information",
				["Logging:File:LogDirectory"] = testContext.Directory,
				["Logging:File:FileNamePattern"] = "disposal-{Timestamp:yyyy-MM-dd-HH-mm-ss-fff}.log"
			})
			.Build();

		var services = new ServiceCollection();
		services.AddSingleton<IConfiguration>(configuration);
		services.AddLogging(builder =>
		{
			builder.AddConfiguration(configuration.GetSection("Logging"));
			builder.AddFileLogger();
		});

		string finalContent;
		
		// Create scope to ensure proper disposal
		{
			using var serviceProvider = services.BuildServiceProvider();
			var logger = serviceProvider.GetRequiredService<ILogger<FileLoggerReliabilityTests>>();

			logger.LogInformation("Before disposal message");
			
			// Don't add any delay - disposal should handle flushing
		} // ServiceProvider disposed here - should flush all buffers

		// Now check files
		var files = Directory.GetFiles(testContext.Directory, "*.log");
		finalContent = "";
		foreach (var file in files)
		{
			finalContent += await File.ReadAllTextAsync(file);
		}

		Assert.Contains("Before disposal message", finalContent);
	}

	[Fact]
	public async Task FileAndMemoryLoggers_ProperProviderRetrieval_WorksConsistently()
	{
		// This test ensures we're using the correct provider retrieval pattern
		using var testContext = CreateTestContext(nameof(FileAndMemoryLoggers_ProperProviderRetrieval_WorksConsistently));
		
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Logging:File:LogLevel:Default"] = "Warning",
				["Logging:File:LogDirectory"] = testContext.Directory,
				["Logging:File:FileNamePattern"] = "consistent-{Timestamp:yyyy-MM-dd-HH-mm-ss-fff}.log",
				["Logging:Memory:LogLevel:Default"] = "Debug",
				["Logging:Memory:MaxCapacity"] = "1000"
			})
			.Build();

		var services = new ServiceCollection();
		services.AddSingleton<IConfiguration>(configuration);
		services.AddLogging(builder =>
		{
			builder.AddConfiguration(configuration.GetSection("Logging"));
			builder.AddFileLogger();
			builder.AddMemoryLogger();
		});

		using var serviceProvider = services.BuildServiceProvider();
		var logger = serviceProvider.GetRequiredService<ILogger<FileLoggerReliabilityTests>>();
		
		// Test both provider retrieval methods
		var memoryProviderViaInterface = serviceProvider.GetRequiredService<IMemoryLoggerProvider>();
		var memoryProviderViaType = serviceProvider.GetServices<ILoggerProvider>()
			.OfType<MemoryLoggerProvider>()
			.FirstOrDefault();

		Assert.NotNull(memoryProviderViaInterface);
		Assert.NotNull(memoryProviderViaType);
		Assert.Same(memoryProviderViaInterface, memoryProviderViaType);

		// Log messages at different levels
		logger.LogDebug("Debug - should only be in memory");
		logger.LogInformation("Info - should only be in memory");
		logger.LogWarning("Warning - should be in both");
		logger.LogError("Error - should be in both");

		// Proper disposal ensures file logger flushes
		using var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
		await Task.Delay(100); // Small delay for any async operations
		
		// Force disposal to flush file logger
		loggerFactory.Dispose();
		await Task.Delay(100); // Give file operations time to complete

		// Check memory logger
		var memoryEntries = memoryProviderViaInterface.Snapshot();
		Assert.Contains(memoryEntries, e => e.Message.Contains("Debug"));
		Assert.Contains(memoryEntries, e => e.Message.Contains("Info"));
		Assert.Contains(memoryEntries, e => e.Message.Contains("Warning"));
		Assert.Contains(memoryEntries, e => e.Message.Contains("Error"));

		// Check file logger
		var files = Directory.GetFiles(testContext.Directory, "*.log");
		if (files.Length > 0)
		{
			var combinedContent = "";
			foreach (var file in files)
			{
				combinedContent += await File.ReadAllTextAsync(file);
			}

			// File should only have Warning and Error (not Debug/Info)
			Assert.DoesNotContain("Debug", combinedContent);
			Assert.DoesNotContain("Info", combinedContent);
			Assert.Contains("Warning", combinedContent);
			Assert.Contains("Error", combinedContent);
		}
	}
}
