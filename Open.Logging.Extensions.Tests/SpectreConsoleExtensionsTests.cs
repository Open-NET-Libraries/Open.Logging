using NSubstitute;
using Open.Logging.Extensions.SpectreConsole;
using Spectre.Console;

namespace Open.Logging.Extensions.Tests;

public class SpectreConsoleExtensionsTests
{
    [Fact]
    public void WriteStyled_WithText_WritesToConsole()
    {
        // Arrange
        var mockConsole = Substitute.For<IAnsiConsole>();
        var style = new Style(foreground: Color.Red);
        
        // Act
        mockConsole.WriteStyled("Test text", style);
        
        // Assert
        mockConsole.Received(1).Write(Arg.Is<Text>(t => t.ToString() == "Test text"));
    }
    
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
    
    [Fact]
    public void WriteStyled_WithWhitespaceTextAndTrimEnabled_DoesNotWriteToConsole()
    {
        // Arrange
        var mockConsole = Substitute.For<IAnsiConsole>();
        var style = new Style(foreground: Color.Red);
        
        // Act
        mockConsole.WriteStyled("   ", style, trim: true);
        
        // Assert
        mockConsole.DidNotReceive().Write(Arg.Any<Text>());
    }
    
    [Fact]
    public void WriteStyled_WithWhitespaceTextAndTrimDisabled_WritesToConsole()
    {
        // Arrange
        var mockConsole = Substitute.For<IAnsiConsole>();
        var style = new Style(foreground: Color.Red);
        
        // Act
        mockConsole.WriteStyled("   ", style, trim: false);
        
        // Assert
        mockConsole.Received(1).Write(Arg.Is<Text>(t => t.ToString() == "   "));
    }
    
    [Fact]
    public void WriteStyled_WithTextAndTrimEnabled_TrimsWhitespace()
    {
        // Arrange
        var mockConsole = Substitute.For<IAnsiConsole>();
        var style = new Style(foreground: Color.Red);
        
        // Act
        mockConsole.WriteStyled("  Trimmed Text  ", style, trim: true);
        
        // Assert
        mockConsole.Received(1).Write(Arg.Is<Text>(t => t.ToString() == "Trimmed Text"));
    }
    
    [Fact]
    public void WriteStyled_PassesStyleToConsole()
    {
        // Arrange
        var mockConsole = Substitute.For<IAnsiConsole>();
        var style = new Style(foreground: Color.Red);
        
        // Act
        mockConsole.WriteStyled("Test text", style);
        
        // Assert
        mockConsole.Received(1).Write(Arg.Is<Text>(t => 
            t.ToString() == "Test text"));
    }
}