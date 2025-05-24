using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Open.Logging.Extensions.Tests;

public class FileLoggerTests
{
	[Fact]
	public async Task FileLogger_WritesToFile()
	{
		// Arrange
		var testOutputDir = Path.Combine(Path.GetTempPath(), "OpenLoggingFileLoggerTests");
		Directory.CreateDirectory(testOutputDir);
		var testFilePath = Path.Combine(testOutputDir, "test.log");

		// Create a test file to write to
		if (File.Exists(testFilePath))
		{
			File.Delete(testFilePath);
		}

		var options = new FileFormatterOptions
		{
			LogDirectory = testOutputDir,
			FileNamePattern = "test.log",
			MinLogLevel = LogLevel.Debug,
			AutoFlush = true
		};

		var optionsSnapshot = new TestOptionsSnapshot<FileFormatterOptions>(options);
		var provider = new FileLoggerProvider(optionsSnapshot);
		var logger = provider.CreateLogger("TestCategory");

		// Act
		using (logger.BeginScope("TestScope"))
		{
			logger.Log(LogLevel.Information, 0, "Test message", null, (s, _) => s.ToString()!);
		}

		// Dispose the provider to flush the file writer
		await provider.DisposeAsync();
		// Assert
		Assert.True(File.Exists(testFilePath));
		var logContent = await File.ReadAllTextAsync(testFilePath);
		Assert.Contains("TestCategory", logContent, StringComparison.Ordinal);
		Assert.Contains("TestScope", logContent, StringComparison.Ordinal);
		Assert.Contains("Test message", logContent, StringComparison.Ordinal);
	}

	[Fact]
	public void AddFile_RegistersFileLoggerProvider()
	{
		// Arrange
		var services = new ServiceCollection();

		// Act
		services.AddLogging(builder =>
		{
			builder.AddFile(options =>
			{
				options.LogDirectory = Path.Combine(Path.GetTempPath(), "FileLoggerTest");
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

		var options = new FileFormatterOptions
		{
			LogDirectory = testOutputDir,
			FileNamePattern = "format-test.log",
			MinLogLevel = LogLevel.Debug,
			AutoFlush = true,
			Template = "{Elapsed} {Category} {Scopes}\n[{Level}]: {Message}{NewLine}{Exception}"
		};

		var optionsSnapshot = new TestOptionsSnapshot<FileFormatterOptions>(options);
		var provider = new FileLoggerProvider(optionsSnapshot);
		var logger = provider.CreateLogger("TestCategory");

		// Act
		using (logger.BeginScope("OuterScope"))
		{
			using (logger.BeginScope("InnerScope"))
			{
				logger.LogError(
					new InvalidOperationException("Test exception"),
					"Error message");
			}
		}

		// Dispose the provider to flush the file writer
		await provider.DisposeAsync();
		// Assert
		Assert.True(File.Exists(testFilePath));
		var logContent = await File.ReadAllTextAsync(testFilePath);

		// Check core formatting elements
		Assert.Contains("TestCategory", logContent, StringComparison.Ordinal);
		Assert.Contains("> OuterScope > > InnerScope", logContent, StringComparison.Ordinal);
		Assert.Contains("[ERR!]", logContent, StringComparison.Ordinal);
		Assert.Contains("Error message", logContent, StringComparison.Ordinal);
		Assert.Contains("Test exception", logContent, StringComparison.Ordinal);

		// Verify the log format structure
		var lines = logContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
		Assert.True(lines.Length >= 3);

		// First line should contain timestamp, category and scopes
		Assert.Contains("TestCategory > OuterScope > > InnerScope", lines[0], StringComparison.Ordinal);

		// Second line should contain level and message
		Assert.Equal("[ERR!]: Error message", lines[1]);

		// Third line should contain exception
		Assert.Contains("InvalidOperationException", lines[2], StringComparison.Ordinal);
		Assert.Contains("Test exception", lines[2], StringComparison.Ordinal);
	}
	private class TestOptionsSnapshot<T> : IOptionsSnapshot<T> where T : class, new()
	{
		public TestOptionsSnapshot(T value)
		{
			Value = value;
		}

		public T Value { get; }

		public T Get(string? name)
		{
			return Value;
		}
	}
}
