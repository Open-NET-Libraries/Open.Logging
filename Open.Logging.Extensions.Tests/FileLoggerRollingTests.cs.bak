using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Open.Logging.Extensions.Tests;

public class FileLoggerRollingTests
{
	[Fact]
	public async Task FileLogger_RollsToNewFileWhenSizeExceeded()
	{
		// Arrange
		var testOutputDir = Path.Combine(Path.GetTempPath(), "OpenLoggingFileLoggerRollingTests");
		Directory.CreateDirectory(testOutputDir);

		// Clean up any existing test files
		foreach (var file in Directory.GetFiles(testOutputDir))
		{
			File.Delete(file);
		}

		// Configure a small max file size to trigger rolling
		var options = new FileFormatterOptions
		{
			LogDirectory = testOutputDir,
			FileNamePattern = "rolling_{Timestamp:yyyyMMdd_HHmmss}.log",
			MinLogLevel = LogLevel.Debug,
			AutoFlush = true,
			MaxFileSize = 100  // Very small size to force rolling
		};

		var optionsSnapshot = new TestOptionsSnapshot<FileFormatterOptions>(options);
		using var provider = new FileLoggerProvider(optionsSnapshot);
		var logger = provider.CreateLogger("TestCategory");
		var firstFilePath = provider.FilePath;

		// Act - Write enough logs to exceed the file size limit
		for (var i = 0; i < 10; i++)
		{
			var largeMessage = new string('X', 20); // Write messages that will exceed MaxFileSize
			logger.LogInformation("{Message}", largeMessage);
		}

		// Ensure logs are flushed
		await provider.DisposeAsync();

		// Assert
		Assert.True(File.Exists(firstFilePath), "Initial log file should exist");

		// Get all log files in the directory
		var logFiles = Directory.GetFiles(testOutputDir, "rolling_*.log");

		// There should be multiple files created due to rolling
		Assert.True(logFiles.Length > 1, $"Expected multiple log files due to rolling, but got {logFiles.Length}");
	}

	[Fact]
	public async Task FileLogger_AppliesRetentionPolicy()
	{
		// Arrange
		var testOutputDir = Path.Combine(Path.GetTempPath(), "OpenLoggingFileLoggerRetentionTests");
		Directory.CreateDirectory(testOutputDir);

		// Clean up any existing test files
		foreach (var file in Directory.GetFiles(testOutputDir))
		{
			File.Delete(file);
		}

		// First, create several test log files to simulate existing logs
		for (var i = 0; i < 5; i++)
		{
			var fileName = Path.Combine(testOutputDir, $"retention_test_{i}.log");
			await File.WriteAllTextAsync(fileName, $"Test log file {i}");

			// Set the file's last write time to ensure proper ordering
			File.SetLastWriteTime(fileName, DateTime.Now.AddMinutes(-i));
		}

		// Configure retention policy to keep only 2 files
		var options = new FileFormatterOptions
		{
			LogDirectory = testOutputDir,
			FileNamePattern = "retention_current_{Timestamp:yyyyMMdd_HHmmss}.log",
			MinLogLevel = LogLevel.Debug,
			AutoFlush = true,
			MaxRetainedFiles = 2
		};

		var optionsSnapshot = new TestOptionsSnapshot<FileFormatterOptions>(options);

		// Act - Creating the provider should trigger retention policy
		using var provider = new FileLoggerProvider(optionsSnapshot);

		// Wait a bit for the background retention task to complete
		await Task.Delay(500);

		// Assert
		var logFiles = Directory.GetFiles(testOutputDir, "retention_*.log");

		// Should have 3 files: the 2 newest test files + the current log file created by the provider
		Assert.True(logFiles.Length <= 3, $"Expected at most 3 log files after retention policy, but got {logFiles.Length}");
	}

	private sealed class TestOptionsSnapshot<T>(T value) : IOptionsSnapshot<T> where T : class, new()
	{
		public T Value { get; } = value;

		public T Get(string? name) => Value;
	}
}
