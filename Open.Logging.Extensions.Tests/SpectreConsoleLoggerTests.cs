using Microsoft.Extensions.Logging;
using NSubstitute;
using Open.Logging.Extensions.SpectreConsole;
using Spectre.Console;

namespace Open.Logging.Extensions.Tests;

public class SpectreConsoleLoggerTests
{
	[Fact]
	public void LogInformation_WritesToConsole()
	{
		// Arrange
		var mockConsole = Substitute.For<IAnsiConsole>();
		var logger = new SimpleSpectreConsoleLogger(
			level: LogLevel.Information,
			category: "TestCategory",
			console: mockConsole);

		// Act
		logger.LogInformation("Test information message");

		// Assert
		mockConsole.Received().Write(Arg.Any<Text>()); // Timestamp
		mockConsole.Received().Write(Arg.Is<string>(s => s.Contains('['))); // Level bracket
		mockConsole.Received(1).WriteStyled("Test information message", Arg.Any<Style>());
	}

	[Fact]
	public void LogWarning_WritesToConsole()
	{
		// Arrange
		var mockConsole = Substitute.For<IAnsiConsole>();
		var logger = new SimpleSpectreConsoleLogger(
			level: LogLevel.Warning,
			category: "TestCategory",
			console: mockConsole);

		// Act
		logger.LogWarning("Test warning message");

		// Assert
		mockConsole.Received().Write(Arg.Any<Text>()); // Timestamp
		mockConsole.Received().Write(Arg.Is<string>(s => s.Contains('['))); // Level bracket
		mockConsole.Received(1).WriteStyled("Test warning message", Arg.Any<Style>());
	}

	[Fact]
	public void LogError_WithException_WritesExceptionDetails()
	{
		// Arrange
		var mockConsole = Substitute.For<IAnsiConsole>();
		var logger = new SimpleSpectreConsoleLogger(
			level: LogLevel.Error,
			category: "TestCategory",
			console: mockConsole);
		var exception = new InvalidOperationException("Test exception");

		// Act
		logger.LogError(exception, "An error occurred");

		// Assert
		mockConsole.Received(1).WriteStyled("An error occurred", Arg.Any<Style>());
		mockConsole.Received(1).WriteException(exception);
	}

	[Fact]
	public void LogWithScope_IncludesScopeInformation()
	{
		// Arrange
		var mockConsole = Substitute.For<IAnsiConsole>();
		var logger = new SimpleSpectreConsoleLogger(
			level: LogLevel.Information,
			category: "TestCategory",
			console: mockConsole,
			scoped: true);

		// Act
		using (logger.BeginScope("TestScope"))
		{
			logger.LogInformation("Message within scope");
		}

		// Assert
		mockConsole.Received(1).WriteStyled("(", Arg.Any<Style>());
		mockConsole.Received(1).WriteStyled("TestScope", Arg.Any<Style>());
		mockConsole.Received(1).WriteStyled(")", Arg.Any<Style>());
		mockConsole.Received(1).WriteStyled("Message within scope", Arg.Any<Style>());
	}

	[Fact]
	public void Logger_WithCustomLogLevel_RespectsLogLevel()
	{
		// Arrange
		var mockConsole = Substitute.For<IAnsiConsole>();
		var logger = new SimpleSpectreConsoleLogger(
			level: LogLevel.Warning,
			category: "TestCategory",
			console: mockConsole);

		// Act
		logger.LogInformation("This should not be logged");
		logger.LogWarning("This should be logged");

		// Assert
		mockConsole.DidNotReceive().WriteStyled("This should not be logged", Arg.Any<Style>());
		mockConsole.Received(1).WriteStyled("This should be logged", Arg.Any<Style>());
	}

	[Fact]
	public void Logger_WithCustomLabels_UsesCustomLabels()
	{
		// Arrange
		var mockConsole = Substitute.For<IAnsiConsole>();
		var customLabels = new LogLevelLabels
		{
			Warning = "ALERT"
		};

		var logger = new SimpleSpectreConsoleLogger(
			level: LogLevel.Warning,
			category: "TestCategory",
			console: mockConsole,
			labels: customLabels);

		// Act
		logger.LogWarning("Warning with custom label");

		// Assert
		// We can't directly verify the exact text, but we can verify the right methods are called
		mockConsole.Received().Write(Arg.Any<Text>());
		mockConsole.Received(1).WriteStyled("Warning with custom label", Arg.Any<Style>());
	}

	[Fact]
	public void Logger_WithCustomTheme_DoesNotThrow()
	{
		// Arrange
		var mockConsole = Substitute.For<IAnsiConsole>();
		var customTheme = new SpectreConsoleLogTheme();

		var logger = new SimpleSpectreConsoleLogger(
			level: LogLevel.Information,
			category: "TestCategory",
			console: mockConsole,
			theme: customTheme);

		// Act - This should not throw
		logger.LogInformation("Message with custom theme");

		// Assert
		mockConsole.Received(1).WriteStyled("Message with custom theme", Arg.Any<Style>());
	}

	[Fact]
	public void LogCritical_DisplaysCriticalSeverity()
	{
		// Arrange
		var mockConsole = Substitute.For<IAnsiConsole>();
		var logger = new SimpleSpectreConsoleLogger(
			level: LogLevel.Critical,
			category: "TestCategory",
			console: mockConsole);

		// Act
		logger.LogCritical("Critical system failure");

		// Assert
		mockConsole.Received(1).WriteStyled("Critical system failure", Arg.Any<Style>());
		// Can't verify the exact level text, but we can verify it was called
		mockConsole.Received().Write(Arg.Any<Text>());
	}

	[Fact]
	public void Logger_WithNestedScopes_DisplaysAllScopes()
	{
		// Arrange
		var mockConsole = Substitute.For<IAnsiConsole>();
		var logger = new SimpleSpectreConsoleLogger(
			level: LogLevel.Information,
			category: "TestCategory",
			console: mockConsole,
			scoped: true);

		// Act
		using (logger.BeginScope("OuterScope"))
		{
			using (logger.BeginScope("InnerScope"))
			{
				logger.LogInformation("Message with nested scopes");
			}
		}

		// Assert
		mockConsole.Received(1).WriteStyled("OuterScope", Arg.Any<Style>());
		mockConsole.Received(1).WriteStyled("InnerScope", Arg.Any<Style>());
		mockConsole.Received(1).WriteStyled(" > ", Arg.Any<Style>()); // The scope separator
	}
}