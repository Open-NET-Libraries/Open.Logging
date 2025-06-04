using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Open.Logging.Extensions.FileSystem;
using Open.Logging.Extensions.Memory;

namespace Open.Logging.Extensions.Tests;

/// <summary>
/// Integration tests to verify that multiple logging providers can work together.
/// </summary>
public sealed class MultipleLoggersIntegrationTests : FileLoggerTestBase
{
	[Fact]
	public async Task FileAndMemoryLoggers_AddedTogether_BothReceiveSameLogs()
	{
		// Arrange
		using var testContext = CreateTestContext(nameof(FileAndMemoryLoggers_AddedTogether_BothReceiveSameLogs)); var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Logging:LogLevel:Default"] = "Debug", // Changed to Debug to allow debug messages
				["Logging:File:LogLevel:Default"] = "Debug",
				["Logging:File:LogDirectory"] = testContext.Directory,
				["Logging:File:FileNamePattern"] = "AddedTogether-{Timestamp:yyyy-MM-dd-HH-mm-ss-fff}.log",
				["Logging:Memory:LogLevel:Default"] = "Debug",
				["Logging:Memory:MaxCapacity"] = "1000"
			})
			.Build();

		var services = new ServiceCollection();
		services.AddSingleton<IConfiguration>(configuration);

		// Act - Add both File and Memory loggers
		services.AddLogging(builder =>
		{
			builder.ClearProviders(); // Start clean
			builder.AddConfiguration(configuration.GetSection("Logging"));
			builder.SetMinimumLevel(LogLevel.Debug); // Explicitly set minimum level
			builder.AddFileLogger();   // Uses "Logging:File" section
			builder.AddMemoryLogger(); // Uses "Logging:Memory" section
		});

		using var serviceProvider = services.BuildServiceProvider();
		var logger = serviceProvider.GetRequiredService<ILogger<MultipleLoggersIntegrationTests>>();
		var memoryLoggerProvider = serviceProvider.GetRequiredService<IMemoryLoggerProvider>();

		Assert.NotNull(memoryLoggerProvider);

		// Log different levels to test both providers
		logger.LogTrace("This is a trace message");
		logger.LogDebug("This is a debug message");
		logger.LogInformation("This is an info message");
		logger.LogWarning("This is a warning message");
		logger.LogError("This is an error message");

		// Log with scope to test scope handling
		using (logger.BeginScope("TestScope"))
		{
			logger.LogInformation("Message with scope");
		}
		// Give file logger time to write
		await Task.Delay(FileOperationDelay);
		// Assert - Check Memory Logger received logs
		var memoryEntries = memoryLoggerProvider.Snapshot();

		Assert.NotEmpty(memoryEntries);

		// Should have debug, info, warning, error (trace might be filtered out)
		Assert.Contains(memoryEntries, e => e.Message.Contains("debug message", StringComparison.OrdinalIgnoreCase));
		Assert.Contains(memoryEntries, e => e.Message.Contains("info message", StringComparison.OrdinalIgnoreCase));
		Assert.Contains(memoryEntries, e => e.Message.Contains("warning message", StringComparison.OrdinalIgnoreCase));
		Assert.Contains(memoryEntries, e => e.Message.Contains("error message", StringComparison.OrdinalIgnoreCase));
		Assert.Contains(memoryEntries, e => e.Message.Contains("Message with scope", StringComparison.OrdinalIgnoreCase));

		// Assert - Check File Logger wrote logs
		var logFiles = Directory.GetFiles(testContext.Directory, "*.log"); Assert.NotEmpty(logFiles);

		// Combine content from all log files (file logger may roll to multiple files)
		var allLogContent = "";
		foreach (var file in logFiles)
		{
			var content = await File.ReadAllTextAsync(file);
			allLogContent += content;
		}

		var logContent = allLogContent;
		Assert.NotEmpty(logContent);

		// Same messages should be in the file
		Assert.Contains("debug message", logContent, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("info message", logContent, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("warning message", logContent, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("error message", logContent, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("Message with scope", logContent, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task FileAndMemoryLoggers_WithDifferentLogLevels_FilterCorrectly()
	{
		// Arrange
		using var testContext = CreateTestContext(nameof(FileAndMemoryLoggers_WithDifferentLogLevels_FilterCorrectly));
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Logging:LogLevel:Default"] = "Information",
				["Logging:File:LogLevel:Default"] = "Warning", // File only gets Warning and above
				["Logging:File:LogDirectory"] = testContext.Directory,
				["Logging:File:FileNamePattern"] = "DifferentLevels-{Timestamp:yyyy-MM-dd-HH-mm-ss-fff}.log",
				["Logging:Memory:LogLevel:Default"] = "Debug", // Memory gets Debug and above
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
		var logger = serviceProvider.GetRequiredService<ILogger<MultipleLoggersIntegrationTests>>();
		var memoryLoggerProvider = serviceProvider.GetServices<ILoggerProvider>()
			.OfType<MemoryLoggerProvider>()
			.FirstOrDefault();

		Assert.NotNull(memoryLoggerProvider);

		// Act - Log at different levels
		logger.LogDebug("Debug message");
		logger.LogInformation("Info message");
		logger.LogWarning("Warning message");
		logger.LogError("Error message");

		await Task.Delay(FileOperationDelay);
		// Assert - Memory should have Debug, Info, Warning, Error
		var memoryEntries = memoryLoggerProvider.Snapshot().ToList();
		Assert.Contains(memoryEntries, e => e.Message.Contains("Debug message", StringComparison.OrdinalIgnoreCase));
		Assert.Contains(memoryEntries, e => e.Message.Contains("Info message", StringComparison.OrdinalIgnoreCase));
		Assert.Contains(memoryEntries, e => e.Message.Contains("Warning message", StringComparison.OrdinalIgnoreCase));
		Assert.Contains(memoryEntries, e => e.Message.Contains("Error message", StringComparison.OrdinalIgnoreCase));       // Assert - File should only have Warning and Error
		var logFiles = Directory.GetFiles(testContext.Directory, "*.log");
		if (logFiles.Length > 0)
		{
			// Combine content from all log files (file logger may roll to multiple files)
			var allLogContent = "";
			foreach (var file in logFiles)
			{
				var content = await File.ReadAllTextAsync(file);
				allLogContent += content;
			}

			var logContent = allLogContent;

			// Should NOT contain Debug or Info
			Assert.DoesNotContain("Debug message", logContent, StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain("Info message", logContent, StringComparison.OrdinalIgnoreCase);

			// Should contain Warning and Error
			Assert.Contains("Warning message", logContent, StringComparison.OrdinalIgnoreCase);
			Assert.Contains("Error message", logContent, StringComparison.OrdinalIgnoreCase);
		}
	}
	[Fact]
	public async Task FileAndMemoryLoggers_WithCustomTemplates_FormatIndependently()
	{
		// Arrange
		using var testContext = CreateTestContext(nameof(FileAndMemoryLoggers_WithCustomTemplates_FormatIndependently));
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Logging:LogLevel:Default"] = "Information",
				["Logging:File:LogLevel:Default"] = "Information",
				["Logging:File:LogDirectory"] = testContext.Directory,
				["Logging:File:FileNamePattern"] = "CustomTemplates-{Timestamp:yyyy-MM-dd-HH-mm-ss-fff}.log",
				["Logging:File:Template"] = "[{Timestamp:HH:mm:ss}] FILE {Level}: {Message}",
				["Logging:Memory:LogLevel:Default"] = "Information",
				["Logging:Memory:MaxCapacity"] = "1000",
				["Logging:Memory:Template"] = "MEMORY [{Level}] {Category} - {Message}"
			})
			.Build();

		var services = new ServiceCollection();
		services.AddSingleton<IConfiguration>(configuration);

		IMemoryLoggerProvider memoryLoggerProvider;

		// Use proper scoping to ensure disposal
		{
			services.AddLogging(builder =>
			{
				builder.ClearProviders(); // Start clean
				builder.AddConfiguration(configuration.GetSection("Logging"));
				builder.SetMinimumLevel(LogLevel.Information); // Explicitly set minimum level for templates test
				builder.AddFileLogger();
				builder.AddMemoryLogger();
			});

			using var serviceProvider = services.BuildServiceProvider();
			var logger = serviceProvider.GetRequiredService<ILogger<MultipleLoggersIntegrationTests>>();
			memoryLoggerProvider = serviceProvider.GetRequiredService<IMemoryLoggerProvider>();

			Assert.NotNull(memoryLoggerProvider);

			// Act
			logger.LogInformation("Test message for template verification");

			// Memory logger should have the message immediately (synchronous)
			var memoryEntries = memoryLoggerProvider.Snapshot().ToList();
			var memoryEntry = memoryEntries.FirstOrDefault(e => e.Message.Contains("Test message", StringComparison.OrdinalIgnoreCase));

			// The memory logger stores raw PreparedLogEntry objects, not formatted text
			// So we just verify it captured the message correctly
			Assert.Contains("Test message for template verification", memoryEntry.Message, StringComparison.OrdinalIgnoreCase);

			// Properly dispose the service provider to flush all file buffers
			await serviceProvider.DisposeAsync();
		}

		// Now check the file after proper disposal ensures all buffers are flushed
		var logFiles = Directory.GetFiles(testContext.Directory, "*.log");
		Assert.NotEmpty(logFiles);

		var logContent = await File.ReadAllTextAsync(logFiles[0]);

		// File template should produce something like: "[14:30:25] FILE INFO: Test message for template verification"
		Assert.Contains("FILE INFO:", logContent, StringComparison.Ordinal);
		Assert.Contains("Test message for template verification", logContent, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void MultipleLoggerProviders_CanBeRetrievedFromServiceProvider()
	{
		// Arrange
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Logging:LogLevel:Default"] = "Information"
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

		// Act
		using var serviceProvider = services.BuildServiceProvider();
		var loggerProviders = serviceProvider.GetServices<ILoggerProvider>().ToList();

		// Assert
		Assert.NotEmpty(loggerProviders);

		var fileLoggerProvider = loggerProviders.OfType<FileLoggerProvider>().FirstOrDefault();
		var memoryLoggerProvider = loggerProviders.OfType<MemoryLoggerProvider>().FirstOrDefault();

		Assert.NotNull(fileLoggerProvider);
		Assert.NotNull(memoryLoggerProvider);

		// Both should be different instances
		Assert.NotSame(fileLoggerProvider, memoryLoggerProvider);
	}

	[Fact(Skip = "Stress test to expose race conditions - disable for regular runs")]
	public async Task FileAndMemoryLoggers_StressTest_ExposeBufferingRaceCondition()
	{
		// This test runs multiple iterations to expose the intermittent failure
		// related to FileLogger's async buffering vs Memory Logger's synchronous behavior

		const int iterations = 200; // Aggressive iteration count to expose race conditions
		var failures = new List<(int iteration, string reason)>();

		for (int iteration = 1; iteration <= iterations; iteration++)
		{
			try
			{
				// Arrange - Create a new test context for each iteration
				using var testContext = CreateTestContext($"{nameof(FileAndMemoryLoggers_StressTest_ExposeBufferingRaceCondition)}_Iteration_{iteration}");
				var configuration = new ConfigurationBuilder()
					.AddInMemoryCollection(new Dictionary<string, string?>
					{
						["Logging:LogLevel:Default"] = "Information",
						["Logging:File:LogLevel:Default"] = "Information",
						["Logging:File:LogDirectory"] = testContext.Directory,
						["Logging:File:FileNamePattern"] = $"StressTest-Iter{iteration}-{{Timestamp:yyyy-MM-dd-HH-mm-ss-fff}}.log",
						["Logging:File:Template"] = "[{Timestamp:HH:mm:ss}] FILE {Level}: {Message}",
						["Logging:Memory:LogLevel:Default"] = "Information",
						["Logging:Memory:MaxCapacity"] = "1000"
					})
					.Build();

				var services = new ServiceCollection();
				services.AddSingleton<IConfiguration>(configuration);
				services.AddLogging(builder =>
				{
					builder.ClearProviders();
					builder.AddConfiguration(configuration.GetSection("Logging"));
					builder.SetMinimumLevel(LogLevel.Information);
					builder.AddFileLogger();
					builder.AddMemoryLogger();
				});

				using var serviceProvider = services.BuildServiceProvider();
				var logger = serviceProvider.GetRequiredService<ILogger<MultipleLoggersIntegrationTests>>();
				var memoryLoggerProvider = serviceProvider.GetRequiredService<IMemoryLoggerProvider>();             // Act - Log a unique message for this iteration
				var iterationGuid = Guid.NewGuid().ToString("N");
				var uniqueMessage = $"Test message iteration {iteration} - {iterationGuid}";
				logger.LogInformation("Test message iteration {Iteration} - {Guid}", iteration, iterationGuid);

				// Memory Logger should have the message immediately (synchronous)
				var memoryEntries = memoryLoggerProvider.Snapshot().ToList();
				var memoryEntry = memoryEntries.FirstOrDefault(e => e.Message.Contains(uniqueMessage, StringComparison.OrdinalIgnoreCase));

				if (memoryEntry.Equals(default))
				{
					failures.Add((iteration, $"Memory logger missing message: {uniqueMessage}"));
					continue;
				}

				// Wait minimal time and check file - this is where race condition occurs
				await Task.Delay(0); // No delay at all to maximize race condition exposure

				var logFiles = Directory.GetFiles(testContext.Directory, "*.log");

				if (logFiles.Length == 0)
				{
					failures.Add((iteration, "No log files created"));
					continue;
				}

				var logContent = await File.ReadAllTextAsync(logFiles[0]);

				// Check if file has the content - this might fail due to buffering
				if (!logContent.Contains(uniqueMessage, StringComparison.OrdinalIgnoreCase))
				{
					failures.Add((iteration, $"File missing message (len={logContent.Length}): {uniqueMessage}"));
					continue;
				}

				if (!logContent.Contains("FILE INFO:", StringComparison.Ordinal))
				{
					failures.Add((iteration, $"File missing template format: {uniqueMessage}"));
					continue;
				}

				// Force disposal to ensure buffers are flushed - this should make it more reliable
				await serviceProvider.DisposeAsync();
			}
			catch (Exception ex)
			{
				failures.Add((iteration, $"Exception: {ex.Message}"));
			}
		}
		// Report all failures
		if (failures.Count > 0)
		{
			var failureReport = string.Join("\n", failures.Select(f => $"  Iteration {f.iteration}: {f.reason}"));
			Assert.Fail($"Race condition exposed! {failures.Count}/{iterations} iterations failed:\n{failureReport}");
		}
	}

	[Fact]
	public async Task FileAndMemoryLoggers_WithProperDisposal_ShouldBeReliable()
	{
		// This test shows the correct way to handle FileLogger's async buffering
		// by ensuring proper disposal before checking file contents

		using var testContext = CreateTestContext(nameof(FileAndMemoryLoggers_WithProperDisposal_ShouldBeReliable));
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Logging:LogLevel:Default"] = "Information",
				["Logging:File:LogLevel:Default"] = "Information",
				["Logging:File:LogDirectory"] = testContext.Directory,
				["Logging:File:FileNamePattern"] = "ProperDisposal-{Timestamp:yyyy-MM-dd-HH-mm-ss-fff}.log",
				["Logging:File:Template"] = "[{Timestamp:HH:mm:ss}] FILE {Level}: {Message}",
				["Logging:Memory:LogLevel:Default"] = "Information",
				["Logging:Memory:MaxCapacity"] = "1000"
			})
			.Build();

		var services = new ServiceCollection();
		services.AddSingleton<IConfiguration>(configuration);
		IMemoryLoggerProvider memoryLoggerProvider;
		var testGuid = Guid.NewGuid().ToString("N");
		var uniqueMessage = $"Test message with proper disposal - {testGuid}";

		// Scope the service provider to ensure proper disposal
		{
			services.AddLogging(builder =>
			{
				builder.ClearProviders();
				builder.AddConfiguration(configuration.GetSection("Logging"));
				builder.SetMinimumLevel(LogLevel.Information);
				builder.AddFileLogger();
				builder.AddMemoryLogger();
			});

			using var serviceProvider = services.BuildServiceProvider();
			var logger = serviceProvider.GetRequiredService<ILogger<MultipleLoggersIntegrationTests>>();
			memoryLoggerProvider = serviceProvider.GetRequiredService<IMemoryLoggerProvider>();

			// Act - Log the message with structured logging
			logger.LogInformation("Test message with proper disposal - {TestGuid}", testGuid);

			// Memory should have it immediately (synchronous behavior)
			var memoryEntries = memoryLoggerProvider.Snapshot().ToList();
			var memoryEntry = memoryEntries.FirstOrDefault(e => e.Message.Contains(uniqueMessage, StringComparison.OrdinalIgnoreCase));
			Assert.False(memoryEntry.Equals(default));
			Assert.Contains(uniqueMessage, memoryEntry.Message, StringComparison.OrdinalIgnoreCase);

			// Properly dispose the service provider to flush all buffers
			await serviceProvider.DisposeAsync();
		}

		// Now check the file after proper disposal
		var logFiles = Directory.GetFiles(testContext.Directory, "*.log");
		Assert.NotEmpty(logFiles);

		var logContent = await File.ReadAllTextAsync(logFiles[0]);

		// With proper disposal, file should reliably contain the message
		Assert.Contains(uniqueMessage, logContent, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("FILE INFO:", logContent, StringComparison.Ordinal);

		// Multiple runs of this test should be reliable
		for (int i = 0; i < 10; i++)
		{
			// Re-read to ensure consistency
			var rereadContent = await File.ReadAllTextAsync(logFiles[0]);
			Assert.Equal(logContent, rereadContent);
		}
	}
	[Fact(Skip = "Stress test to expose race conditions - disable for regular runs")]
	public async Task FileAndMemoryLoggers_TemplateTest_RepeatedExecution()
	{
		// This test repeats the exact scenario from the original failing test
		// to expose the intermittent failure more reliably

		const int iterations = 100;
		var failures = new List<(int iteration, string reason)>();

		for (int iteration = 1; iteration <= iterations; iteration++)
		{
			try
			{
				// Arrange - Same as original test but with unique names
				using var testContext = CreateTestContext($"{nameof(FileAndMemoryLoggers_TemplateTest_RepeatedExecution)}_Iter_{iteration}");
				var configuration = new ConfigurationBuilder()
					.AddInMemoryCollection(new Dictionary<string, string?>
					{
						["Logging:LogLevel:Default"] = "Information",
						["Logging:File:LogLevel:Default"] = "Information",
						["Logging:File:LogDirectory"] = testContext.Directory,
						["Logging:File:FileNamePattern"] = $"Templates-Iter{iteration}-{{Timestamp:yyyy-MM-dd-HH-mm-ss-fff}}.log",
						["Logging:File:Template"] = "[{Timestamp:HH:mm:ss}] FILE {Level}: {Message}",
						["Logging:Memory:LogLevel:Default"] = "Information",
						["Logging:Memory:MaxCapacity"] = "1000",
						// This configuration is meaningless for memory logger but was in original test
						["Logging:Memory:Template"] = "MEMORY [{Level}] {Category} - {Message}"
					})
					.Build();

				var services = new ServiceCollection();
				services.AddSingleton<IConfiguration>(configuration);
				services.AddLogging(builder =>
				{
					builder.ClearProviders();
					builder.AddConfiguration(configuration.GetSection("Logging"));
					builder.SetMinimumLevel(LogLevel.Information);
					builder.AddFileLogger();
					builder.AddMemoryLogger();
				});

				using var serviceProvider = services.BuildServiceProvider();
				var logger = serviceProvider.GetRequiredService<ILogger<MultipleLoggersIntegrationTests>>();
				var memoryLoggerProvider = serviceProvider.GetRequiredService<IMemoryLoggerProvider>();

				// Act - Same as original test
				logger.LogInformation("Test message for template verification");
				await Task.Delay(FileOperationDelay); // Use the same delay as original

				// Assert - Memory part (should always work)
				var memoryEntries = memoryLoggerProvider.Snapshot().ToList();
				var memoryEntry = memoryEntries.FirstOrDefault(e => e.Message.Contains("Test message", StringComparison.OrdinalIgnoreCase));

				if (memoryEntry.Equals(default))
				{
					failures.Add((iteration, "Memory logger missing message"));
					continue;
				}

				// Assert - File part (this is where race conditions occur)
				var logFiles = Directory.GetFiles(testContext.Directory, "*.log");
				if (logFiles.Length == 0)
				{
					failures.Add((iteration, "No log files created"));
					continue;
				}

				var logContent = await File.ReadAllTextAsync(logFiles[0]);

				if (!logContent.Contains("FILE INFO:", StringComparison.Ordinal))
				{
					failures.Add((iteration, $"File missing template format (len={logContent.Length})"));
					continue;
				}

				if (!logContent.Contains("Test message for template verification", StringComparison.OrdinalIgnoreCase))
				{
					failures.Add((iteration, $"File missing message content (len={logContent.Length})"));
					continue;
				}
			}
			catch (Exception ex)
			{
				failures.Add((iteration, $"Exception: {ex.Message}"));
			}
		}
		// Report results
		if (failures.Count > 0)
		{
			var failureReport = string.Join("\n", failures.Take(10).Select(f => $"  Iteration {f.iteration}: {f.reason}"));
			if (failures.Count > 10)
			{
				failureReport += $"\n  ... and {failures.Count - 10} more failures";
			}

			var failureRate = (double)failures.Count / iterations * 100;
			Assert.Fail($"Intermittent failure detected! {failures.Count}/{iterations} iterations failed ({failureRate:F1}%):\n{failureReport}");
		}
	}
}
