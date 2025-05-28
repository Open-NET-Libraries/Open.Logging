using System.Globalization;

namespace Open.Logging.Extensions.Tests;

/// <summary>
/// Base partial class for TemplateFormatterOptions tests.
/// Contains shared test utilities and common setup code.
/// </summary>
public sealed partial class TemplateFormatterOptionsTests
{
	/// <summary>
	/// Creates a new TemplateFormatterOptions instance for testing.
	/// </summary>
	private static TemplateFormatterOptions CreateOptions() => new();

	/// <summary>
	/// Validates that a format string can be successfully formatted with test data.
	/// </summary>
	/// <param name="formatString">The format string to validate.</param>
	/// <returns>The formatted result if successful.</returns>
	private static string ValidateFormatString(string formatString)
	{
		return string.Format(
			CultureInfo.InvariantCulture,
			formatString,
			Environment.NewLine,        // {0} NewLine
			DateTimeOffset.Now,         // {1} Timestamp
			TimeSpan.FromSeconds(30),   // {2} Elapsed
			"TestCategory",             // {3} Category
			" > ScopeA > ScopeB",      // {4} Scopes
			"INFO",                     // {5} Level
			"Test message",             // {6} Message
			"Exception details"         // {7} Exception
		);
	}   /// <summary>
		/// Asserts that setting a template throws a FormatException.
		/// </summary>
		/// <param name="template">The invalid template to test.</param>
	private static void AssertTemplateThrowsFormatException(string template)
	{
		var options = CreateOptions();
		var exception = Assert.Throws<FormatException>(() => options.Template = template);
		// The actual implementation throws raw FormatException from string.Format
		Assert.NotNull(exception.Message);
	}

	/// <summary>
	/// Asserts that a template transforms to the expected format string.
	/// </summary>
	/// <param name="template">The input template.</param>
	/// <param name="expectedFormatString">The expected transformed format string.</param>
	private static void AssertTemplateTransformation(string template, string expectedFormatString)
	{
		var options = CreateOptions();
		options.Template = template;
		Assert.Equal(expectedFormatString, options.TemplateFormatString);

		// Ensure the format string is actually valid by testing it
		_ = ValidateFormatString(options.TemplateFormatString);
	}
	/// <summary>
	/// Test data for various scope configurations.
	/// </summary>
	public static readonly object[][] ScopeTestData =
	[
		[Array.Empty<object>(), ""],
		[new object[] { "Single" }, " > Single"],
		[new object[] { "First", "Second" }, " > First > Second"],
		[new object[] { "A", "B", "C", "D" }, " > A > B > C > D"],
		[new object[] { 123, 45.67, true }, " > 123 > 45.67 > True"]
	];
}
