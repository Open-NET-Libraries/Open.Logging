using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Open.Logging.Extensions.Memory;

namespace Open.Logging.Extensions.Tests;

/// <summary>
/// Tests for the MemoryLoggerBuilderExtensions.
/// </summary>
public class MemoryLoggerBuilderExtensionsTests
{
	[Fact]
	public void AddMemoryLogger_RegistersProvider()
	{
		// Arrange
		var services = new ServiceCollection();

		// Act
		services.AddLogging(builder => builder.AddMemoryLogger());
		var serviceProvider = services.BuildServiceProvider();

		// Assert
		var provider = serviceProvider.GetService<IMemoryLoggerProvider>();

		Assert.NotNull(provider);
	}

	[Fact]
	public void AddMemoryLogger_WithAction_ConfiguresOptions()
	{
		// Skip this test until MemoryLoggerProvider fully supports options
		// This will be implemented in a future PR

		// Arrange
		var services = new ServiceCollection();

		// Act
		services.AddLogging(builder => builder.AddMemoryLogger(options =>
		{
			options.MaxCapacity = 500;
			options.MinLogLevel = LogLevel.Warning;
			options.IncludeScopes = false;
		}));

		var serviceProvider = services.BuildServiceProvider();

		// Assert
		var options = serviceProvider.GetService<IOptions<MemoryLoggerOptions>>();
		Assert.NotNull(options);

		var optionsValue = options.Value;
		Assert.Equal(500, optionsValue.MaxCapacity);
		Assert.Equal(LogLevel.Warning, optionsValue.MinLogLevel);
		Assert.False(optionsValue.IncludeScopes);
	}

	[Fact]
	public void AddMemoryLogger_WithConfiguration_RegistersProvider()
	{
		// Arrange
		var configValues = new Dictionary<string, string?>
		{
			["Logging:Memory:MaxCapacity"] = "2000",
			["Logging:Memory:MinLogLevel"] = "Error",
			["Logging:Memory:IncludeScopes"] = "false"
		};

		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(configValues)
			.Build();

		var services = new ServiceCollection();
		services.AddSingleton<IConfiguration>(configuration);

		// Act
		services.AddLogging(builder =>
		{
			builder.AddConfiguration(configuration.GetSection("Logging"));
			builder.AddMemoryLogger();
		});

		var serviceProvider = services.BuildServiceProvider();

		// Assert
		var provider = serviceProvider.GetService<IMemoryLoggerProvider>();
		Assert.NotNull(provider);
	}
}