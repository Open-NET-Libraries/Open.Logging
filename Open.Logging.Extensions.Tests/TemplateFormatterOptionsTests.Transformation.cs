namespace Open.Logging.Extensions.Tests;

/// <summary>
/// Tests for template transformation functionality in TemplateFormatterOptions.
/// </summary>
public sealed partial class TemplateFormatterOptionsTests
{
	[Fact]
	public void Template_TransformsTokensToFormatString()
	{
		// Arrange
		var template = "{Timestamp:yyyy-MM-dd} {Elapsed:hh:mm:ss} {Category} [{Level}]: {Message}";

		// Act & Assert
		var expected = @"{1:yyyy-MM-dd} {2:hh\:mm\:ss} {3} [{5}]: {6}";
		AssertTemplateTransformation(template, expected);
	}

	[Fact]
	public void Template_WithColonInFormat_PreservesFormat()
	{
		// Arrange
		var template = @"{Timestamp:HH:mm\:ss.fff} {Elapsed:hh:mm:ss.fff}";

		// Act & Assert
		var expected = @"{1:HH\:mm\:ss\.fff} {2:hh\:mm\:ss\.fff}";
		AssertTemplateTransformation(template, expected);
	}

	[Fact]
	public void Template_WithAllTokens_TransformsCorrectly()
	{
		// Arrange
		var template = "{NewLine}{Timestamp:yyyy-MM-dd}{Elapsed:hh:mm:ss}{Category}{Scopes}{Level}{Message}{Exception}";

		// Act & Assert
		var expected = @"{0}{1:yyyy-MM-dd}{2:hh\:mm\:ss}{3}{4}{5}{6}{7}";
		AssertTemplateTransformation(template, expected);
	}

	[Fact]
	public void Template_TokenMappings_WorkAsExpected()
	{
		// Arrange
		var template = "{NewLine}{Timestamp}{Elapsed}{Category}{Scopes}{Level}{Message}{Exception}";

		// Act & Assert
		var expected = "{0}{1}{2}{3}{4}{5}{6}{7}";
		AssertTemplateTransformation(template, expected);
	}

	[Fact]
	public void Template_WithComplexFormat_PreservesFormatting()
	{
		// Arrange
		var tsTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}";
		var elapsedTemplate = "{Elapsed:c}";
		var messageTemplate = "{Message,-50}";
		var template = $"{tsTemplate} {elapsedTemplate} {messageTemplate}";

		// Act & Assert
		var expected = @"{1:yyyy-MM-dd HH\:mm\:ss\.fff zzz} {2:c} {6,-50}";
		AssertTemplateTransformation(template, expected);
	}
	[Theory]
	[InlineData("{Timestamp}", "{1}")]
	[InlineData("{Elapsed:c}", "{2:c}")]
	[InlineData("{Category,-20}", "{3,-20}")]
	[InlineData("{Level}", "{5}")]
	[InlineData("{Message,50}", "{6,50}")]
	[InlineData("{Exception:-10}", "{7:-10}")]
	public void Template_IndividualTokens_TransformCorrectly(string template, string expected)
	{
		AssertTemplateTransformation(template, expected);
	}

	[Fact]
	public void Template_WithMultipleColons_EscapesCorrectly()
	{
		// Arrange
		var template = "{Elapsed:hh:mm:ss.fff}";

		// Act & Assert  
		var expected = @"{2:hh\:mm\:ss\.fff}";
		AssertTemplateTransformation(template, expected);
	}

	[Fact]
	public void Template_WithNestedBraces_HandlesCorrectly()
	{
		// Arrange
		var template = "{{NotAToken}} {Category} {{AlsoNotAToken}}";

		// Act & Assert
		var expected = "{{NotAToken}} {3} {{AlsoNotAToken}}";
		AssertTemplateTransformation(template, expected);
	}

	[Theory]
	[InlineData("Plain text without tokens", "Plain text without tokens")]
	[InlineData("", "")]
	[InlineData("   ", "   ")]
	[InlineData("Multiple {Category} tokens {Level} here", "Multiple {3} tokens {5} here")]
	public void Template_EdgeCases_HandleCorrectly(string template, string expected)
	{
		AssertTemplateTransformation(template, expected);
	}
}
