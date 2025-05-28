namespace Open.Logging.Extensions.Tests;

/// <summary>
/// Tests for scope formatting functionality in TemplateFormatterOptions.
/// </summary>
public sealed partial class TemplateFormatterOptionsTests
{
	[Fact]
	public void FormatScopes_WithEmptyScopes_ReturnsEmpty()
	{
		// Arrange
		var options = CreateOptions();

		// Act & Assert
		Assert.Equal(string.Empty, options.FormatScopes(Array.Empty<object>()));
		Assert.Equal(string.Empty, options.FormatScopes(null!));
	}

	[Fact]
	public void FormatScopes_WithSingleScope_ReturnsSeparatorPlusScope()
	{
		// Arrange
		var options = CreateOptions();
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
		var options = CreateOptions();
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
		var options = CreateOptions();
		options.ScopesSeparator = " | ";
		var scopes = new object[] { "Scope1", "Scope2" };

		// Act
		var result = options.FormatScopes(scopes);

		// Assert
		Assert.Equal(" | Scope1 | Scope2", result);
	}

	[Theory]
	[MemberData(nameof(ScopeTestData))]
	public void FormatScopes_VariousInputs_ProducesExpectedOutput(object[] scopes, string expected)
	{
		// Arrange
		var options = CreateOptions();

		// Act
		var result = options.FormatScopes(scopes);

		// Assert
		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData(" -> ", "First", " -> First")]
	[InlineData("=>", "Test", "=>Test")]
	[InlineData("", "NoSeparator", "NoSeparator")]
	[InlineData("   ", "Spaces", "   Spaces")]
	public void FormatScopes_CustomSeparators_WorkCorrectly(string separator, string scope, string expected)
	{
		// Arrange
		var options = CreateOptions();
		options.ScopesSeparator = separator;
		var scopes = new object[] { scope };

		// Act
		var result = options.FormatScopes(scopes);

		// Assert
		Assert.Equal(expected, result);
	}
	[Fact]
	public void FormatScopes_WithComplexObjects_HandlesStringRepresentation()
	{
		// Arrange
		var options = CreateOptions();
		var complexObject = new { Name = "Test", Value = 42 };
		var scopes = new object[] { complexObject, DateTime.Now };

		// Act
		var result = options.FormatScopes(scopes);

		// Assert
		Assert.StartsWith(" > { Name = Test, Value = 42 }", result, StringComparison.Ordinal);
		Assert.Contains(" > ", result, StringComparison.Ordinal);
	}
	[Fact]
	public void FormatScopes_WithNullElementsInArray_HandlesGracefully()
	{
		// Arrange
		var options = CreateOptions();
		var scopes = new object[] { "Valid", "Empty", "AnotherValid" };

		// Act
		var result = options.FormatScopes(scopes);

		// Assert
		Assert.Equal(" > Valid > Empty > AnotherValid", result);
	}

	[Fact]
	public void ScopesSeparator_DefaultValue_IsCorrect()
	{
		// Arrange & Act
		var options = CreateOptions();

		// Assert
		Assert.Equal(" > ", options.ScopesSeparator);
	}
	[Theory]
	[InlineData("")]
	[InlineData(">>>")]
	[InlineData("\n")]
	[InlineData("\t")]
	public void ScopesSeparator_CanBeSetToVariousValues(string separator)
	{
		// Arrange
		var options = CreateOptions();

		// Act
		options.ScopesSeparator = separator;

		// Assert
		Assert.Equal(separator, options.ScopesSeparator);
	}

	[Fact]
	public void ScopesSeparator_CanBeSetToNull()
	{
		// Arrange
		var options = CreateOptions();

		// Act
		options.ScopesSeparator = null!;

		// Assert
		Assert.Null(options.ScopesSeparator);
	}
}
