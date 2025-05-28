using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Open.Logging.Extensions.FileSystem;
using Open.Logging.Extensions.Memory;

namespace Open.Logging.Extensions.Tests;

/// <summary>
/// Tests to verify logger providers work in isolation and in combination.
/// </summary>
public sealed class IsolatedLoggerTests : FileLoggerTestBase
{
	[Fact]
	public void MemoryLoggerProvider_InIsolation_CapturesLogs()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddLogging(builder =>
		{
			builder.ClearProviders(); // Start clean
			builder.AddMemoryLogger();
		});

		using var serviceProvider = services.BuildServiceProvider();
		var logger = serviceProvider.GetRequiredService<ILogger<IsolatedLoggerTests>>();
		var memoryProvider = serviceProvider.GetRequiredService<IMemoryLoggerProvider>();

		// Act
		logger.LogInformation("Test message 1");
		logger.LogWarning("Test message 2");
		logger.LogError("Test message 3");

		// Assert
		var entries = memoryProvider.Snapshot();
		Assert.Equal(3, entries.Count);
		Assert.Equal("Test message 1", entries[0].Message);
		Assert.Equal(LogLevel.Information, entries[0].Level);
		Assert.Equal("Test message 2", entries[1].Message);
		Assert.Equal(LogLevel.Warning, entries[1].Level);
		Assert.Equal("Test message 3", entries[2].Message);
		Assert.Equal(LogLevel.Error, entries[2].Level);
	}

	[Fact]
	public async Task FileLoggerProvider_InIsolation_WritesLogs()
	{
		// Arrange
		using var testContext = CreateTestContext(nameof(FileLoggerProvider_InIsolation_WritesLogs));
		var services = new ServiceCollection();

		services.AddLogging(builder =>
		{
			builder.ClearProviders(); // Start clean
			builder.AddFileLogger(options =>
			{
				options.LogDirectory = testContext.Directory;
				options.FileNamePattern = "isolation-test.log";
				options.MinLogLevel = LogLevel.Debug;
			});
		});

		string logFilePath;

		// Act
		using (var serviceProvider = services.BuildServiceProvider())
		{
			var logger = serviceProvider.GetRequiredService<ILogger<IsolatedLoggerTests>>();

			logger.LogInformation("Test message 1");
			logger.LogWarning("Test message 2");
			logger.LogError("Test message 3");

			logFilePath = Path.Combine(testContext.Directory, "isolation-test.log");
		} // Dispose serviceProvider to flush logs

		// Give file system time to write
		await Task.Delay(FileOperationDelay);
		// Assert
		Assert.True(File.Exists(logFilePath), $"Log file should exist at {logFilePath}");
		var logContent = await File.ReadAllTextAsync(logFilePath);
		Assert.Contains("Test message 1", logContent, StringComparison.Ordinal);
		Assert.Contains("Test message 2", logContent, StringComparison.Ordinal);
		Assert.Contains("Test message 3", logContent, StringComparison.Ordinal);
	}
	[Fact]
	public async Task MemoryThenFile_BothProviders_BothReceiveLogs()
	{
		// Arrange
		using var testContext = CreateTestContext(nameof(MemoryThenFile_BothProviders_BothReceiveLogs));
		var services = new ServiceCollection();

		services.AddLogging(builder =>
		{
			builder.ClearProviders(); // Start clean
			builder.AddMemoryLogger(); // Add memory first
			builder.AddFileLogger(options =>
			{
				options.LogDirectory = testContext.Directory;
				options.FileNamePattern = "memory-then-file.log";
				options.MinLogLevel = LogLevel.Debug;
			});
		});

		string logFilePath;
		IReadOnlyList<PreparedLogEntry> memoryEntries;
		// Act
		using (var serviceProvider = services.BuildServiceProvider())
		{
			var logger = serviceProvider.GetRequiredService<ILogger<IsolatedLoggerTests>>();
			var memoryProvider = serviceProvider.GetRequiredService<IMemoryLoggerProvider>();

			logger.LogInformation("Test message 1");
			logger.LogWarning("Test message 2");
			logger.LogError("Test message 3");

			// Capture memory snapshot before disposal
			memoryEntries = memoryProvider.Snapshot();
			logFilePath = Path.Combine(testContext.Directory, "memory-then-file.log");
		} // Dispose serviceProvider to flush logs

		// Give file system time to write
		await Task.Delay(FileOperationDelay);

		// Assert Memory Logger
		Assert.Equal(3, memoryEntries.Count);
		Assert.Equal("Test message 1", memoryEntries[0].Message);
		Assert.Equal("Test message 2", memoryEntries[1].Message);
		Assert.Equal("Test message 3", memoryEntries[2].Message);

		// Assert File Logger
		Assert.True(File.Exists(logFilePath), $"Log file should exist at {logFilePath}");
		var logContent = await File.ReadAllTextAsync(logFilePath);
		Assert.Contains("Test message 1", logContent, StringComparison.Ordinal);
		Assert.Contains("Test message 2", logContent, StringComparison.Ordinal);
		Assert.Contains("Test message 3", logContent, StringComparison.Ordinal);
	}
	[Fact]
	public async Task FileThenMemory_BothProviders_BothReceiveLogs()
	{
		// Arrange
		using var testContext = CreateTestContext(nameof(FileThenMemory_BothProviders_BothReceiveLogs));
		var services = new ServiceCollection();

		services.AddLogging(builder =>
		{
			builder.ClearProviders(); // Start clean
			builder.AddFileLogger(options => // Add file first
			{
				options.LogDirectory = testContext.Directory;
				options.FileNamePattern = "file-then-memory.log";
				options.MinLogLevel = LogLevel.Debug;
			});
			builder.AddMemoryLogger();
		});

		string logFilePath;
		IReadOnlyList<PreparedLogEntry> memoryEntries;

		// Act
		using (var serviceProvider = services.BuildServiceProvider())
		{
			var logger = serviceProvider.GetRequiredService<ILogger<IsolatedLoggerTests>>();
			var memoryProvider = serviceProvider.GetRequiredService<IMemoryLoggerProvider>();

			logger.LogInformation("Test message 1");
			logger.LogWarning("Test message 2");
			logger.LogError("Test message 3");

			// Capture memory snapshot before disposal
			memoryEntries = memoryProvider.Snapshot();
			logFilePath = Path.Combine(testContext.Directory, "file-then-memory.log");
		} // Dispose serviceProvider to flush logs

		// Give file system time to write
		await Task.Delay(FileOperationDelay);
		// Assert Memory Logger
		Assert.Equal(3, memoryEntries.Count);
		Assert.Equal("Test message 1", memoryEntries[0].Message);
		Assert.Equal("Test message 2", memoryEntries[1].Message);
		Assert.Equal("Test message 3", memoryEntries[2].Message);

		// Assert File Logger
		Assert.True(File.Exists(logFilePath), $"Log file should exist at {logFilePath}");
		var logContent = await File.ReadAllTextAsync(logFilePath);
		Assert.Contains("Test message 1", logContent, StringComparison.Ordinal);
		Assert.Contains("Test message 2", logContent, StringComparison.Ordinal);
		Assert.Contains("Test message 3", logContent, StringComparison.Ordinal);
	}

	[Fact]
	public void DiagnosticTest_VerifyProviderRegistration()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddLogging(builder =>
		{
			builder.ClearProviders();
			builder.AddMemoryLogger();
		});

		using var serviceProvider = services.BuildServiceProvider();

		// Act & Assert
		var allLoggerProviders = serviceProvider.GetServices<ILoggerProvider>().ToList();
		var memoryLoggerProviders = allLoggerProviders.OfType<IMemoryLoggerProvider>().ToList();
		var memoryLoggerProvider = serviceProvider.GetService<IMemoryLoggerProvider>();

		// Diagnostic output
		Assert.NotEmpty(allLoggerProviders);
		Assert.Single(memoryLoggerProviders);
		Assert.NotNull(memoryLoggerProvider);

		// Verify they're the same instance
		Assert.Same(memoryLoggerProviders[0], memoryLoggerProvider);
	}
}
