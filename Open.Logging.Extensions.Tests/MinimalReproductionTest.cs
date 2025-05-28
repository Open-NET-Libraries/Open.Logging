using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Open.Logging.Extensions.FileSystem;
using Open.Logging.Extensions.Memory;

namespace Open.Logging.Extensions.Tests;

/// <summary>
/// Minimal test to reproduce the memory logger issue with configuration-based setup.
/// </summary>
public sealed class MinimalReproductionTest : FileLoggerTestBase
{
	[Fact]
	public void MinimalMemoryLoggerConfiguration_ShouldCaptureLog()
	{
		// Arrange - Exact same setup as failing integration test
		using var testContext = CreateTestContext(nameof(MinimalMemoryLoggerConfiguration_ShouldCaptureLog));

		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Logging:LogLevel:Default"] = "Debug",
				["Logging:File:LogLevel:Default"] = "Debug",
				["Logging:File:Directory"] = testContext.Directory,
				["Logging:File:FileNamePattern"] = "minimal-{Timestamp:yyyy-MM-dd-HH-mm-ss-fff}.log",
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
			builder.AddFileLogger();   // Uses "Logging:File" section
			builder.AddMemoryLogger(); // Uses "Logging:Memory" section
		});

		using var serviceProvider = services.BuildServiceProvider();
		var logger = serviceProvider.GetRequiredService<ILogger<MinimalReproductionTest>>();
		var memoryLoggerProvider = serviceProvider.GetRequiredService<IMemoryLoggerProvider>();

		Assert.NotNull(memoryLoggerProvider);

		// Act - Single simple log message
		logger.LogInformation("Test message");

		// Assert - Check Memory Logger received the log
		var memoryEntries = memoryLoggerProvider.Snapshot();

		// Debug output to understand what's happening
		var allProviders = serviceProvider.GetServices<ILoggerProvider>().ToList();
		var memoryProviders = allProviders.OfType<IMemoryLoggerProvider>().ToList();

		Assert.NotEmpty(memoryEntries); // This is where it should fail if the issue reproduces
		Assert.Contains(memoryEntries, e => e.Message.Contains("Test message", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public void CompareWithWorkingDirectSetup_ShouldBothWork()
	{
		// Arrange - Working setup (direct configuration)
		var services1 = new ServiceCollection();
		services1.AddLogging(builder =>
		{
			builder.ClearProviders();
			builder.AddMemoryLogger(options =>
			{
				options.MaxCapacity = 1000;
				options.MinLogLevel = LogLevel.Debug;
			});
		});

		using var serviceProvider1 = services1.BuildServiceProvider();
		var logger1 = serviceProvider1.GetRequiredService<ILogger<MinimalReproductionTest>>();
		var memoryProvider1 = serviceProvider1.GetRequiredService<IMemoryLoggerProvider>();

		// Arrange - Configuration-based setup
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Logging:LogLevel:Default"] = "Debug",
				["Logging:Memory:LogLevel:Default"] = "Debug",
				["Logging:Memory:MaxCapacity"] = "1000"
			})
			.Build();

		var services2 = new ServiceCollection();
		services2.AddSingleton<IConfiguration>(configuration);
		services2.AddLogging(builder =>
		{
			builder.ClearProviders();
			builder.AddConfiguration(configuration.GetSection("Logging"));
			builder.SetMinimumLevel(LogLevel.Debug);
			builder.AddMemoryLogger();
		});

		using var serviceProvider2 = services2.BuildServiceProvider();
		var logger2 = serviceProvider2.GetRequiredService<ILogger<MinimalReproductionTest>>();
		var memoryProvider2 = serviceProvider2.GetRequiredService<IMemoryLoggerProvider>();

		// Act - Log to both
		logger1.LogInformation("Direct setup message");
		logger2.LogInformation("Config setup message");

		// Assert - Both should work
		var entries1 = memoryProvider1.Snapshot();
		var entries2 = memoryProvider2.Snapshot();

		Assert.NotEmpty(entries1); // Direct setup should work
		Assert.NotEmpty(entries2); // Configuration setup should also work

		Assert.Contains(entries1, e => e.Message.Contains("Direct setup message", StringComparison.OrdinalIgnoreCase));
		Assert.Contains(entries2, e => e.Message.Contains("Config setup message", StringComparison.OrdinalIgnoreCase));
	}
}
