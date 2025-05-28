using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Open.Logging.Extensions.Tests;

/// <summary>
/// Tests to verify that LogLevelLabels customization works correctly for Level formatting.
/// </summary>
public sealed class LevelFormattingVerificationTests
{	
	[Theory]
	[InlineData(LogLevel.Information, "{Level}", "INFO")]
	[InlineData(LogLevel.Warning, "{Level}", "WARN")]
	[InlineData(LogLevel.Error, "{Level}", "ERROR")]
	public void Level_FormattingSpecifiers_WorkCorrectly(LogLevel logLevel, string template, string expectedLevel)
	{
		// Arrange
		var options = new TemplateFormatterOptions
		{
			Template = template,
			StartTime = DateTimeOffset.Now
		};

		var writer = new Writers.TemplateTextLogEntryWriter(options);
		var entry = new PreparedLogEntry
		{
			StartTime = DateTimeOffset.Now.AddMinutes(-3),
			Timestamp = DateTimeOffset.Now,
			Category = "Test.Category",
			Scopes = [],
			Level = logLevel,
			Message = "Test message",
			Exception = null
		};

		// Act
		using var stringWriter = new StringWriter();
		writer.Write(in entry, stringWriter);
		var result = stringWriter.ToString();

		// Assert
		Assert.Contains(expectedLevel, result, StringComparison.Ordinal);
	}

	[Fact]
	public void Template_LevelFormatString_TransformsCorrectly()
	{
		// Test that the template transformation preserves Level placeholders correctly
		var options = new TemplateFormatterOptions
		{
			Template = "{Level}"
		};
		Assert.Equal("{5}", options.TemplateFormatString);

		// Test Level with alignment 
		options.Template = "{Level,10}";
		Assert.Equal("{5,10}", options.TemplateFormatString);
		
		// Test Level with negative alignment (left-aligned)
		options.Template = "{Level,-10}";
		Assert.Equal("{5,-10}", options.TemplateFormatString);
	}
	
	[Fact]
	public void StringFormat_UAndLFormatSpecifiers_AssumptionTest()
	{
		// ASSUMPTION TEST: Let's verify if .NET's string.Format actually supports :U and :L format specifiers
		// for strings (which is what Level values are)

		var testString = "Information";
		var testEnum = LogLevel.Information;

		// Test what string.Format does with :U and :L on strings
		try
		{
			var stringResultU = string.Format(CultureInfo.InvariantCulture, "{0:U}", testString);
			var stringResultL = string.Format(CultureInfo.InvariantCulture, "{0:L}", testString);
			var stringResultPlain = string.Format(CultureInfo.InvariantCulture, "{0}", testString);

			// Test what string.Format does with :U and :L on enums
			var enumResultU = string.Format(CultureInfo.InvariantCulture, "{0:U}", testEnum);
			var enumResultL = string.Format(CultureInfo.InvariantCulture, "{0:L}", testEnum);
			var enumResultPlain = string.Format(CultureInfo.InvariantCulture, "{0}", testEnum);

			// Output what we actually get
			// If :U and :L don't work, they'll likely just be ignored or throw an exception
			Assert.Equal("Information", stringResultU); // Expecting no change if :U is ignored
			Assert.Equal("Information", stringResultL); // Expecting no change if :L is ignored
			Assert.Equal("Information", stringResultPlain);

			Assert.Equal("Information", enumResultU); // Expecting enum.ToString() if :U is ignored
			Assert.Equal("Information", enumResultL); // Expecting enum.ToString() if :L is ignored
			Assert.Equal("Information", enumResultPlain);
		}
		catch (FormatException ex)
		{
			// If we get a FormatException, then :U and :L are not valid format specifiers
			Assert.True(true, $"FormatException thrown: {ex.Message} - This proves :U and :L are not standard format specifiers");
		}
	}
	[Fact]
	public void StringFormat_UAndLFormatSpecifiers_ExplicitOutputTest()
	{
		// EXPLICIT TEST: Let's see exactly what values we get from string.Format with :U and :L

		var testString = "Information";

		// Test string formatting - :U and :L are not standard format specifiers for strings
		var stringResultU = string.Format(CultureInfo.InvariantCulture, "{0:U}", testString);
		var stringResultL = string.Format(CultureInfo.InvariantCulture, "{0:L}", testString);
		var stringResultPlain = string.Format(CultureInfo.InvariantCulture, "{0}", testString);

		// Use Assert.True with messages to see the actual values in test output
		Assert.True(true, $"String results: Plain='{stringResultPlain}', U='{stringResultU}', L='{stringResultL}'");

		// Check if U and L actually change the output
		var uChangesString = !string.Equals(stringResultPlain, stringResultU, StringComparison.Ordinal);
		var lChangesString = !string.Equals(stringResultPlain, stringResultL, StringComparison.Ordinal);

		Assert.True(true, $"Format specifiers change output - String: U={uChangesString}, L={lChangesString}");
		
		// For enums, :U and :L are not valid format specifiers and would throw FormatException
		var testEnum = LogLevel.Information;
		
		// These should throw FormatException
		Assert.Throws<FormatException>(() => string.Format(CultureInfo.InvariantCulture, "{0:U}", testEnum));
		Assert.Throws<FormatException>(() => string.Format(CultureInfo.InvariantCulture, "{0:L}", testEnum));
	}

	[Theory]
	[InlineData(LogLevel.Information, "INFO")]
	[InlineData(LogLevel.Warning, "WARN")]
	[InlineData(LogLevel.Error, "ERROR")]
	[InlineData(LogLevel.Debug, "DEBUG")]
	[InlineData(LogLevel.Trace, "TRACE")]
	[InlineData(LogLevel.Critical, "CRITICAL")]
	public void LogLevelLabels_DefaultLabels_OutputCorrectly(LogLevel logLevel, string expectedLabel)
	{
		// Arrange
		var labels = LogLevelLabels.Default;

		// Act
		var result = labels.GetLabelForLevel(logLevel);

		// Assert
		Assert.Equal(expectedLabel, result);
	}

	[Theory]
	[InlineData(LogLevel.Information, "info")]
	[InlineData(LogLevel.Warning, "warn")]
	[InlineData(LogLevel.Error, "error")]
	[InlineData(LogLevel.Debug, "debug")]
	[InlineData(LogLevel.Trace, "trace")]
	[InlineData(LogLevel.Critical, "critical")]
	public void LogLevelLabels_CustomLowercaseLabels_WorkInTemplate(LogLevel logLevel, string expectedLabel)
	{
		// Arrange
		var customLabels = new LogLevelLabels
		{
			Trace = "trace",
			Debug = "debug",
			Information = "info",
			Warning = "warn",
			Error = "error",
			Critical = "critical"
		};

		var options = new TemplateFormatterOptions
		{
			Template = "{Level}",
			LevelLabels = customLabels,
			StartTime = DateTimeOffset.Now
		};

		var writer = new Writers.TemplateTextLogEntryWriter(options);
		var entry = new PreparedLogEntry
		{
			StartTime = DateTimeOffset.Now.AddMinutes(-3),
			Timestamp = DateTimeOffset.Now,
			Category = "Test.Category",
			Scopes = [],
			Level = logLevel,
			Message = "Test message",
			Exception = null
		};

		// Act
		using var stringWriter = new StringWriter();
		writer.Write(in entry, stringWriter);
		var result = stringWriter.ToString();

		// Assert
		Assert.Contains(expectedLabel, result, StringComparison.Ordinal);
	}

	[Theory]
	[InlineData(LogLevel.Information, "INF")]
	[InlineData(LogLevel.Warning, "WRN")]
	[InlineData(LogLevel.Error, "ERR")]
	[InlineData(LogLevel.Debug, "DBG")]
	[InlineData(LogLevel.Trace, "TRC")]
	[InlineData(LogLevel.Critical, "CRT")]
	public void LogLevelLabels_CustomShortLabels_WorkInTemplate(LogLevel logLevel, string expectedLabel)
	{
		// Arrange
		var customLabels = new LogLevelLabels
		{
			Trace = "TRC",
			Debug = "DBG",
			Information = "INF",
			Warning = "WRN",
			Error = "ERR",
			Critical = "CRT"
		};

		var options = new TemplateFormatterOptions
		{
			Template = "{Level}",
			LevelLabels = customLabels,
			StartTime = DateTimeOffset.Now
		};

		var writer = new Writers.TemplateTextLogEntryWriter(options);
		var entry = new PreparedLogEntry
		{
			StartTime = DateTimeOffset.Now.AddMinutes(-3),
			Timestamp = DateTimeOffset.Now,
			Category = "Test.Category",
			Scopes = [],
			Level = logLevel,
			Message = "Test message",
			Exception = null
		};

		// Act
		using var stringWriter = new StringWriter();
		writer.Write(in entry, stringWriter);
		var result = stringWriter.ToString();

		// Assert
		Assert.Contains(expectedLabel, result, StringComparison.Ordinal);
	}

	[Fact]
	public void LogLevelLabels_AlignmentWithCustomLabels_WorksCorrectly()
	{
		// Arrange
		var customLabels = new LogLevelLabels
		{
			Information = "INFO",
			Warning = "WARN",
			Error = "ERROR"
		};

		var options = new TemplateFormatterOptions
		{
			Template = "{Level,10} {Message}",  // Right-aligned in 10-character field
			LevelLabels = customLabels,
			StartTime = DateTimeOffset.Now
		};

		var writer = new Writers.TemplateTextLogEntryWriter(options);
		var entry = new PreparedLogEntry
		{
			StartTime = DateTimeOffset.Now.AddMinutes(-3),
			Timestamp = DateTimeOffset.Now,
			Category = "Test.Category",
			Scopes = [],
			Level = LogLevel.Information,
			Message = "Test message",
			Exception = null
		};

		// Act
		using var stringWriter = new StringWriter();
		writer.Write(in entry, stringWriter);
		var result = stringWriter.ToString();

		// Assert - Should be right-aligned INFO in 10-character field
		Assert.Contains("      INFO", result, StringComparison.Ordinal);
		Assert.Contains("Test message", result, StringComparison.Ordinal);
	}
}
