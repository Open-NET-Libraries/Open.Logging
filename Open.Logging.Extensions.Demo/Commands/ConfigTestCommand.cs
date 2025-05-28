using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Open.Logging.Extensions.FileSystem;
using Spectre.Console.Cli;

namespace Open.Logging.Extensions.Demo.Commands;

/// <summary>
/// Settings for the configuration test command.
/// </summary>
internal sealed class ConfigTestCommandSettings : CommandSettings
{
	// No specific settings needed for this demo
}

/// <summary>
/// Command to run the configuration test demo.
/// </summary>
internal sealed class ConfigTestCommand : BaseCommand<ConfigTestCommandSettings>
{    /// <summary>
	 /// Executes the configuration test command.
	 /// </summary>
	 /// <param name="context">The command context.</param>
	 /// <param name="settings">The command settings.</param>
	 /// <returns>Exit code (0 for success).</returns>
	protected override async Task<int> ExecuteCommandAsync(CommandContext context, ConfigTestCommandSettings settings)
	{
		return await RunConfigurationTestAsync().ConfigureAwait(false);
	}

	/// <summary>
	/// Tests file logger configuration binding.
	/// </summary>
	private static Task<int> RunConfigurationTestAsync()
	{
		System.Console.WriteLine("Testing File Logger Configuration Binding...");
		System.Console.WriteLine();

		// Test different configuration section patterns
		var testPatterns = new Dictionary<string, string>
		{
			["Logging:FileLogger"] = "FileLogger",
			["Logging:FileLoggerProvider"] = "FileLoggerProvider",
			["Logging:File"] = "File"
		};

		foreach (var pattern in testPatterns)
		{
			System.Console.WriteLine($"Testing configuration pattern: {pattern.Key}");
			TestConfigurationPattern(pattern.Key, pattern.Value);
			System.Console.WriteLine();
		}

		return Task.FromResult(0);
	}

	private static void TestConfigurationPattern(string configPrefix, string patternName)
	{
		// Create configuration
		var path = Path.Combine(Path.GetTempPath(), $"ConfigTest_{patternName}");
		var configValues = new Dictionary<string, string?>
		{
			[$"{configPrefix}:LogDirectory"] = path,
			[$"{configPrefix}:FileNamePattern"] = $"config-test-{patternName}-{{Timestamp}}.log",
			[$"{configPrefix}:MaxLogEntries"] = "10",
		};

		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(configValues)
			.Build();

		// Create services
		var services = new ServiceCollection();
		services.AddSingleton<IConfiguration>(configuration);

		// Add logging with file logger and configuration
		services.AddLogging(builder =>
		{
			builder.AddConfiguration(configuration.GetSection("Logging"));
			builder.AddFileLogger();
		});

		var serviceProvider = services.BuildServiceProvider();

		// Get and test the options
		var options = serviceProvider.GetService<IOptions<FileLoggerOptions>>();
		if (options == null)
		{
			System.Console.WriteLine($"❌ Failed: Could not get FileLoggerOptions from DI");
			return;
		}

		var optionsValue = options.Value;

		System.Console.WriteLine($"  Expected LogDirectory: {path}");
		System.Console.WriteLine($"  Actual LogDirectory: {optionsValue.LogDirectory}");
		System.Console.WriteLine($"  Match: {(optionsValue.LogDirectory == path ? "✅" : "❌")}");

		if (optionsValue.LogDirectory == path)
		{
			System.Console.WriteLine($"  🎉 SUCCESS! Configuration pattern '{configPrefix}' works!");
		}
	}
}
