using NSubstitute;
using Open.Logging.Extensions.SpectreConsole;
using Spectre.Console;

namespace Open.Logging.Extensions.Tests;

public class SpectreConsoleExtensionsTests
{
    [Fact]
    public void WriteStyled_WithNullText_DoesNotWriteToConsole()
    {
        // Arrange
        var mockConsole = Substitute.For<IAnsiConsole>();
        var style = new Style(foreground: Color.Red);
        
        // Act
        mockConsole.WriteStyled(null, style);
        
        // Assert
        mockConsole.DidNotReceive().Write(Arg.Any<Text>());
    }
    
    [Fact]
    public void WriteStyled_WithEmptyText_DoesNotWriteToConsole()
    {
        // Arrange
        var mockConsole = Substitute.For<IAnsiConsole>();
        var style = new Style(foreground: Color.Red);
        
        // Act
        mockConsole.WriteStyled("", style);
        
        // Assert
        mockConsole.DidNotReceive().Write(Arg.Any<Text>());
    }
}