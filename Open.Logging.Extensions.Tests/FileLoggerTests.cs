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
	private static readonly TimeSpan FileOperationDelay = TimeSpan.FromMilliseconds(100);

	/// <summary>
	/// Creates a test context with unique directory and file management.
	/// </summary>
	private static TestContext CreateTestContext(string testName)
		=> new(testName);

	/// <summary>
	/// Creates options for file logging with common defaults.
	/// </summary>
	private static FileLoggerFormatterOptions CreateOptions(
		string directory,
		string fileName,
		string? template = null,
		LogLevel minLogLevel = LogLevel.Debug,
		int? rollSizeKb = null,
		int? bufferSize = null)
	{
		return new FileLoggerFormatterOptions
		{
			LogDirectory = directory,
			FileNamePattern = fileName,
			MinLogLevel = minLogLevel,
			Template = template ?? "{Level}: {Message}",
			RollSizeKb = rollSizeKb ?? 0,
			BufferSize = bufferSize ?? 0
		};
	}   /// <summary>
		/// Executes a test with a file logger provider and handles cleanup.
		/// </summary>
	private static async Task<string> ExecuteWithFileLogger(
		FileLoggerFormatterOptions options,
		Func<ILogger, Task> logAction)
	{
		using var provider = new FileLoggerProvider(options);
		var logger = provider.CreateLogger("TestCategory");

		await logAction(logger).ConfigureAwait(true);
		await provider.DisposeAsync().ConfigureAwait(true);
		await Task.Delay(FileOperationDelay).ConfigureAwait(true);

		var expectedFilePath = Path.Combine(options.LogDirectory, options.FileNamePattern);
		return expectedFilePath;
	}

	/// <summary>
	/// Executes a test with a file logger provider (synchronous action) and handles cleanup.
	/// </summary>
	private static async Task<string> ExecuteWithFileLoggerSync(
		FileLoggerFormatterOptions options,
		Action<ILogger> logAction)
	{
		using var provider = new FileLoggerProvider(options);
		var logger = provider.CreateLogger("TestCategory");

		logAction(logger);
		await provider.DisposeAsync().ConfigureAwait(true);
		await Task.Delay(FileOperationDelay).ConfigureAwait(true);

		var expectedFilePath = Path.Combine(options.LogDirectory, options.FileNamePattern);
		return expectedFilePath;
	}

	/// <summary>
	/// Manages test directory and file cleanup for a single test.
	/// </summary>
	private sealed class TestContext : IDisposable
	{
		public string Directory { get; }
		public string TestName { get; }

		public TestContext(string testName)
		{
			TestName = testName;
			Directory = Path.Combine(Path.GetTempPath(), $"OpenLoggingFileLoggerTests_{testName}");
			CleanupDirectory();
			System.IO.Directory.CreateDirectory(Directory);
		}

		public string GetFilePath(string fileName) => Path.Combine(Directory, fileName);

		public void CleanupFiles(string filePattern)
		{
			if (!System.IO.Directory.Exists(Directory)) return;

			foreach (var file in System.IO.Directory.GetFiles(Directory, filePattern))
			{
				try { File.Delete(file); }
				catch { /* Ignore cleanup failures */ }
			}
		}

		private void CleanupDirectory()
		{
			if (!System.IO.Directory.Exists(Directory)) return;

			try
			{
				System.IO.Directory.Delete(Directory, true);
			}
			catch { /* Ignore cleanup failures */ }
		}

		public void Dispose()
		{
			CleanupDirectory();
		}
	}

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
	public void FileLoggerProvider_Constructor_WithOptions_CreatesInstance()
	{
		// Arrange
		using var context = CreateTestContext(nameof(FileLoggerProvider_Constructor_WithOptions_CreatesInstance));
		var options = CreateOptions(context.Directory, "test-options.log");
		var expectedFilePath = context.GetFilePath("test-options.log");

		// Act
		using var provider = new FileLoggerProvider(options);

		// Assert
		Assert.NotNull(provider);
		Assert.True(File.Exists(expectedFilePath));
	}

	[Fact]
	public void FileLoggerProvider_Constructor_WithOptionsSnapshot_CreatesInstance()
	{
		// Arrange
		using var context = CreateTestContext(nameof(FileLoggerProvider_Constructor_WithOptionsSnapshot_CreatesInstance));
		var options = CreateOptions(context.Directory, "test-snapshot.log");
		var expectedFilePath = context.GetFilePath("test-snapshot.log");
		var optionsSnapshot = new TestOptionsSnapshot<FileLoggerFormatterOptions>(options);

		// Act
		using var provider = new FileLoggerProvider(optionsSnapshot);

		// Assert
		Assert.NotNull(provider);
		Assert.True(File.Exists(expectedFilePath));
	}
	[Fact]
	public async Task FileLogger_FormatsProperly()
	{
		// Arrange
		using var context = CreateTestContext(nameof(FileLogger_FormatsProperly));
		var options = CreateOptions(
			context.Directory,
			"format-test.log",
			"{Elapsed} {Category} {Scopes}\n[{Level}]: {Message}{NewLine}{Exception}");

		// Act
		var testFilePath = await ExecuteWithFileLoggerSync(options, logger =>
		{
			using (logger.BeginScope("OuterScope"))
			{
				using (logger.BeginScope("InnerScope"))
				{
					logger.LogError(
						new InvalidOperationException("Test exception"),
						"Error message");
				}
			}
		});

		// Assert
		Assert.True(File.Exists(testFilePath));
		var logContent = await File.ReadAllTextAsync(testFilePath);

		// Check core formatting elements
		Assert.Contains("TestCategory", logContent, StringComparison.Ordinal);
		Assert.Contains("> OuterScope > InnerScope", logContent, StringComparison.Ordinal);
		Assert.Contains($"[{LogLevelLabels.Default.Error}]", logContent, StringComparison.Ordinal);
		Assert.Contains("Error message", logContent, StringComparison.Ordinal);
		Assert.Contains("Test exception", logContent, StringComparison.Ordinal);
	}

	[Fact]
	public async Task FileLogger_FormatsProperly_WithMultipleEntries()
	{
		// Arrange
		using var context = CreateTestContext(nameof(FileLogger_FormatsProperly_WithMultipleEntries));
		var options = CreateOptions(context.Directory, "multiple-entries-test.log", "[{Level}]: {Message}");

		// Act
		var testFilePath = await ExecuteWithFileLoggerSync(options, logger =>
		{
			logger.LogInformation("Info message 1");
			logger.LogInformation("Info message 2");
			logger.LogWarning("Warning message");
		});

		// Assert
		Assert.True(File.Exists(testFilePath));
		var logContent = await File.ReadAllTextAsync(testFilePath);

		var lines = logContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
		Assert.Equal(3, lines.Length);

		Assert.Equal($"[{LogLevelLabels.Default.Information}]: Info message 1", lines[0]);
		Assert.Equal($"[{LogLevelLabels.Default.Information}]: Info message 2", lines[1]);
		Assert.Equal($"[{LogLevelLabels.Default.Warning}]: Warning message", lines[2]);
	}

	[Fact]
	public async Task FileLogger_PathFormatting_WorksCorrectly()
	{
		// Arrange
		using var context = CreateTestContext(nameof(FileLogger_PathFormatting_WorksCorrectly));
		var uniqueId = Guid.NewGuid().ToString("N");
		var filePattern = $"unique-test-{uniqueId}-{{Timestamp:yyyyMMdd}}.log";
		var expectedDatePart = DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
		var options = CreateOptions(context.Directory, filePattern, minLogLevel: LogLevel.Information);

		// Act
		await ExecuteWithFileLoggerSync(options, logger => logger.LogInformation("Test message"));

		// Assert
		var logFiles = Directory.GetFiles(context.Directory, $"unique-test-{uniqueId}-{expectedDatePart}.log");
		Assert.Single(logFiles);
	}
	[Fact]
	public async Task FileLogger_SizeBasedRolling_WorksCorrectly()
	{
		// Arrange
		using var context = CreateTestContext(nameof(FileLogger_SizeBasedRolling_WorksCorrectly));
		var testFilePath = context.GetFilePath("rolling-test.log");
		context.CleanupFiles("rolling-test*.log");

		var options = CreateOptions(
			context.Directory,
			"rolling-test.log",
			"{Message}",
			rollSizeKb: 1, // 1KB roll size for testing
			bufferSize: 1);

		var testData = new string('X', 500);
		// Act
		await ExecuteWithFileLogger(options, async logger =>
		{
			// Make the file much larger to ensure rolling happens
			for (int i = 0; i < 20; i++)
			{
				// Add index to verify log order later
				logger.LogInformation("TestData {Index}", $"{testData}-{i}");

				// Give some time for I/O between logs
				await Task.Delay(50).ConfigureAwait(true);
			}
		});

		// Assert
		var logFiles = Directory.GetFiles(context.Directory, "rolling-test*.log");
		Assert.True(logFiles.Length >= 2, $"Expected at least 2 log files, but got {logFiles.Length}");

		// Verify at least one rolled file exists (with timestamp)
		var rolledFiles = logFiles.Where(f => f != testFilePath).ToList();
		Assert.NotEmpty(rolledFiles);

		// The current file should exist
		Assert.True(File.Exists(testFilePath));

		// Verify the current file has content (the last entries)
		var currentContent = await File.ReadAllTextAsync(testFilePath);
		Assert.NotEmpty(currentContent);

		// Get content from the first rolled file
		var rolledContent = await File.ReadAllTextAsync(rolledFiles[0]);
		Assert.NotEmpty(rolledContent);

		// Verify we have logging content distributed across the files
		var combinedEntries = currentContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
			.Concat(rolledContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
		// Make sure we have entries from our test data
		var allEntriesText = string.Join(Environment.NewLine, combinedEntries);
		for (int i = 0; i < 10; i++)
		{
			// Check that the log contains our test data with index
			Assert.Contains($"TestData {testData}-{i}", allEntriesText, StringComparison.Ordinal);
		}
	}

	[Fact]
	public async Task FileLogger_NonRolling_WritesToSameFile()
	{
		// Arrange
		using var context = CreateTestContext(nameof(FileLogger_NonRolling_WritesToSameFile));
		var testFilePath = context.GetFilePath("non-rolling-test.log");
		context.CleanupFiles("non-rolling-test*.log");

		var options = CreateOptions(
			context.Directory,
			"non-rolling-test.log",
			"{Level}: {Message}");

		// Act - Write multiple batches of logs
		// First batch
		await ExecuteWithFileLoggerSync(options, logger =>
		{
			// Write some logs
			for (int i = 0; i < 5; i++)
			{
				logger.LogInformation("First batch message {Count}", i);
			}
		});

		// Second batch - should append to the same file
		await ExecuteWithFileLoggerSync(options, logger =>
		{
			// Write more logs
			for (int i = 5; i < 10; i++)
			{
				logger.LogInformation("Second batch message {Count}", i);
			}
		});

		// Assert
		// Should have only one log file
		var logFiles = Directory.GetFiles(context.Directory, "non-rolling-test*.log");
		Assert.Single(logFiles);

		// Check file content
		var logContent = await File.ReadAllTextAsync(testFilePath);
		var lines = logContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

		// Should contain all log entries from both batches
		Assert.Equal(10, lines.Length);

		// Verify first and last entries
		Assert.Equal($"{LogLevelLabels.Default.Information}: First batch message 0", lines[0]);
		Assert.Equal($"{LogLevelLabels.Default.Information}: Second batch message 9", lines[^1]);
	}

	private sealed class TestOptionsSnapshot<T>(T value)
		: IOptionsSnapshot<T> where T : class, new()
	{
		public T Value { get; } = value;

		public T Get(string? name) => Value;
	}
}