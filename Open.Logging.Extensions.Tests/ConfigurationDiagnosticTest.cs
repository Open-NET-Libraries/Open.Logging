using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Open.Logging.Extensions.Memory;

namespace Open.Logging.Extensions.Tests;

/// <summary>
/// Diagnostic tests to understand configuration-based memory logger behavior.
/// </summary>
public sealed class ConfigurationDiagnosticTest
{
	[Fact]
	public void ConfigurationBasedMemoryLogger_WithDebugLevel_CapturesLogs()
	{
		// Arrange
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Logging:LogLevel:Default"] = "Debug",
				["Logging:Memory:LogLevel:Default"] = "Debug",
				["Logging:Memory:MaxCapacity"] = "1000"
			})
			.Build();

		var services = new ServiceCollection();
		services.AddSingleton<IConfiguration>(configuration);

		services.AddLogging(builder =>
		{
			builder.ClearProviders();
			builder.AddConfiguration(configuration.GetSection("Logging"));
			builder.SetMinimumLevel(LogLevel.Debug);
			builder.AddMemoryLogger();
		});

		using var serviceProvider = services.BuildServiceProvider();
		var logger = serviceProvider.GetRequiredService<ILogger<ConfigurationDiagnosticTest>>();
		var memoryProvider = serviceProvider.GetRequiredService<IMemoryLoggerProvider>();

		// Act
		logger.LogDebug("Debug message");
		logger.LogInformation("Info message");
		logger.LogWarning("Warning message");

		// Assert
		var entries = memoryProvider.Snapshot();
		Assert.NotEmpty(entries);
		Assert.Contains(entries, e => e.Message.Contains("Debug message", StringComparison.Ordinal));
		Assert.Contains(entries, e => e.Message.Contains("Info message", StringComparison.Ordinal));
		Assert.Contains(entries, e => e.Message.Contains("Warning message", StringComparison.Ordinal));
	}

	[Fact]
	public void SimpleMemoryLogger_WithoutConfiguration_CapturesLogs()
	{
		// Arrange
		var services = new ServiceCollection();

		services.AddLogging(builder =>
		{
			builder.ClearProviders();
			builder.SetMinimumLevel(LogLevel.Debug);
			builder.AddMemoryLogger();
		});

		using var serviceProvider = services.BuildServiceProvider();
		var logger = serviceProvider.GetRequiredService<ILogger<ConfigurationDiagnosticTest>>();
		var memoryProvider = serviceProvider.GetRequiredService<IMemoryLoggerProvider>();

		// Act
		logger.LogDebug("Debug message");
		logger.LogInformation("Info message");
		logger.LogWarning("Warning message");

		// Assert
		var entries = memoryProvider.Snapshot();
		Assert.NotEmpty(entries);
		Assert.Contains(entries, e => e.Message.Contains("Debug message", StringComparison.Ordinal));
		Assert.Contains(entries, e => e.Message.Contains("Info message", StringComparison.Ordinal));
		Assert.Contains(entries, e => e.Message.Contains("Warning message", StringComparison.Ordinal));
	}

	[Fact]
	public void CompareRegistrationMethods_BothShouldWork()
	{
		// Test 1: Configuration-based
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Logging:LogLevel:Default"] = "Debug",
				["Logging:Memory:LogLevel:Default"] = "Debug"
			})
			.Build();

		var services1 = new ServiceCollection();
		services1.AddSingleton<IConfiguration>(configuration);
		services1.AddLogging(builder =>
		{
			builder.ClearProviders();
			builder.AddConfiguration(configuration.GetSection("Logging"));
			builder.SetMinimumLevel(LogLevel.Debug);
			builder.AddMemoryLogger();
		});

		using var serviceProvider1 = services1.BuildServiceProvider();
		var logger1 = serviceProvider1.GetRequiredService<ILogger<ConfigurationDiagnosticTest>>();
		var memoryProvider1 = serviceProvider1.GetRequiredService<IMemoryLoggerProvider>();

		logger1.LogInformation("Config test message");
		var entries1 = memoryProvider1.Snapshot();

		// Test 2: Direct registration
		var services2 = new ServiceCollection();
		services2.AddLogging(builder =>
		{
			builder.ClearProviders();
			builder.SetMinimumLevel(LogLevel.Debug);
			builder.AddMemoryLogger();
		});

		using var serviceProvider2 = services2.BuildServiceProvider();
		var logger2 = serviceProvider2.GetRequiredService<ILogger<ConfigurationDiagnosticTest>>();
		var memoryProvider2 = serviceProvider2.GetRequiredService<IMemoryLoggerProvider>();

		logger2.LogInformation("Direct test message");
		var entries2 = memoryProvider2.Snapshot();

		// Both should have captured logs
		Assert.NotEmpty(entries1);
		Assert.NotEmpty(entries2);
		Assert.Contains(entries1, e => e.Message.Contains("Config test message", StringComparison.Ordinal));
		Assert.Contains(entries2, e => e.Message.Contains("Direct test message", StringComparison.Ordinal));
	}
}
