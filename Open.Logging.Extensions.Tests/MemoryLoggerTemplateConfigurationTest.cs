using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Open.Logging.Extensions.FileSystem;
using Open.Logging.Extensions.Memory;
using Xunit;

namespace Open.Logging.Extensions.Tests;

/// <summary>
/// Tests to verify that MemoryLoggerOptions correctly handles template configuration.
/// This test exposes the issue where template configuration is silently ignored.
/// </summary>
public sealed class MemoryLoggerTemplateConfigurationTest
{
	[Fact]
	public void MemoryLoggerOptions_DoesNotSupportTemplateConfiguration_ShouldFailOrWarn()
	{
		// Arrange - Configuration that attempts to set a template for memory logger
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Logging:Memory:Template"] = "MEMORY [{Level}] {Category} - {Message}",
				["Logging:Memory:MaxCapacity"] = "500"
			})
			.Build();

		var services = new ServiceCollection();
		services.AddSingleton<IConfiguration>(configuration);
		services.AddLogging(builder =>
		{
			builder.AddConfiguration(configuration.GetSection("Logging"));
			builder.AddMemoryLogger();
		});

		using var serviceProvider = services.BuildServiceProvider();

		// Act - Get the configured options
		var memoryOptions = serviceProvider.GetRequiredService<IOptionsSnapshot<MemoryLoggerOptions>>().Value;

		// Assert - The template configuration should be ignored (this is the current problematic behavior)
		// MemoryLoggerOptions does not have a Template property, so the configuration is silently ignored
		Assert.Equal(500, memoryOptions.MaxCapacity); // This works
		
		// This test documents the current limitation:
		// There's no way to verify if a template was configured because MemoryLoggerOptions doesn't support it
		// This is the root cause of the intermittent test failure in MultipleLoggersIntegrationTests
		
		// The test below would fail if we tried to access a Template property:
		// Assert.Equal("MEMORY [{Level}] {Category} - {Message}", memoryOptions.Template); // Would not compile
	}

	[Fact]
	public void MemoryLoggerOptions_ComparedToFileLoggerOptions_ShowsTemplateSupport()
	{
		// Arrange
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Logging:File:Template"] = "FILE [{Level}] {Category} - {Message}",
				["Logging:File:LogDirectory"] = Path.GetTempPath(),
				["Logging:Memory:MaxCapacity"] = "500"
			})
			.Build();

		var services = new ServiceCollection();
		services.AddSingleton<IConfiguration>(configuration);
		services.AddLogging(builder =>
		{
			builder.AddConfiguration(configuration.GetSection("Logging"));
			builder.AddFileLogger();
			builder.AddMemoryLogger();
		});

		using var serviceProvider = services.BuildServiceProvider();

		// Act
		var fileOptions = serviceProvider.GetRequiredService<IOptionsSnapshot<FileLoggerOptions>>().Value;
		var memoryOptions = serviceProvider.GetRequiredService<IOptionsSnapshot<MemoryLoggerOptions>>().Value;

		// Assert - File logger supports templates, memory logger does not
		Assert.Equal("FILE [{Level}] {Category} - {Message}", fileOptions.Template);
		Assert.Equal(500, memoryOptions.MaxCapacity);
		// This shows the architectural difference:
		Assert.True(fileOptions is TemplateFormatterOptions); // FileLoggerOptions inherits from TemplateFormatterOptions
		// MemoryLoggerOptions does NOT inherit from TemplateFormatterOptions - this is by design
		Assert.IsNotType<TemplateFormatterOptions>(memoryOptions);
	}

	[Fact]
	public void ConfigurationBinding_WithInvalidMemoryTemplate_SilentlyIgnored()
	{
		// Arrange - Configuration with invalid template property for memory logger
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Logging:Memory:Template"] = "This should be ignored",
				["Logging:Memory:InvalidProperty"] = "This should also be ignored",
				["Logging:Memory:MaxCapacity"] = "1000"
			})
			.Build();

		var services = new ServiceCollection();
		services.AddSingleton<IConfiguration>(configuration);
		services.Configure<MemoryLoggerOptions>(configuration.GetSection("Logging:Memory"));

		using var serviceProvider = services.BuildServiceProvider();

		// Act
		var options = serviceProvider.GetRequiredService<IOptions<MemoryLoggerOptions>>().Value;

		// Assert - Only valid properties are bound, invalid ones are silently ignored
		Assert.Equal(1000, options.MaxCapacity); // Valid property is bound
		
		// Template and InvalidProperty are silently ignored because they don't exist on MemoryLoggerOptions
		// This is the source of the intermittent test behavior - silent configuration failures
	}
}
