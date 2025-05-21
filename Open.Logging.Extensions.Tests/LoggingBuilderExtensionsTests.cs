using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using NSubstitute;

namespace Open.Logging.Extensions.Tests;

public class LoggingBuilderExtensionsTests
{
	[Fact]
	public void AddSpecializedConsoleFormatter_WithConfigureOptions_ConfiguresOptions()
	{
		// Arrange
		var testServiceCollection = new ServiceCollection();
		var loggingBuilder = Substitute.For<ILoggingBuilder>();
		loggingBuilder.Services.Returns(testServiceCollection);

		var formatterName = "test-formatter";

		static void handler(TextWriter writer, PreparedLogEntry entry)
			=> writer.Write($"Test: {entry.Message}");

		static void configureOptions(ConsoleFormatterOptions options)
		{
			options.IncludeScopes = true;
			options.TimestampFormat = "HH:mm:ss ";
		}

		// Act
		loggingBuilder.AddSpecializedConsoleFormatter(formatterName, handler, configureOptions);

		// Assert - Verify that Configure was called on the service collection
		// Check if any service descriptor was added that configures options
		var configurationDescriptor = testServiceCollection.FirstOrDefault(static sd =>
			sd.ServiceType.FullName?.Contains("IConfigureOptions", StringComparison.Ordinal) == true);

		Assert.NotNull(configurationDescriptor);
	}

	[Fact]
	public void AddSpecializedConsoleFormatter_WithNullBuilder_ThrowsArgumentNullException()
	{
		// Arrange
		ILoggingBuilder? nullBuilder = null;
		static void handler(TextWriter writer, PreparedLogEntry entry) { }

		// Act & Assert
		var exception = Assert.Throws<ArgumentNullException>(() =>
			nullBuilder!.AddSpecializedConsoleFormatter("formatter", handler));

		Assert.Equal("builder", exception.ParamName);
	}

	[Fact]
	public void AddSpecializedConsoleFormatter_WithEmptyName_ThrowsArgumentException()
	{
		// Arrange
		var loggingBuilder = Substitute.For<ILoggingBuilder>();
		static void handler(TextWriter writer, PreparedLogEntry entry) { }

		// Act & Assert
		var exception = Assert.Throws<ArgumentException>(() =>
			loggingBuilder.AddSpecializedConsoleFormatter("", handler));

		Assert.Contains("Formatter name must be provided", exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void AddSpecializedConsoleFormatter_WithNullHandler_ThrowsArgumentNullException()
	{
		// Arrange
		var loggingBuilder = Substitute.For<ILoggingBuilder>();
		Action<TextWriter, PreparedLogEntry>? nullHandler = null;

		// Act & Assert
		var exception = Assert.Throws<ArgumentNullException>(() =>
			loggingBuilder.AddSpecializedConsoleFormatter("formatter", nullHandler!));

		Assert.Equal("handler", exception.ParamName);
	}
}