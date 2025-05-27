using System.Globalization;

namespace Open.Logging.Extensions.Tests;

/// <summary>
/// Tests for the TemplateFormatterOptions class to verify template transformation and formatting.
/// </summary>
public sealed class TemplateFormatterOptionsTests
{
	[Fact]
	public void Template_TransformsTokensToFormatString()
	{
		// Arrange
		var options = new TemplateFormatterOptions();
		var template = "{Timestamp:yyyy-MM-dd} {Elapsed:hh:mm:ss} {Category} [{Level}]: {Message}";

		// Act
		options.Template = template;

		// Assert
		// Based on the Tokens enum:
		// NewLine = 0, Timestamp = 1, Elapsed = 2, Category = 3, Scopes = 4, Level = 5, Message = 6, Exception = 7
		var expected = @"{1:yyyy-MM-dd} {2:hh\:mm\:ss} {3} [{5}]: {6}";
		Assert.Equal(expected, options.TemplateFormatString);
	}

	[Fact]
	public void Template_WithColonInFormat_PreservesFormat()
	{
		// Arrange
		var options = new TemplateFormatterOptions();
		var template = @"{Timestamp:HH:mm\:ss.fff} {Elapsed:hh:mm:ss.fff}";

		// Act
		options.Template = template;

		// Assert
		var expected = @"{1:HH\:mm\:ss\.fff} {2:hh\:mm\:ss\.fff}";
		Assert.Equal(expected, options.TemplateFormatString);
	}

	[Fact]
	public void Template_DefaultValue_CreatesValidFormatString()
	{
		// Arrange & Act
		var options = new TemplateFormatterOptions();

		// Assert
		Assert.NotEmpty(options.TemplateFormatString);
		Assert.NotEqual(options.Template, options.TemplateFormatString);
		// The default template should be transformed properly
		Assert.Contains("{2:", options.TemplateFormatString, StringComparison.Ordinal); // Elapsed token
		Assert.Contains("{3}", options.TemplateFormatString, StringComparison.Ordinal);  // Category token
		Assert.Contains("{5}", options.TemplateFormatString, StringComparison.Ordinal);  // Level token
		Assert.Contains("{6}", options.TemplateFormatString, StringComparison.Ordinal);  // Message token
	}

	[Fact]
	public void Template_ValidatesFormatString_WithTimeSpanFormat()
	{
		// Arrange
		var options = new TemplateFormatterOptions();

		// Act & Assert - should not throw
		var validTemplate = "{Elapsed:hh:mm:ss.fff} {Category} [{Level}]: {Message}";
		options.Template = validTemplate;

		// Verify the transformation
		var expectedFormatString = @"{2:hh\:mm\:ss\.fff} {3} [{5}]: {6}";
		Assert.Equal(expectedFormatString, options.TemplateFormatString);
	}

	[Fact]
	public void Template_InvalidFormatString_ThrowsFormatException()
	{
		// Arrange
		var options = new TemplateFormatterOptions();

		// Act & Assert
		// This should throw because HH:mm:ss.fff is DateTime format, not TimeSpan format
		var invalidTemplate = "{Elapsed:HH:mm:ss.fff} {Category} [{Level}]: {Message}";

		Assert.Throws<FormatException>(() => options.Template = invalidTemplate);
	}

	[Fact]
	public void Template_WithAllTokens_TransformsCorrectly()
	{
		// Arrange
		var options = new TemplateFormatterOptions();
		var template = "{NewLine}{Timestamp:yyyy-MM-dd}{Elapsed:hh:mm:ss}{Category}{Scopes}{Level}{Message}{Exception}";

		// Act
		options.Template = template;

		// Assert
		var expected = @"{0}{1:yyyy-MM-dd}{2:hh\:mm\:ss}{3}{4}{5}{6}{7}";
		Assert.Equal(expected, options.TemplateFormatString);
	}

	[Fact]
	public void Template_WithUnknownToken_LeavesUnchanged()
	{
		// Arrange
		var options = new TemplateFormatterOptions();
		var template = "{Timestamp} {UnknownToken} {Message}";

		// Act & Assert

		Assert.Throws<FormatException>(() => options.Template = template);
	}

	[Fact]
	public void Template_WithComplexFormat_PreservesFormatting()
	{
		// Arrange
		const string expected = @"{1:yyyy-MM-dd HH\:mm\:ss\.fff zzz} {2:c} {6,-50}";
		// Ensure the format is valid
		_ = string.Format(CultureInfo.InvariantCulture, expected,
			Environment.NewLine, DateTimeOffset.Now, TimeSpan.FromSeconds(35), string.Empty, string.Empty, string.Empty, "Hello there.");

		var options = new TemplateFormatterOptions();
		const string tsTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}";
		const string elapsedTemplate = "{Elapsed:c}";
		const string messageTemplate = "{Message,-50}";

		// Act
		options.Template = tsTemplate;
		options.Template = elapsedTemplate;
		options.Template = messageTemplate;
		options.Template = $"{tsTemplate} {elapsedTemplate} {messageTemplate}";

		// Assert
		Assert.Equal(expected, options.TemplateFormatString);
	}

	[Fact]
	public void Template_TokenMappings_WorkAsExpected()
	{
		// Arrange
		var options = new TemplateFormatterOptions
		{
			// Act - Set a template with all tokens to verify their mappings
			Template = "{NewLine}{Timestamp}{Elapsed}{Category}{Scopes}{Level}{Message}{Exception}"
		};

		// Assert - Based on the Tokens enum order
		Assert.Equal("{0}{1}{2}{3}{4}{5}{6}{7}", options.TemplateFormatString);
	}

	[Fact]
	public void FormatScopes_WithEmptyScopes_ReturnsEmpty()
	{
		// Arrange
		var options = new TemplateFormatterOptions();

		// Act & Assert
		Assert.Equal(string.Empty, options.FormatScopes(Array.Empty<object>()));
		Assert.Equal(string.Empty, options.FormatScopes(null!));
	}

	[Fact]
	public void FormatScopes_WithSingleScope_ReturnsSeparatorPlusScope()
	{
		// Arrange
		var options = new TemplateFormatterOptions();
		var scopes = new object[] { "Scope1" };

		// Act
		var result = options.FormatScopes(scopes);

		// Assert
		Assert.Equal(" > Scope1", result);
	}

	[Fact]
	public void FormatScopes_WithMultipleScopes_ReturnsAllScopesWithSeparators()
	{
		// Arrange
		var options = new TemplateFormatterOptions();
		var scopes = new object[] { "Scope1", "Scope2", "Scope3" };

		// Act
		var result = options.FormatScopes(scopes);

		// Assert
		Assert.Equal(" > Scope1 > Scope2 > Scope3", result);
	}

	[Fact]
	public void FormatScopes_WithCustomSeparator_UsesCustomSeparator()
	{
		// Arrange
		var options = new TemplateFormatterOptions { ScopesSeparator = " | " };
		var scopes = new object[] { "Scope1", "Scope2" };

		// Act
		var result = options.FormatScopes(scopes);

		// Assert
		Assert.Equal(" | Scope1 | Scope2", result);
	}
}
