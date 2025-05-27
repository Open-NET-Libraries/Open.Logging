using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Open.Logging.Extensions.FileSystem;

namespace Open.Logging.Extensions.Tests;

/// <summary>
/// Base class for file logger tests providing common functionality and utilities.
/// </summary>
public abstract class FileLoggerTestBase
{
	protected static readonly TimeSpan FileOperationDelay = TimeSpan.FromMilliseconds(100);

	/// <summary>
	/// Default timeout for deadlock detection tests.
	/// </summary>
	protected static readonly TimeSpan DeadlockDetectionTimeout = TimeSpan.FromSeconds(5);

	/// <summary>
	/// Creates a test context with unique directory and file management.
	/// </summary>
	protected static TestContext CreateTestContext(string testName)
		=> new(testName);

	/// <summary>
	/// Executes a task with deadlock detection timeout.
	/// </summary>
	/// <param name="taskFactory">Function that creates the task to execute</param>
	/// <param name="testName">Name of the test for error messages</param>
	/// <param name="timeout">Timeout duration (defaults to DeadlockDetectionTimeout)</param>
	protected static async Task ExecuteWithDeadlockDetection(
		Func<CancellationToken, Task> taskFactory,
		string testName,
		TimeSpan? timeout = null)
	{
		using var timeoutCts = new CancellationTokenSource(timeout ?? DeadlockDetectionTimeout);

		var testTask = Task.Run(() => taskFactory(timeoutCts.Token), timeoutCts.Token);

		try
		{
			await testTask.WaitAsync(timeoutCts.Token).ConfigureAwait(true);
		}
		catch (OperationCanceledException)
		{
			Assert.Fail($"Test '{testName}' timed out after {(timeout ?? DeadlockDetectionTimeout).TotalSeconds} seconds - likely deadlock detected");
		}
	}

	/// <summary>
	/// Creates options for file logging with common defaults.
	/// </summary>
	protected static FileLoggerOptions CreateOptions(
		string directory,
		string fileName,
		string? template = null,
		LogLevel minLogLevel = LogLevel.Debug,
		int bufferSize = 10000,
		int maxLogEntries = 0)
	{
		return new FileLoggerOptions
		{
			LogDirectory = directory,
			FileNamePattern = fileName,
			MinLogLevel = minLogLevel,
			Template = template ?? "{Level}: {Message}",
			BufferSize = bufferSize,
			MaxLogEntries = maxLogEntries
		};
	}

	/// <summary>
	/// Executes a test with a file logger provider and handles cleanup.
	/// </summary>
	protected static async Task<string> ExecuteWithFileLogger(
		FileLoggerOptions options,
		Func<ILogger, Task> logAction)
	{
		ArgumentNullException.ThrowIfNull(options);
		ArgumentNullException.ThrowIfNull(logAction);

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
	protected static async Task<string> ExecuteWithFileLoggerSync(
		FileLoggerOptions options,
		Action<ILogger> logAction)
	{
		ArgumentNullException.ThrowIfNull(options);
		ArgumentNullException.ThrowIfNull(logAction);

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
	protected sealed class TestContext : IDisposable
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

	protected sealed class TestOptionsSnapshot<T>(T value)
		: IOptionsSnapshot<T> where T : class, new()
	{
		public T Value { get; } = value;

		public T Get(string? name) => Value;
	}
}
