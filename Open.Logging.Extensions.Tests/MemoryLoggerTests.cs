using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Open.Logging.Extensions.Memory;

namespace Open.Logging.Extensions.Tests;

public class MemoryLoggerTests
{
	[Fact]
	public void MemoryLogger_CapturesLogs()
	{
		// Arrange
		using var provider = new MemoryLoggerProvider();
		var logger = provider.CreateLogger("TestCategory");

		// Act
		logger.LogInformation("Test message");
		logger.LogWarning("Warning message");
		logger.LogError(new InvalidOperationException("Test exception"), "Error message");

		// Assert
		var logs = provider.Snapshot();
		Assert.Equal(3, logs.Count);

		Assert.Equal("Test message", logs[0].Message);
		Assert.Equal(LogLevel.Information, logs[0].Level);
		Assert.Equal("TestCategory", logs[0].Category);

		Assert.Equal("Warning message", logs[1].Message);
		Assert.Equal(LogLevel.Warning, logs[1].Level);

		Assert.Equal("Error message", logs[2].Message);
		Assert.Equal(LogLevel.Error, logs[2].Level);
		Assert.NotNull(logs[2].Exception);
		Assert.IsType<InvalidOperationException>(logs[2].Exception);
	}

	[Fact]
	public void MemoryLogger_SupportsScopes()
	{
		// Arrange
		using var provider = new MemoryLoggerProvider();
		var logger = provider.CreateLogger("TestCategory");

		// Act
		using (logger.BeginScope("OuterScope"))
		{
			using (logger.BeginScope("InnerScope"))
			{
				logger.LogInformation("Message with scopes");
			}
		}

		// Assert
		var logs = provider.Snapshot();
		Assert.Single(logs);
		Assert.Equal(2, logs[0].Scopes.Count);
		Assert.Equal("OuterScope", logs[0].Scopes[0]);
		Assert.Equal("InnerScope", logs[0].Scopes[1]);
	}

	[Fact]
	public void MemoryLogger_Drain_ReturnsAndClearsLogs()
	{
		// Arrange
		using var provider = new MemoryLoggerProvider();
		var logger = provider.CreateLogger("TestCategory");

		// Act
		logger.LogInformation("Message 1");
		logger.LogInformation("Message 2");

		// First drain should return the logs
		var logs = provider.Drain();
		Assert.Equal(2, logs.Count);

		// Second drain should return an empty list
		var emptyLogs = provider.Drain();
		Assert.Empty(emptyLogs);
	}

	[Fact]
	public void MemoryLogger_Clear_RemovesAllLogs()
	{
		// Arrange
		using var provider = new MemoryLoggerProvider();
		var logger = provider.CreateLogger("TestCategory");

		// Act
		logger.LogInformation("Message 1");
		logger.LogInformation("Message 2");

		// Verify logs were captured
		var snapshot1 = provider.Snapshot();
		Assert.Equal(2, snapshot1.Count);

		// Clear logs
		provider.Clear();

		// Verify logs were cleared
		var snapshot2 = provider.Snapshot();
		Assert.Empty(snapshot2);
	}

	[Fact]
	public void AddMemoryLogger_RegistersProviderWithDI()
	{
		// Arrange
		var services = new ServiceCollection();

		// Act
		services.AddLogging(builder => builder.AddMemoryLogger());

		using var serviceProvider = services.BuildServiceProvider();

		// Assert
		var memoryLoggerProvider = serviceProvider.GetService<IMemoryLoggerProvider>();
		Assert.NotNull(memoryLoggerProvider);

		var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
		Assert.NotNull(loggerFactory);
	}
}