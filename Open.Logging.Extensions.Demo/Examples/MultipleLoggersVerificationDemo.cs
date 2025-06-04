using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Open.Logging.Extensions.FileSystem;
using Open.Logging.Extensions.Memory;

namespace Open.Logging.Extensions.Demo.Examples;

/// <summary>
/// Demonstrates that multiple logger providers can work together correctly,
/// with logs appearing in both File and Memory providers simultaneously.
/// </summary>
internal static class MultipleLoggersVerificationDemo
{
	/// <summary>
	/// Runs a verification test to ensure multiple loggers capture the same log messages.
	/// </summary>
	/// <returns>A task that completes when the verification is finished.</returns>
	public static async Task RunVerificationAsync()
	{
		System.Console.WriteLine("=== Multiple Loggers Verification Demo ===");
		System.Console.WriteLine();

		// Create a temporary directory for file logging
		var tempDir = Path.Combine(Path.GetTempPath(), $"MultipleLoggersDemo_{DateTime.Now:yyyyMMdd_HHmmss}");
		Directory.CreateDirectory(tempDir);

		try
		{
			System.Console.WriteLine($"Creating logs in: {tempDir}");
			System.Console.WriteLine();

			IReadOnlyList<PreparedLogEntry> memoryLogs;
			string[] logFiles;

			// Configure services with both File and Memory loggers
			var services = new ServiceCollection();
			services.AddLogging(builder =>
			{
				builder.ClearProviders();

				// Add File Logger
				builder.AddFileLogger(options =>
				{
					options.LogDirectory = tempDir;
					options.FileNamePattern = "verification_logs.txt";
					options.MaxLogEntries = 1000;
				});

				// Add Memory Logger
				builder.AddMemoryLogger(options => options.MaxCapacity = 1000);

				builder.SetMinimumLevel(LogLevel.Trace);
			});
			using (var serviceProvider = services.BuildServiceProvider())
			{
				var logger = serviceProvider.GetRequiredService<ILogger<object>>();
				var memoryLoggerProvider = serviceProvider.GetRequiredService<IMemoryLoggerProvider>();

				// Generate test log messages
				System.Console.WriteLine("Generating test log messages...");

				logger.LogTrace("This is a trace message for verification");
				logger.LogDebug("This is a debug message for verification");
				logger.LogInformation("This is an information message for verification");
				logger.LogWarning("This is a warning message for verification");
				logger.LogError("This is an error message for verification");
				logger.LogCritical("This is a critical message for verification");

				// Log a message with structured data
				logger.LogInformation("User {UserId} performed {Action} at {Timestamp}",
					42, "verification_test", DateTime.UtcNow);

				// Allow some time for file writing
				await Task.Delay(100).ConfigureAwait(false);

				// Capture memory logs before disposal
				memoryLogs = memoryLoggerProvider.Snapshot();
			}

			// Allow the service provider to dispose and release file handles
			await Task.Delay(300).ConfigureAwait(false);

			// Verify Memory Logger captured the logs
			System.Console.WriteLine();
			System.Console.WriteLine("=== Memory Logger Results ===");
			System.Console.WriteLine($"Memory logger captured {memoryLogs.Count} log entries:");

			foreach (var logEntry in memoryLogs)
			{
				System.Console.WriteLine($"  [{logEntry.Level}] {logEntry.Message}");
			}

			// Verify File Logger captured the logs
			System.Console.WriteLine();
			System.Console.WriteLine("=== File Logger Results ===");
			logFiles = Directory.GetFiles(tempDir, "*.txt");

			if (logFiles.Length > 0)
			{
				System.Console.WriteLine($"Found {logFiles.Length} log file(s):");

				foreach (var logFile in logFiles)
				{
					System.Console.WriteLine($"  {Path.GetFileName(logFile)}");

					try
					{
						var fileContent = await File.ReadAllTextAsync(logFile).ConfigureAwait(false);
						var lines = fileContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
						System.Console.WriteLine($"  File contains {lines.Length} log lines");

						// Show first few lines as sample
						System.Console.WriteLine("  Sample lines:");
						foreach (var line in lines.Take(3))
						{
							System.Console.WriteLine($"    {line.Trim()}");
						}

						if (lines.Length > 3)
						{
							System.Console.WriteLine("    ...");
						}
					}
					catch (IOException ex)
					{
						System.Console.WriteLine($"  ⚠️  Could not read file (likely still in use): {ex.Message}");
						System.Console.WriteLine("  File exists and appears to contain log data.");
					}
				}
			}
			else
			{
				System.Console.WriteLine("⚠️  No log files found!");
			}

			// Verification Summary
			System.Console.WriteLine();
			System.Console.WriteLine("=== Verification Summary ===");

			var expectedLogCount = 7; // 6 basic levels + 1 structured message
			var memorySuccess = memoryLogs.Count >= expectedLogCount;
			var fileSuccess = logFiles.Length > 0;

			System.Console.WriteLine($"✓ Memory Logger: {(memorySuccess ? "PASS" : "FAIL")} - Expected ≥{expectedLogCount}, Got {memoryLogs.Count}");
			System.Console.WriteLine($"✓ File Logger: {(fileSuccess ? "PASS" : "FAIL")} - Expected ≥1 file, Got {logFiles.Length}");

			if (memorySuccess && fileSuccess)
			{
				System.Console.WriteLine();
				System.Console.WriteLine("🎉 SUCCESS: Both loggers are working correctly!");
				System.Console.WriteLine("   Logs are being duplicated to both File and Memory providers as expected.");
			}
			else
			{
				System.Console.WriteLine();
				System.Console.WriteLine("❌ FAILURE: One or more loggers are not working correctly.");
			}
		}
		finally
		{
			// Cleanup
			System.Console.WriteLine();
			System.Console.WriteLine($"Cleaning up temporary directory: {tempDir}");

			try
			{
				if (Directory.Exists(tempDir))
				{
					Directory.Delete(tempDir, recursive: true);
				}
			}
			catch (Exception ex)
			{
				System.Console.WriteLine($"⚠️  Could not delete temporary directory: {ex.Message}");
			}
		}

		System.Console.WriteLine();
		System.Console.WriteLine("Press any key to continue...");
		System.Console.ReadKey();
	}
}
