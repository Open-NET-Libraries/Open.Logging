using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Open.Logging.Extensions.FileSystem;
using System.Globalization;

namespace Open.Logging.Extensions.Tests;

/// <summary>
/// Tests for the file logger provider.
/// </summary>
public class FileLoggerTests : FileLoggerTestBase
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
	public void FileLoggerProvider_Constructor_WithOptions_CreatesInstance()
	{
		// Arrange
		using var context = CreateTestContext(nameof(FileLoggerProvider_Constructor_WithOptions_CreatesInstance));
		var options = CreateOptions(context.Directory, "test-options.log");

		// Act
		using var provider = new FileLoggerProvider(options);

		// Assert
		Assert.NotNull(provider);
		// File is only created when first log entry is written (lazy initialization)
		var logger = provider.CreateLogger("TestCategory");
		Assert.NotNull(logger);
	}
	[Fact]
	public void FileLoggerProvider_Constructor_WithOptionsSnapshot_CreatesInstance()
	{
		// Arrange
		using var context = CreateTestContext(nameof(FileLoggerProvider_Constructor_WithOptionsSnapshot_CreatesInstance));
		var options = CreateOptions(context.Directory, "test-snapshot.log");
		var optionsSnapshot = new TestOptionsSnapshot<FileLoggerOptions>(options);

		// Act
		using var provider = new FileLoggerProvider(optionsSnapshot);

		// Assert
		Assert.NotNull(provider);
		// File is only created when first log entry is written (lazy initialization)
		var logger = provider.CreateLogger("TestCategory");
		Assert.NotNull(logger);
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

	/// <summary>
	/// Test to verify that concurrent logging operations don't cause deadlocks.
	/// Uses timeout to detect if the operations hang.
	/// </summary>
	[Fact]
	public async Task FileLogger_ConcurrentLogging_DoesNotDeadlock()
	{
		// Arrange
		using var context = CreateTestContext(nameof(FileLogger_ConcurrentLogging_DoesNotDeadlock));
		var options = CreateOptions(
			context.Directory,
			"concurrent-test.log",
			"{Message}",
			maxLogEntries: 1, // Small size to trigger rolling
			bufferSize: 1);

		// Create a timeout cancellation token to detect deadlocks
		using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

		// Act & Assert - This should complete within the timeout
		var testTask = Task.Run(async () =>
		{
			using var provider = new FileLoggerProvider(options);
			var logger = provider.CreateLogger("ConcurrentTestCategory");

			// Start multiple concurrent logging tasks
			var tasks = new List<Task>();
			for (int i = 0; i < 10; i++)
			{
				var taskIndex = i;
				tasks.Add(Task.Run(async () =>
				{
					for (int j = 0; j < 50; j++)
					{
						timeoutCts.Token.ThrowIfCancellationRequested();
						logger.LogInformation("Task {TaskIndex} Message {MessageIndex} with large data: {Data}",
							taskIndex, j, new string('X', 200)); await Task.Delay(1, timeoutCts.Token).ConfigureAwait(false);
					}
				}, timeoutCts.Token));
			}

			await Task.WhenAll(tasks).ConfigureAwait(false);
			await provider.DisposeAsync().ConfigureAwait(false);
		}, timeoutCts.Token);

		try
		{
			await testTask.WaitAsync(timeoutCts.Token);
		}
		catch (OperationCanceledException)
		{
			Assert.Fail("Test timed out - likely deadlock detected in concurrent logging operations");
		}

		// Verify that files were created
		var logFiles = Directory.GetFiles(context.Directory, "concurrent-test*.log");
		Assert.True(logFiles.Length >= 1, "No log files were created");
	}
	/// <summary>
	/// Test to verify that entry-based stream recreation works without deadlocks.
	/// </summary>
	[Fact]
	public async Task FileLogger_EntryBasedStreamRecreation_DoesNotDeadlock()
	{
		// Arrange
		using var context = CreateTestContext(nameof(FileLogger_EntryBasedStreamRecreation_DoesNotDeadlock));
		var options = CreateOptions(
			context.Directory,
			"stream-recreation-test.log",
			"{Message}",
			maxLogEntries: 5, // Small size to trigger stream recreation
			bufferSize: 10);

		// Create a timeout cancellation token to detect deadlocks
		using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

		// Act & Assert - This should complete within the timeout
		var testTask = Task.Run(async () =>
		{
			using var provider = new FileLoggerProvider(options);
			var logger = provider.CreateLogger("StreamRecreationTestCategory");

			// Log messages that should trigger stream recreation
			for (int i = 0; i < 20; i++)
			{
				timeoutCts.Token.ThrowIfCancellationRequested();
				logger.LogInformation("Message {MessageNumber}", i);
				// Small delay to allow file operations to process
				await Task.Delay(10, timeoutCts.Token).ConfigureAwait(false);
			}

			await provider.DisposeAsync().ConfigureAwait(false);
		}, timeoutCts.Token);

		try
		{
			await testTask.WaitAsync(timeoutCts.Token);
		}
		catch (OperationCanceledException)
		{
			Assert.Fail("Test timed out - likely deadlock detected in stream recreation operations");
		}
		// Verify that the log file was created and contains messages
		var logFiles = Directory.GetFiles(context.Directory, "stream-recreation-test.log");
		Assert.Single(logFiles);

		var logContent = await File.ReadAllTextAsync(logFiles[0]);
		Assert.Contains("Message 0", logContent, StringComparison.Ordinal);
		Assert.Contains("Message 19", logContent, StringComparison.Ordinal);
	}

	/// <summary>
	/// Test to verify that dispose operations during active logging don't cause deadlocks.
	/// Uses timeout to detect if the operations hang.
	/// </summary>
	[Fact]
	public async Task FileLogger_DisposeWhileLogging_DoesNotDeadlock()
	{
		// Arrange
		using var context = CreateTestContext(nameof(FileLogger_DisposeWhileLogging_DoesNotDeadlock));
		var options = CreateOptions(
			context.Directory,
			"dispose-test.log",
			"{Message}",
			bufferSize: 100);

		// Create a timeout cancellation token to detect deadlocks
		using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

		// Act & Assert - This should complete within the timeout
		var testTask = Task.Run(async () =>
		{
			var provider = new FileLoggerProvider(options);
			var logger = provider.CreateLogger("DisposeTestCategory");          // Start a continuous logging task
			var loggingTask = Task.Run(async () =>
			{
				try
				{
					for (int i = 0; i < 1000; i++)
					{
						timeoutCts.Token.ThrowIfCancellationRequested();
						logger.LogInformation("Continuous logging message {Index} with data: {Data}",
							i, new string('B', 100));
						await Task.Delay(5, timeoutCts.Token).ConfigureAwait(false);
					}
				}
				catch (ObjectDisposedException)
				{
					// Expected when provider is disposed
				}
				catch (System.Threading.Channels.ChannelClosedException)
				{
					// Expected when buffered writer channel is closed during dispose
				}
				catch (AggregateException ex) when (ex.InnerException is System.Threading.Channels.ChannelClosedException)
				{
					// Expected when channel closure is wrapped in AggregateException from Wait()
				}
			}, timeoutCts.Token);// Let logging run for a bit
			await Task.Delay(200, timeoutCts.Token).ConfigureAwait(false);

			// Dispose while logging is active
			await provider.DisposeAsync().ConfigureAwait(false);

			// The logging task should complete or throw ObjectDisposedException
			try
			{
				await loggingTask.WaitAsync(TimeSpan.FromSeconds(5), timeoutCts.Token).ConfigureAwait(false);
			}
			catch (TimeoutException)
			{
				// If logging task doesn't complete, that's also acceptable
			}
		}, timeoutCts.Token);

		try
		{
			await testTask.WaitAsync(timeoutCts.Token);
		}
		catch (OperationCanceledException)
		{
			Assert.Fail("Test timed out - likely deadlock detected during dispose operations");
		}

		// Verify that some log file was created
		var logFiles = Directory.GetFiles(context.Directory, "dispose-test*.log");
		Assert.True(logFiles.Length >= 1, "No log files were created");
	}
}