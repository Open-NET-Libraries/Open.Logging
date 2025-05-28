using Microsoft.Extensions.Logging;

namespace Open.Logging.Extensions.Tests;

/// <summary>
/// Tests for properties and configuration in TemplateFormatterOptions.
/// </summary>
public sealed partial class TemplateFormatterOptionsTests
{
	[Fact]
	public void StartTime_DefaultValue_IsReasonablyClose()
	{
		// Arrange
		var beforeCreation = DateTimeOffset.Now;

		// Act
		var options = CreateOptions();
		var afterCreation = DateTimeOffset.Now;

		// Assert
		Assert.True(options.StartTime >= beforeCreation);
		Assert.True(options.StartTime <= afterCreation);
		Assert.True(afterCreation - options.StartTime < TimeSpan.FromSeconds(1));
	}

	[Fact]
	public void StartTime_CanBeSetToCustomValue()
	{
		// Arrange
		var customTime = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
		var options = CreateOptions();

		// Act
		options.StartTime = customTime;

		// Assert
		Assert.Equal(customTime, options.StartTime);
	}

	[Fact]
	public void EntrySeparator_DefaultValue_IsNewLine()
	{
		// Arrange & Act
		var options = CreateOptions();

		// Assert
		Assert.Equal(Environment.NewLine, options.EntrySeparator);
	}
	[Theory]
	[InlineData("")]
	[InlineData("---")]
	[InlineData("\n\n")]
	[InlineData("===")]
	public void EntrySeparator_CanBeSetToVariousValues(string separator)
	{
		// Arrange
		var options = CreateOptions();

		// Act
		options.EntrySeparator = separator;

		// Assert
		Assert.Equal(separator, options.EntrySeparator);
	}

	[Fact]
	public void EntrySeparator_CanBeSetToNull()
	{
		// Arrange
		var options = CreateOptions();

		// Act
		options.EntrySeparator = null;

		// Assert
		Assert.Null(options.EntrySeparator);
	}

	[Fact]
	public void LevelLabels_DefaultValue_IsNull()
	{
		// Arrange & Act
		var options = CreateOptions();

		// Assert
		Assert.Null(options.LevelLabels);
	}

	[Fact]
	public void LevelLabels_CanBeSetToCustomValue()
	{
		// Arrange
		var customLabels = new LogLevelLabels();
		var options = CreateOptions();

		// Act
		options.LevelLabels = customLabels;

		// Assert
		Assert.Same(customLabels, options.LevelLabels);
	}

	[Fact]
	public void MinLogLevel_DefaultValue_IsFromDefaults()
	{
		// Arrange & Act
		var options = CreateOptions();

		// Assert
		Assert.Equal(Defaults.LogLevel, options.MinLogLevel);
	}

	[Theory]
	[InlineData(LogLevel.Trace)]
	[InlineData(LogLevel.Debug)]
	[InlineData(LogLevel.Information)]
	[InlineData(LogLevel.Warning)]
	[InlineData(LogLevel.Error)]
	[InlineData(LogLevel.Critical)]
	[InlineData(LogLevel.None)]
	public void MinLogLevel_CanBeSetToAnyLogLevel(LogLevel logLevel)
	{
		// Arrange
		var options = CreateOptions();

		// Act
		options.MinLogLevel = logLevel;

		// Assert
		Assert.Equal(logLevel, options.MinLogLevel);
	}

	[Fact]
	public void TemplateFormatString_IsReadOnly()
	{
		// Arrange
		var options = CreateOptions();
		var originalFormatString = options.TemplateFormatString;

		// Act & Assert - Property should be read-only (no public setter)
		// This test verifies the property only changes when Template is set
		options.Template = "{Message}";
		Assert.NotEqual(originalFormatString, options.TemplateFormatString);
		Assert.Equal("{6}", options.TemplateFormatString);
	}
	[Fact]
	public void Record_Equality_WorksCorrectly()
	{
		// Arrange
		var fixedTime = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
		var options1 = CreateOptions();
		var options2 = CreateOptions();

		// Set the same StartTime to make them equal
		options1.StartTime = fixedTime;
		options2.StartTime = fixedTime;

		// Act & Assert - Should be equal with same values
		Assert.Equal(options1, options2);
		Assert.True(options1 == options2);
		Assert.False(options1 != options2);

		// After changing a property, they should not be equal
		options1.Template = "{Message}";
		Assert.NotEqual(options1, options2);
		Assert.False(options1 == options2);
		Assert.True(options1 != options2);
	}

	[Fact]
	public void Record_GetHashCode_ChangesWithPropertyChanges()
	{
		// Arrange
		var options = CreateOptions();
		var originalHashCode = options.GetHashCode();

		// Act
		options.Template = "{Level}: {Message}";
		var newHashCode = options.GetHashCode();

		// Assert
		Assert.NotEqual(originalHashCode, newHashCode);
	}
	[Fact]
	public void Record_ToString_ReturnsUsefulRepresentation()
	{
		// Arrange
		var options = CreateOptions();

		// Act
		var stringRepresentation = options.ToString();

		// Assert
		Assert.NotNull(stringRepresentation);
		Assert.Contains(nameof(TemplateFormatterOptions), stringRepresentation, StringComparison.Ordinal);
	}
}
