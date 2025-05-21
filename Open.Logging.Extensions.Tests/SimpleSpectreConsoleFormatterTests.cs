using Open.Logging.Extensions.SpectreConsole;

namespace Open.Logging.Extensions.Tests;

public class SimpleSpectreConsoleFormatterTests
{
	[Fact]
	public void GetConsoleFormatter_ReturnsValidFormatter()
	{
		// Arrange
		var formatter = new SimpleSpectreConsoleFormatter();
		var timestamp = DateTimeOffset.Now;

		// Act
		var consoleFormatter = formatter.GetConsoleFormatter("test-formatter", timestamp);

		// Assert
		Assert.NotNull(consoleFormatter);
		Assert.Equal("test-formatter", consoleFormatter.Name);
	}
}