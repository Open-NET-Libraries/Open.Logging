namespace Open.Logging.Extensions.Tests;

/// <summary>
/// Tests for template validation functionality in TemplateFormatterOptions.
/// </summary>
public sealed partial class TemplateFormatterOptionsTests
{
	[Fact]
	public void Template_ValidatesFormatString_WithTimeSpanFormat()
	{
		// Arrange
		var validTemplate = "{Elapsed:hh:mm:ss.fff} {Category} [{Level}]: {Message}";

		// Act & Assert - should not throw
		var expected = @"{2:hh\:mm\:ss\.fff} {3} [{5}]: {6}";
		AssertTemplateTransformation(validTemplate, expected);
	}

	[Fact]
	public void Template_InvalidFormatString_ThrowsFormatException()
	{
		// Arrange - HH:mm:ss.fff is DateTime format, not TimeSpan format
		var invalidTemplate = "{Elapsed:HH:mm:ss.fff} {Category} [{Level}]: {Message}";

		// Act & Assert
		AssertTemplateThrowsFormatException(invalidTemplate);
	}
	[Fact]
	public void Template_WithUnknownToken_LeavesTokenUnchanged()
	{
		// Arrange
		var template = "{Timestamp} {UnknownToken} {Message}";
		var options = CreateOptions();

		// Act & Assert - Unknown tokens are left unchanged, which creates invalid format strings
		AssertTemplateThrowsFormatException(template);
	}

	[Fact]
	public void Template_NullValue_ThrowsArgumentNullException()
	{
		// Arrange
		var options = CreateOptions();

		// Act & Assert
		Assert.Throws<ArgumentNullException>(() => options.Template = null!);
	}
	[Theory]
	[InlineData("{Elapsed:INVALID_FORMAT}")]
	public void Template_InvalidFormatSpecifiers_ThrowFormatException(string invalidTemplate)
	{
		AssertTemplateThrowsFormatException(invalidTemplate);
	}

	[Theory]
	[InlineData("{")]                    // Unclosed brace
	[InlineData("}")]                    // Unopened brace
	[InlineData("{Category")]            // Missing closing brace
	[InlineData("Category}")]            // Missing opening brace
	[InlineData("{Category{Level}}")]    // Nested braces
	public void Template_MalformedBraces_ThrowsFormatException(string malformedTemplate)
	{
		AssertTemplateThrowsFormatException(malformedTemplate);
	}

	[Fact]
	public void Template_DefaultValue_CreatesValidFormatString()
	{
		// Arrange & Act
		var options = CreateOptions();

		// Assert
		Assert.NotEmpty(options.TemplateFormatString);
		Assert.NotEqual(options.Template, options.TemplateFormatString);
		
		// The default template should be transformed properly
		Assert.Contains("{2:", options.TemplateFormatString, StringComparison.Ordinal); // Elapsed token
		Assert.Contains("{3}", options.TemplateFormatString, StringComparison.Ordinal);  // Category token
		Assert.Contains("{5}", options.TemplateFormatString, StringComparison.Ordinal);  // Level token
		Assert.Contains("{6}", options.TemplateFormatString, StringComparison.Ordinal);  // Message token
		
		// Ensure the default format string is actually valid
		_ = ValidateFormatString(options.TemplateFormatString);
	}

	[Theory]
	[InlineData("{Elapsed:c}")]          // TimeSpan standard format
	[InlineData("{Elapsed:g}")]          // TimeSpan general format
	[InlineData("{Elapsed:G}")]          // TimeSpan general long format
	[InlineData("{Timestamp:yyyy-MM-dd HH:mm:ss}")]  // DateTime format
	[InlineData("{Timestamp:O}")]        // DateTime ISO format
	[InlineData("{Message,-50}")]        // Left-aligned with width
	[InlineData("{Message,50}")]         // Right-aligned with width
	[InlineData("{Level:U}")]            // Uppercase format
	public void Template_ValidFormatSpecifiers_DoNotThrow(string validTemplate)
	{
		// Act & Assert - should not throw
		var options = CreateOptions();
		options.Template = validTemplate;
		
		// Ensure the format string is actually valid
		_ = ValidateFormatString(options.TemplateFormatString);
	}
}
