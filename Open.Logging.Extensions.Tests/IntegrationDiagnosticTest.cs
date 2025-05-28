using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Open.Logging.Extensions.FileSystem;
using Open.Logging.Extensions.Memory;

namespace Open.Logging.Extensions.Tests;

/// <summary>
/// Diagnostic test to understand why integration tests fail while isolated tests work.
/// </summary>
public sealed class IntegrationDiagnosticTest
{
	[Fact]
	public void DiagnosticTest_IntegrationStyleSetup_ShouldWork()
	{
		// Arrange - Exactly like the failing integration test
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Logging:LogLevel:Default"] = "Information",
				["Logging:Memory:LogLevel:Default"] = "Debug",
				["Logging:Memory:MaxCapacity"] = "1000"
			})
			.Build();

		var services = new ServiceCollection();
		services.AddSingleton<IConfiguration>(configuration);

		services.AddLogging(builder =>
		{
			builder.ClearProviders(); // Start clean
			builder.AddConfiguration(configuration.GetSection("Logging"));
			builder.AddMemoryLogger(); // Uses "Logging:Memory" section
		});

		using var serviceProvider = services.BuildServiceProvider();
		var logger = serviceProvider.GetRequiredService<ILogger<IntegrationDiagnosticTest>>();
		var memoryLoggerProvider = serviceProvider.GetRequiredService<IMemoryLoggerProvider>();

		// Act
		logger.LogInformation("Test message");

		// Assert
		var entries = memoryLoggerProvider.Snapshot();
		Assert.NotEmpty(entries);
		Assert.Equal("Test message", entries[0].Message);
	}

	[Fact]
	public void DiagnosticTest_IsolatedStyleSetup_ShouldWork()
	{
		// Arrange - Exactly like the working isolated test
		var services = new ServiceCollection();
		services.AddLogging(builder =>
		{
			builder.ClearProviders(); // Start clean
			builder.AddMemoryLogger();
		});

		using var serviceProvider = services.BuildServiceProvider();
		var logger = serviceProvider.GetRequiredService<ILogger<IntegrationDiagnosticTest>>();
		var memoryProvider = serviceProvider.GetRequiredService<IMemoryLoggerProvider>();

		// Act
		logger.LogInformation("Test message");

		// Assert
		var entries = memoryProvider.Snapshot();
		Assert.NotEmpty(entries);
		Assert.Equal("Test message", entries[0].Message);
	}

	[Fact]
	public void DiagnosticTest_CheckProviderRegistration()
	{
		// Test the configuration-based setup to see what's different
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Logging:LogLevel:Default"] = "Information",
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
			builder.AddMemoryLogger();
		});

		using var serviceProvider = services.BuildServiceProvider();

		// Check all registered providers
		var allProviders = serviceProvider.GetServices<ILoggerProvider>().ToList();
		var memoryProviders = allProviders.OfType<IMemoryLoggerProvider>().ToList();
		var memoryProviderViaInterface = serviceProvider.GetService<IMemoryLoggerProvider>();

		// Diagnostics
		Assert.NotEmpty(allProviders);
		Assert.Single(memoryProviders);
		Assert.NotNull(memoryProviderViaInterface);
		Assert.Same(memoryProviders[0], memoryProviderViaInterface);
	}

	[Fact]
	public void DiagnosticTest_BothFileAndMemory_ShouldWork()
	{
		// Arrange - Test both loggers together like the failing integration test
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Logging:LogLevel:Default"] = "Information",
				["Logging:File:LogLevel:Default"] = "Debug",
				["Logging:File:Directory"] = Path.GetTempPath(),
				["Logging:File:FileNamePattern"] = "diagnostic-test.log",
				["Logging:Memory:LogLevel:Default"] = "Debug",
				["Logging:Memory:MaxCapacity"] = "1000"
			})
			.Build();

		var services = new ServiceCollection();
		services.AddSingleton<IConfiguration>(configuration);

		services.AddLogging(builder =>
		{
			builder.ClearProviders(); // Start clean
			builder.AddConfiguration(configuration.GetSection("Logging"));
			builder.AddFileLogger();   // Add file logger first
			builder.AddMemoryLogger(); // Add memory logger second
		});

		using var serviceProvider = services.BuildServiceProvider();
		var logger = serviceProvider.GetRequiredService<ILogger<IntegrationDiagnosticTest>>();
		var memoryLoggerProvider = serviceProvider.GetRequiredService<IMemoryLoggerProvider>();

		// Act
		logger.LogInformation("Test message with both loggers");

		// Assert
		var entries = memoryLoggerProvider.Snapshot();
		Assert.NotEmpty(entries);
		Assert.Equal("Test message with both loggers", entries[0].Message);
	}

	[Fact]
	public void DiagnosticTest_ConfigurationOrder_ShouldWork()
	{
		// Test different orders of configuration to understand the issue
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
			builder.ClearProviders(); // Start clean
			builder.AddConfiguration(configuration.GetSection("Logging"));
			builder.SetMinimumLevel(LogLevel.Debug); // Explicitly set minimum level
			builder.AddMemoryLogger();
		});

		using var serviceProvider = services.BuildServiceProvider();
		var logger = serviceProvider.GetRequiredService<ILogger<IntegrationDiagnosticTest>>();
		var memoryLoggerProvider = serviceProvider.GetRequiredService<IMemoryLoggerProvider>();

		// Act
		logger.LogDebug("Debug message");
		logger.LogInformation("Info message");

		// Assert
		var entries = memoryLoggerProvider.Snapshot();
		Assert.NotEmpty(entries);
		// Should have both debug and info
		Assert.Contains(entries, e => e.Message.Contains("Debug message", StringComparison.Ordinal));
		Assert.Contains(entries, e => e.Message.Contains("Info message", StringComparison.Ordinal));
	}
}
