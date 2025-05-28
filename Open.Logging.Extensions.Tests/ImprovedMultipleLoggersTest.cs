using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Open.Logging.Extensions.FileSystem;
using Open.Logging.Extensions.Memory;

namespace Open.Logging.Extensions.Tests;

/// <summary>
/// Improved version of the intermittent test with better reliability and diagnostics.
/// </summary>
public class ImprovedMultipleLoggersTest : FileLoggerTestBase
{
	[Fact]
	public async Task FileAndMemoryLoggers_WithDifferentLogLevels_FilterCorrectly_Improved()
	{
		// Arrange
		using var testContext = CreateTestContext(nameof(FileAndMemoryLoggers_WithDifferentLogLevels_FilterCorrectly_Improved));
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Logging:LogLevel:Default"] = "Information",
				["Logging:File:LogLevel:Default"] = "Warning", // File only gets Warning and above
				["Logging:File:LogDirectory"] = testContext.Directory,
				["Logging:File:FileNamePattern"] = "ImprovedTest-{Timestamp:yyyy-MM-dd-HH-mm-ss-fff}.log",
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

		string? combinedFileContent = null;
		IReadOnlyList<PreparedLogEntry> memoryEntries;

		// Use proper scoping to ensure disposal and flushing
		{
			using var serviceProvider = services.BuildServiceProvider();
			var logger = serviceProvider.GetRequiredService<ILogger<ImprovedMultipleLoggersTest>>();
			var memoryLoggerProvider = serviceProvider.GetRequiredService<IMemoryLoggerProvider>();

			// Act - Log at different levels with clear, unique messages
			logger.LogDebug("DEBUG-LEVEL-MESSAGE-SHOULD-BE-MEMORY-ONLY");
			logger.LogInformation("INFO-LEVEL-MESSAGE-SHOULD-BE-MEMORY-ONLY");
			logger.LogWarning("WARNING-LEVEL-MESSAGE-SHOULD-BE-IN-BOTH");
			logger.LogError("ERROR-LEVEL-MESSAGE-SHOULD-BE-IN-BOTH");

			// Small delay for any immediate async operations
			await Task.Delay(50);

			// Capture memory entries before disposal
			memoryEntries = memoryLoggerProvider.Snapshot();

		} // ServiceProvider disposal happens here - should flush all file buffers

		// Additional delay after disposal to ensure file operations complete
		await Task.Delay(200);

		// Now read all file content
		var logFiles = Directory.GetFiles(testContext.Directory, "*.log");
		System.Console.WriteLine($"Found {logFiles.Length} log files after disposal");
		
		if (logFiles.Length > 0)
		{
			var allContent = new List<string>();
			foreach (var file in logFiles)
			{
				var content = await File.ReadAllTextAsync(file);
				allContent.Add(content);
				System.Console.WriteLine($"File: {Path.GetFileName(file)}, Content: '{content.Trim()}'");
			}
			combinedFileContent = string.Join("\n", allContent);
		}

		// Assert - Memory should have all 4 messages (Debug, Info, Warning, Error)
		Assert.Equal(4, memoryEntries.Count);
		Assert.Contains(memoryEntries, e => e.Message.Contains("DEBUG-LEVEL-MESSAGE", StringComparison.Ordinal));
		Assert.Contains(memoryEntries, e => e.Message.Contains("INFO-LEVEL-MESSAGE", StringComparison.Ordinal));
		Assert.Contains(memoryEntries, e => e.Message.Contains("WARNING-LEVEL-MESSAGE", StringComparison.Ordinal));
		Assert.Contains(memoryEntries, e => e.Message.Contains("ERROR-LEVEL-MESSAGE", StringComparison.Ordinal));

		// Assert - File should have exactly 2 messages (Warning and Error only)
		// This is the critical part - we MUST have file content if the file logger is working
		Assert.NotNull(combinedFileContent);
		Assert.NotEmpty(combinedFileContent);

		// File should NOT contain Debug or Info messages
		Assert.DoesNotContain("DEBUG-LEVEL-MESSAGE", combinedFileContent, StringComparison.Ordinal);
		Assert.DoesNotContain("INFO-LEVEL-MESSAGE", combinedFileContent, StringComparison.Ordinal);

		// File MUST contain Warning and Error messages
		Assert.Contains("WARNING-LEVEL-MESSAGE", combinedFileContent, StringComparison.Ordinal);
		Assert.Contains("ERROR-LEVEL-MESSAGE", combinedFileContent, StringComparison.Ordinal);

		System.Console.WriteLine("Test completed successfully - file logger correctly filtered log levels");
	}
}
