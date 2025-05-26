using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Open.Logging.Extensions.FileSystem;
using System.Globalization;

namespace Open.Logging.Extensions.Tests;

/// <summary>
/// Tests for the file logger provider.
/// </summary>
public class FileLoggerTests
{
	[Fact]
	public void AddFileLogger_RegistersProviderWithDI()
	{
		// Arrange
		var services = new ServiceCollection();

		// Act
		services.AddLogging(builder =>
		{
			builder.AddFileLogger(options =>
			{
				options.LogDirectory = Path.Combine(Path.GetTempPath(), "FileLoggerTest");
				options.Template = "{Timestamp:HH:mm:ss} {Category} {Level}: {Message}";
			});
		});

		var serviceProvider = services.BuildServiceProvider();

		// Assert
		var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
		Assert.NotNull(loggerFactory);

		var provider = serviceProvider.GetServices<ILoggerProvider>()
			.OfType<FileLoggerProvider>()
			.FirstOrDefault();

		Assert.NotNull(provider);
	}

	[Fact]
	public async Task FileLogger_FormatsProperly()
	{
		// Arrange
		var testOutputDir = Path.Combine(Path.GetTempPath(), "OpenLoggingFileLoggerTests");
		Directory.CreateDirectory(testOutputDir);
		var testFilePath = Path.Combine(testOutputDir, "format-test.log");

		// Create a test file to write to
		if (File.Exists(testFilePath))
		{
			File.Delete(testFilePath);
		}

		var options = new FileLoggerFormatterOptions
		{
			LogDirectory = testOutputDir,
			FileNamePattern = "format-test.log",
			MinLogLevel = LogLevel.Debug,
			Template = "{Elapsed} {Category} {Scopes}\n[{Level}]: {Message}{NewLine}{Exception}"
		};

		var optionsSnapshot = new TestOptionsSnapshot<FileLoggerFormatterOptions>(options);

		// Act
		using (var provider = new FileLoggerProvider(optionsSnapshot))
		{
			var logger = provider.CreateLogger("TestCategory");

			using (logger.BeginScope("OuterScope"))
			{
				using (logger.BeginScope("InnerScope"))
				{
					logger.LogError(
						new InvalidOperationException("Test exception"),
						"Error message");
				}
			}

			// FileLoggerProvider needs async disposal for flushing
			await provider.DisposeAsync();
		}

		// Wait for file operations to complete
		await Task.Delay(100);

		// Assert
		Assert.True(File.Exists(testFilePath));
		var logContent = await File.ReadAllTextAsync(testFilePath);

		// Check core formatting elements
		Assert.Contains("TestCategory", logContent, StringComparison.Ordinal);
		Assert.Contains("> OuterScope > InnerScope", logContent, StringComparison.Ordinal);
		Assert.Contains("[ERR!]", logContent, StringComparison.Ordinal);
		Assert.Contains("Error message", logContent, StringComparison.Ordinal);
		Assert.Contains("Test exception", logContent, StringComparison.Ordinal);

		// Clean up test file
		File.Delete(testFilePath);
	}

	[Fact]
	public async Task FileLogger_FormatsProperly_WithMultipleEntries()
	{
		// Arrange
		var testOutputDir = Path.Combine(Path.GetTempPath(), "OpenLoggingFileLoggerTests");
		Directory.CreateDirectory(testOutputDir);
		var testFilePath = Path.Combine(testOutputDir, "multiple-entries-test.log");

		// Create a test file to write to
		if (File.Exists(testFilePath))
		{
			File.Delete(testFilePath);
		}

		var options = new FileLoggerFormatterOptions
		{
			LogDirectory = testOutputDir,
			FileNamePattern = "multiple-entries-test.log",
			MinLogLevel = LogLevel.Debug,
			Template = "[{Level}]: {Message}"
		};

		var optionsSnapshot = new TestOptionsSnapshot<FileLoggerFormatterOptions>(options);

		// Act
		using (var provider = new FileLoggerProvider(optionsSnapshot))
		{
			var logger = provider.CreateLogger("TestCategory");

			logger.LogInformation("Info message 1");
			logger.LogInformation("Info message 2");
			logger.LogWarning("Warning message");

			// FileLoggerProvider needs async disposal for flushing
			await provider.DisposeAsync();
		}

		// Wait for file operations to complete
		await Task.Delay(100);

		// Assert
		Assert.True(File.Exists(testFilePath));
		var logContent = await File.ReadAllTextAsync(testFilePath);

		var lines = logContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
		Assert.Equal(3, lines.Length);

		Assert.Equal("[INFO]: Info message 1", lines[0]);
		Assert.Equal("[INFO]: Info message 2", lines[1]);
		Assert.Equal("[WARN]: Warning message", lines[2]);

		// Clean up test file
		File.Delete(testFilePath);
	}

	[Fact]
	public async Task FileLogger_PathFormatting_WorksCorrectly()
	{
		// Arrange
		var testOutputDir = Path.Combine(Path.GetTempPath(), "OpenLoggingFileLoggerPathTests");
		Directory.CreateDirectory(testOutputDir);

		// Create a test file name pattern with timestamp
		var filePattern = $"test-{{Timestamp:yyyyMMdd}}-{Guid.NewGuid():N}.log";
		var expectedDatePart = DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

		var options = new FileLoggerFormatterOptions
		{
			LogDirectory = testOutputDir,
			FileNamePattern = filePattern,
			MinLogLevel = LogLevel.Information,
		};

		var optionsSnapshot = new TestOptionsSnapshot<FileLoggerFormatterOptions>(options);

		// Act
		using (var provider = new FileLoggerProvider(optionsSnapshot))
		{
			var logger = provider.CreateLogger("TestCategory");
			logger.LogInformation("Test message");

			// FileLoggerProvider needs async disposal for flushing
			await provider.DisposeAsync();
		}

		// Wait for file operations to complete
		await Task.Delay(100);

		// Assert
		var logFiles = Directory.GetFiles(testOutputDir, $"test-{expectedDatePart}-*.log");
		Assert.Single(logFiles);

		// Clean up
		File.Delete(logFiles[0]);
		Directory.Delete(testOutputDir);
	}

	[Fact]
	public async Task FileLogger_SizeBasedRolling_WorksCorrectly()
	{
		// Arrange
		var testOutputDir = Path.Combine(Path.GetTempPath(), "OpenLoggingFileLoggerRollingTests");
		Directory.CreateDirectory(testOutputDir);
		var testFilePath = Path.Combine(testOutputDir, "rolling-test.log");

		// Clean up any existing files
		foreach (var file in Directory.GetFiles(testOutputDir, "rolling-test*.log"))
		{
			File.Delete(file);
		}

		// Create small roll size (1KB) to test rolling
		var options = new FileLoggerFormatterOptions
		{
			LogDirectory = testOutputDir,
			FileNamePattern = "rolling-test.log",
			MinLogLevel = LogLevel.Debug,
			Template = "{Message}",
			RollSizeKb = 1 // 1KB roll size for testing
		};

		var optionsSnapshot = new TestOptionsSnapshot<FileLoggerFormatterOptions>(options);

		// Act
		using (var provider = new FileLoggerProvider(optionsSnapshot))
		{
			var logger = provider.CreateLogger("TestCategory");

			// Write enough data to trigger rolling (more than 1KB)
			var longMessage = new string('X', 500);
			for (int i = 0; i < 10; i++)
			{
				logger.LogInformation("{Message} {Count}", longMessage, i);
			}

			// FileLoggerProvider needs async disposal for flushing
			await provider.DisposeAsync();
		}

		// Wait for file operations to complete
		await Task.Delay(100);

		// Assert
		var logFiles = Directory.GetFiles(testOutputDir, "rolling-test*.log");
		Assert.True(logFiles.Length >= 2, $"Expected at least 2 log files, but got {logFiles.Length}");

		// Clean up
		foreach (var file in logFiles)
		{
			File.Delete(file);
		}

		Directory.Delete(testOutputDir);
	}

	private sealed class TestOptionsSnapshot<T>(T value)
		: IOptionsSnapshot<T> where T : class, new()
	{
		public T Value { get; } = value;

		public T Get(string? name) => Value;
	}
}