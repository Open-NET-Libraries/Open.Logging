using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace Open.Logging.Extensions.Tests;

/// <summary>
/// Advanced tests for TemplateFormatterOptions covering performance, edge cases, and integration scenarios.
/// </summary>
public sealed partial class TemplateFormatterOptionsTests
{
	[Fact]
	public void TokenMap_ContainsAllExpectedTokens()
	{
		// Arrange
		var expectedTokens = new[]
		{
			("NewLine", 0),
			("Timestamp", 1),
			("Elapsed", 2),
			("Category", 3),
			("Scopes", 4),
			("Level", 5),
			("Message", 6),
			("Exception", 7)
		};

		// Act & Assert
		foreach (var (tokenName, expectedValue) in expectedTokens)
		{
			Assert.True(TemplateFormatterOptions.TokenMap.ContainsKey(tokenName));
			Assert.Equal(expectedValue, TemplateFormatterOptions.TokenMap[tokenName]);
		}
		
		Assert.Equal(8, TemplateFormatterOptions.TokenMap.Count);
	}

	[Fact]
	public void TokenMap_IsReadOnly()
	{
		// Arrange & Act
		var tokenMap = TemplateFormatterOptions.TokenMap;

		// Assert
		Assert.True(tokenMap.GetType().Name.Contains("Frozen")); // FrozenDictionary
		
		// Verify we can't modify it (should be immutable)
		Assert.Throws<NotSupportedException>(() => 
		{
			var mutableMap = (IDictionary<string, int>)tokenMap;
			mutableMap.Add("NewToken", 99);
		});
	}

	[Theory]
	[InlineData(1)]
	[InlineData(10)]
	[InlineData(50)]
	[InlineData(100)]
	public void FormatScopes_LargeNumberOfScopes_HandlesEfficiently(int scopeCount)
	{
		// Arrange
		var options = CreateOptions();
		var scopes = Enumerable.Range(1, scopeCount)
			.Select(i => (object)$"Scope{i}")
			.ToArray();

		// Act
		var stopwatch = Stopwatch.StartNew();
		var result = options.FormatScopes(scopes);
		stopwatch.Stop();

		// Assert
		Assert.StartsWith(" > Scope1", result, StringComparison.Ordinal);
		Assert.EndsWith($"Scope{scopeCount}", result, StringComparison.Ordinal);
		Assert.Equal(scopeCount, result.Split(" > ", StringSplitOptions.RemoveEmptyEntries).Length);
		
		// Performance assertion - should complete quickly even with many scopes
		Assert.True(stopwatch.ElapsedMilliseconds < 100, $"FormatScopes took {stopwatch.ElapsedMilliseconds}ms for {scopeCount} scopes");
	}

	[Fact]
	public void Template_ComplexEscapingScenario_WorksCorrectly()
	{
		// Arrange
		var template = @"{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Elapsed:h\:mm\:ss\.fff} {Category} {Message}";

		// Act & Assert
		var expected = @"{1:yyyy-MM-dd HH\:mm\:ss\.fff} {2:h\:mm\:ss\.fff} {3} {6}";
		AssertTemplateTransformation(template, expected);
	}

	[Theory]
	[InlineData("")]
	[InlineData("Simple text")]
	[InlineData("{Message}")]
	[InlineData("{Timestamp:yyyy-MM-dd} - {Message}")]
	[InlineData("{Category}: {Level} - {Message} ({Elapsed})")]
	public void Template_IntegrationTest_ProducesValidOutput(string template)
	{
		// Arrange
		var options = CreateOptions();
		options.Template = template;

		// Act - Simulate actual usage by formatting with real-ish data
		var result = string.Format(
			CultureInfo.InvariantCulture,
			options.TemplateFormatString,
			Environment.NewLine,                    // {0} NewLine
			DateTimeOffset.Now,                     // {1} Timestamp  
			TimeSpan.FromMilliseconds(1234),        // {2} Elapsed
			"MyApp.Services.UserService",           // {3} Category
			options.FormatScopes(["Request", "User:123"]), // {4} Scopes
			"Information",                          // {5} Level
			"User login successful",                // {6} Message
			""                                      // {7} Exception
		);

		// Assert
		Assert.NotNull(result);
		if (!string.IsNullOrEmpty(template))
		{
			Assert.NotEmpty(result);
		}
	}

	[Fact]
	public void Template_LargeTemplate_HandlesEfficiently()
	{
		// Arrange
		var largeTemplate = new StringBuilder();
		for (int i = 0; i < 100; i++)
		{
			largeTemplate.Append($"Section{i}: {{Category}} {{Level}} {{Message}} ");
		}

		// Act
		var stopwatch = Stopwatch.StartNew();
		var options = CreateOptions();
		options.Template = largeTemplate.ToString();
		stopwatch.Stop();

		// Assert
		Assert.NotEmpty(options.TemplateFormatString);
		Assert.True(stopwatch.ElapsedMilliseconds < 500, $"Large template processing took {stopwatch.ElapsedMilliseconds}ms");
		
		// Verify the transformation worked
		Assert.Contains("{3}", options.TemplateFormatString, StringComparison.Ordinal); // Category
		Assert.Contains("{5}", options.TemplateFormatString, StringComparison.Ordinal); // Level  
		Assert.Contains("{6}", options.TemplateFormatString, StringComparison.Ordinal); // Message
	}

	[Fact]
	public void Template_ConcurrentAccess_ThreadSafe()
	{
		// Arrange
		var options = CreateOptions();
		var templates = new[]
		{
			"{Timestamp} {Message}",
			"{Level}: {Category} - {Message}",
			"{Elapsed} | {Message}",
			"{Category} [{Level}] {Message}"
		};
		var results = new ConcurrentBag<string>();
		var exceptions = new ConcurrentBag<Exception>();

		// Act - Simulate concurrent access from multiple threads
		Parallel.ForEach(templates, template =>
		{
			try
			{
				for (int i = 0; i < 10; i++)
				{
					options.Template = template;
					results.Add(options.TemplateFormatString);
				}
			}
			catch (Exception ex)
			{
				exceptions.Add(ex);
			}
		});

		// Assert
		Assert.Empty(exceptions);
		Assert.NotEmpty(results);
		Assert.All(results, result => Assert.NotEmpty(result));
	}

	[Theory]
	[InlineData("{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}", @"{1:yyyy-MM-dd HH\:mm\:ss\.fff zzz}")]
	[InlineData("{Elapsed:c}", "{2:c}")]
	[InlineData("{Elapsed:hh:mm:ss.fff}", @"{2:hh\:mm\:ss\.fff}")]
	[InlineData("{Message,-50:X}", "{6,-50:X}")]
	[InlineData("{Level,10:U}", "{5,10:U}")]
	public void Template_RegexEscaping_HandlesSpecialCharacters(string input, string expected)
	{
		// This tests the FormatExcapeCharacters regex more thoroughly
		AssertTemplateTransformation(input, expected);
	}

	[Fact]
	public void FormatScopes_MemoryEfficiency_DoesNotLeakStrings()
	{
		// Arrange
		var options = CreateOptions();
		var initialMemory = GC.GetTotalMemory(true);

		// Act - Create many scope strings
		for (int i = 0; i < 1000; i++)
		{
			var scopes = Enumerable.Range(1, 10)
				.Select(j => (object)$"TempScope{i}_{j}")
				.ToArray();
			_ = options.FormatScopes(scopes);
		}

		// Force garbage collection
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		var finalMemory = GC.GetTotalMemory(false);

		// Assert - Memory shouldn't grow excessively (allowing for some overhead)
		var memoryIncrease = finalMemory - initialMemory;
		Assert.True(memoryIncrease < 1_000_000, $"Memory increased by {memoryIncrease} bytes, which seems excessive");
	}

	[Fact]
	public void Template_EmptyAndWhitespaceScenarios_HandleGracefully()
	{
		// Arrange & Act & Assert
		AssertTemplateTransformation("", "");
		AssertTemplateTransformation("   ", "   ");
		AssertTemplateTransformation("\t\n\r", "\t\n\r");
		AssertTemplateTransformation("No tokens here", "No tokens here");
	}

	[Theory]
	[InlineData("{{NotAToken}}", "{{NotAToken}}")]
	[InlineData("{{{Category}}}", "{{{3}}}")]
	[InlineData("{{Category}} and {Level}", "{{Category}} and {5}")]
	[InlineData("Text with {{{Category}}} embedded", "Text with {{{3}}} embedded")]
	public void Template_EscapedBraces_PreservesLiteralBraces(string template, string expected)
	{
		AssertTemplateTransformation(template, expected);
	}
}
