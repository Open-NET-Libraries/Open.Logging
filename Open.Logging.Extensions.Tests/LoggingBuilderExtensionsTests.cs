using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Open.Logging.Extensions.Tests;

public class LoggingBuilderExtensionsTests
{
	static void VoidHandler(PreparedLogEntry entry, TextWriter writer) { }

	[Fact]
	public void AddSpecializedConsoleFormatter_WithNullBuilder_ThrowsArgumentNullException()
	{
		// Arrange
		ILoggingBuilder? nullBuilder = null;

		// Act & Assert
		var exception = Assert.Throws<ArgumentNullException>(() =>
			nullBuilder!.AddConsoleDelegateFormatter("formatter", VoidHandler));

		Assert.Equal("builder", exception.ParamName);
	}

	[Fact]
	public void AddSpecializedConsoleFormatter_WithEmptyName_ThrowsArgumentException()
	{
		// Arrange
		var loggingBuilder = Substitute.For<ILoggingBuilder>();

		// Act & Assert
		var exception = Assert.Throws<ArgumentException>(() =>
			loggingBuilder.AddConsoleDelegateFormatter("", VoidHandler));

		Assert.Contains("Formatter name must be provided", exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void AddSpecializedConsoleFormatter_WithNullHandler_ThrowsArgumentNullException()
	{
		// Arrange
		var loggingBuilder = Substitute.For<ILoggingBuilder>();
		Action<PreparedLogEntry, TextWriter>? nullHandler = null;

		// Act & Assert
		var exception = Assert.Throws<ArgumentNullException>(() =>
			loggingBuilder.AddConsoleDelegateFormatter("formatter", nullHandler!));

		Assert.Equal("handler", exception.ParamName);
	}
}