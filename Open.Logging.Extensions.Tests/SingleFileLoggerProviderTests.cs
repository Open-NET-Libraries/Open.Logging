using Microsoft.Extensions.Logging;
using Open.Logging.Extensions.FileSystem;
using System.Threading.Channels;

namespace Open.Logging.Extensions.Tests;

/// <summary>
/// Tests for the SingleFileLoggerProvider class.
/// </summary>
public class SingleFileLoggerProviderTests : FileLoggerTestBase
{	[Fact]
	public void SingleFileLoggerProvider_Constructor_WithOptions_CreatesInstance()
	{
		// Arrange
		using var context = CreateTestContext(nameof(SingleFileLoggerProvider_Constructor_WithOptions_CreatesInstance));
		var options = CreateOptions(context.Directory, "test-single.log");

		// Act
		using var provider = new SingleFileLoggerProvider(options);

		// Assert
		Assert.NotNull(provider);
		// File is only created when first log entry is written (lazy initialization)
		var logger = provider.CreateLogger("TestCategory");
		Assert.NotNull(logger);
	}

	[Fact]
	public async Task SingleFileLogger_FormatsProperly()
	{
		// Arrange
		using var context = CreateTestContext(nameof(SingleFileLogger_FormatsProperly));
		var options = CreateOptions(
			context.Directory,
			"format-test-single.log",
			"{Elapsed} {Category} {Scopes}\n[{Level}]: {Message}{NewLine}{Exception}");

		// Act
		var testFilePath = await ExecuteWithFileLoggerSync(options, logger =>
		{
			using (logger.BeginScope("OuterScope"))
			{
				using (logger.BeginScope("InnerScope"))
				{
					logger.LogError(new InvalidOperationException("Test exception"), "Error message");
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
	public async Task SingleFileLogger_FormatsProperly_WithMultipleEntries()
	{
		// Arrange
		using var context = CreateTestContext(nameof(SingleFileLogger_FormatsProperly_WithMultipleEntries));
		var options = CreateOptions(context.Directory, "multiple-entries-test-single.log", "[{Level}]: {Message}");

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
	public async Task SingleFileLogger_WritesToSameFile()
	{
		// Arrange
		using var context = CreateTestContext(nameof(SingleFileLogger_WritesToSameFile));
		var testFilePath = context.GetFilePath("non-rolling-test-single.log");
		context.CleanupFiles("non-rolling-test-single*.log");

		var options = CreateOptions(
			context.Directory,
			"non-rolling-test-single.log",
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
		var logFiles = Directory.GetFiles(context.Directory, "non-rolling-test-single*.log");
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
	/// Test to verify that concurrent logging operations work correctly with SingleFileLoggerProvider.
	/// Uses timeout to detect if the operations hang.
	/// </summary>
	[Fact]
	public async Task SingleFileLogger_ConcurrentLogging_DoesNotDeadlock()
	{
		// Arrange
		using var context = CreateTestContext(nameof(SingleFileLogger_ConcurrentLogging_DoesNotDeadlock));
		var options = CreateOptions(
			context.Directory,
			"concurrent-test-single.log",
			"{Message}",
			bufferSize: 1);

		// Create a timeout cancellation token to detect deadlocks
		using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

		// Act & Assert - This should complete within the timeout
		var testTask = Task.Run(async () =>
		{
			using var provider = new SingleFileLoggerProvider(options);
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
						logger.LogInformation("Task {TaskIndex} Message {MessageIndex} with data: {Data}",
							taskIndex, j, new string('X', 200));
						await Task.Delay(1, timeoutCts.Token).ConfigureAwait(false);
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

		// Verify that file was created
		var logFiles = Directory.GetFiles(context.Directory, "concurrent-test-single*.log");
		Assert.Single(logFiles);
	}

	/// <summary>
	/// Test to verify that dispose operations during active logging don't cause deadlocks.
	/// Uses timeout to detect if the operations hang.
	/// </summary>
	[Fact]
	public async Task SingleFileLogger_DisposeWhileLogging_DoesNotDeadlock()
	{
		// Arrange
		using var context = CreateTestContext(nameof(SingleFileLogger_DisposeWhileLogging_DoesNotDeadlock));
		var options = CreateOptions(
			context.Directory,
			"dispose-test-single.log",
			"{Message}",
			bufferSize: 100);

		// Create a timeout cancellation token to detect deadlocks
		using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

		// Act & Assert - This should complete within the timeout
		var testTask = Task.Run(async () =>
		{
			var provider = new SingleFileLoggerProvider(options);
			var logger = provider.CreateLogger("DisposeTestCategory");			// Start a continuous logging task
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
				catch (AggregateException ex) when (ex.InnerException is ChannelClosedException)
				{
					// Expected during disposal - race condition between logging and disposal
				}
				catch (ChannelClosedException)
				{
					// Expected during disposal - race condition between logging and disposal
				}
			}, timeoutCts.Token);

			// Let logging run for a bit
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

		// Verify that log file was created
		var logFiles = Directory.GetFiles(context.Directory, "dispose-test-single*.log");
		Assert.Single(logFiles);
	}

	/// <summary>
	/// Test to verify high-volume logging works correctly.
	/// </summary>
	[Fact]
	public async Task SingleFileLogger_HighVolumeLogging_WorksCorrectly()
	{
		// Arrange
		using var context = CreateTestContext(nameof(SingleFileLogger_HighVolumeLogging_WorksCorrectly));
		var options = CreateOptions(
			context.Directory,
			"high-volume-test-single.log",
			"{Message}",
			bufferSize: 1000);

		// Create a timeout cancellation token
		using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

		// Act & Assert
		var testTask = Task.Run(async () =>
		{
			using var provider = new SingleFileLoggerProvider(options);
			var logger = provider.CreateLogger("HighVolumeTestCategory");

			// Log a lot of messages quickly
			for (int i = 0; i < 5000; i++)
			{
				timeoutCts.Token.ThrowIfCancellationRequested();
				logger.LogInformation("High volume message {Index} with data: {Data}",
					i, new string('Z', 50));

				// Yield occasionally to prevent blocking
				if (i % 100 == 0)
				{
					await Task.Delay(1, timeoutCts.Token).ConfigureAwait(false);
				}
			}

			await provider.DisposeAsync().ConfigureAwait(false);
		}, timeoutCts.Token);
		try
		{
			await testTask.WaitAsync(timeoutCts.Token);
		}
		catch (OperationCanceledException)
		{
			Assert.Fail("Test timed out - likely deadlock detected in high volume logging");
		}

		// Verify file was created and has content
		var logFiles = Directory.GetFiles(context.Directory, "high-volume-test-single*.log");
		Assert.Single(logFiles);

		var logContent = await File.ReadAllTextAsync(logFiles[0]);
		Assert.NotEmpty(logContent);

		// Verify we have a reasonable number of log entries
		var lines = logContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
		Assert.True(lines.Length >= 4000, $"Expected at least 4000 log entries, but got {lines.Length}");
	}
}
