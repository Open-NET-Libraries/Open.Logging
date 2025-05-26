using Microsoft.Extensions.Logging;
using Open.Logging.Extensions.Console;

namespace Open.Logging.Extensions.Tests;

public class ConsoleDelegateLoggerTests
{
	[Fact]
	public void LogInformation_CallsDelegate()
	{
		// Arrange
		var handlerCalled = false;
		PreparedLogEntry capturedEntry = default;

		var logger = new ConsoleDelegateLogger(
			entry =>
			{
				handlerCalled = true;
				capturedEntry = entry;
			},
			LogLevel.Information,
			"TestCategory");

		// Act
		logger.LogInformation("Test message");

		// Assert
		Assert.True(handlerCalled);
		Assert.Equal(LogLevel.Information, capturedEntry.Level);
		Assert.Equal("Test message", capturedEntry.Message);
		Assert.Equal("TestCategory", capturedEntry.Category);
	}

	[Fact]
	public void LogWarning_RespectsDelegateFunction()
	{
		// Arrange
		var messages = new List<string>();

		var logger = new ConsoleDelegateLogger(
			entry => messages.Add(entry.Message),
			LogLevel.Warning,
			"TestCategory");

		// Act
		logger.LogInformation("This should not be captured");
		logger.LogWarning("This should be captured");
		logger.LogError("This should also be captured");

		// Assert
		Assert.Equal(2, messages.Count);
		Assert.Contains("This should be captured", messages);
		Assert.Contains("This should also be captured", messages);
		Assert.DoesNotContain("This should not be captured", messages);
	}

	[Fact]
	public void LogWithScope_PassesScopeToDelegate()
	{
		// Arrange
		PreparedLogEntry capturedEntry = default;

		var logger = new ConsoleDelegateLogger(
			entry => capturedEntry = entry,
			LogLevel.Information,
			"TestCategory",
			scopeProvider: new LoggerExternalScopeProvider());

		// Act
		using (logger.BeginScope("TestScope"))
		{
			logger.LogInformation("Message with scope");
		}

		// Assert
		Assert.Single(capturedEntry.Scopes);
		Assert.Equal("TestScope", capturedEntry.Scopes[0].ToString());
	}

	[Fact]
	public void LogError_WithException_PassesExceptionToDelegate()
	{
		// Arrange
		PreparedLogEntry capturedEntry = default;
		var expectedException = new InvalidOperationException("Test exception");

		var logger = new ConsoleDelegateLogger(
			entry => capturedEntry = entry,
			LogLevel.Error,
			"TestCategory");

		// Act
		logger.LogError(expectedException, "Error with exception");

		// Assert
		Assert.Equal(expectedException, capturedEntry.Exception);
		Assert.Equal("Error with exception", capturedEntry.Message);
	}

	[Fact]
	public void Constructor_WithNullHandler_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.Throws<ArgumentNullException>(() =>
			new ConsoleDelegateLogger(null!, LogLevel.Information, "TestCategory"));
	}

	[Fact]
	public void Logger_WithSpecificTimestamp_UsesProvidedTimestamp()
	{
		// Arrange
		var timestamp = new DateTimeOffset(2023, 1, 1, 12, 0, 0, TimeSpan.Zero);
		PreparedLogEntry capturedEntry = default;

		var logger = new ConsoleDelegateLogger(
			entry => capturedEntry = entry,
			LogLevel.Information,
			"TestCategory",
			timestamp);

		// Act
		logger.LogInformation("Test message");

		// Assert
		Assert.Equal(timestamp, capturedEntry.StartTime);
	}

	[Fact]
	public void LogWithNestedScopes_CapturesAllScopes()
	{
		// Arrange
		PreparedLogEntry capturedEntry = default;

		var logger = new ConsoleDelegateLogger(
			entry => capturedEntry = entry,
			LogLevel.Information,
			"TestCategory",
			scopeProvider: new LoggerExternalScopeProvider());

		// Act
		using (logger.BeginScope("OuterScope"))
		{
			using (logger.BeginScope("InnerScope"))
			{
				logger.LogInformation("Message with nested scopes");
			}
		}

		// Assert
		Assert.Equal(2, capturedEntry.Scopes.Count);
		Assert.Equal("OuterScope", capturedEntry.Scopes[0].ToString());
		Assert.Equal("InnerScope", capturedEntry.Scopes[1].ToString());
	}

	[Fact]
	public void LogWithStructuredData_FormatsMessageCorrectly()
	{
		// Arrange
		PreparedLogEntry capturedEntry = default;

		var logger = new ConsoleDelegateLogger(
			entry => capturedEntry = entry,
			LogLevel.Information,
			"TestCategory");

		// Act
		logger.LogInformation("User {UserId} logged in from {IpAddress}", 123, "192.168.1.1");

		// Assert
		Assert.Equal("User 123 logged in from 192.168.1.1", capturedEntry.Message);
	}
}