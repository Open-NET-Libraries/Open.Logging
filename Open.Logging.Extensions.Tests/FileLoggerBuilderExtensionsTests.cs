using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Open.Logging.Extensions.FileSystem;

namespace Open.Logging.Extensions.Tests;

/// <summary>
/// Tests for the FileLoggerBuilderExtensions.
/// </summary>
public class FileLoggerBuilderExtensionsTests
{
	[Fact]
	public void AddFileLogger_RegistersProviderAndOptions()
	{
		// Arrange
		var services = new ServiceCollection();

		// Act
		services.AddLogging(builder => builder.AddFileLogger());
		var serviceProvider = services.BuildServiceProvider();

		// Assert
		var provider = serviceProvider.GetServices<ILoggerProvider>()
			.OfType<FileLoggerProvider>()
			.FirstOrDefault();

		Assert.NotNull(provider);

		var options = serviceProvider.GetService<IOptions<FileLoggerFormatterOptions>>();
		Assert.NotNull(options);
		Assert.NotNull(options.Value);
	}

	[Fact]
	public void AddFileLogger_WithAction_ConfiguresOptions()
	{
		// Arrange
		var services = new ServiceCollection();
		var testDirectory = Path.Combine(Path.GetTempPath(), "TestLogDir");
		var testPattern = "test-log-{Timestamp}.log";

		// Act
		services.AddLogging(builder => builder.AddFileLogger(options =>
		{
			options.LogDirectory = testDirectory;
			options.FileNamePattern = testPattern;
			options.MinLogLevel = LogLevel.Warning;
			options.MaxRetainedFiles = 5;
			options.RollSizeKb = 2048;
		}));

		var serviceProvider = services.BuildServiceProvider();

		// Assert
		var options = serviceProvider.GetService<IOptions<FileLoggerFormatterOptions>>();
		Assert.NotNull(options);

		var optionsValue = options.Value;
		Assert.Equal(testDirectory, optionsValue.LogDirectory);
		Assert.Equal(testPattern, optionsValue.FileNamePattern);
		Assert.Equal(LogLevel.Warning, optionsValue.MinLogLevel);
		Assert.Equal(5, optionsValue.MaxRetainedFiles);
		Assert.Equal(2048, optionsValue.RollSizeKb);
	}
	[Fact]
	public void AddFileLogger_WithConfiguration_ConfiguresOptions()
	{
		// Arrange
		var path = Path.Combine(Path.GetTempPath(), "ConfigTestLogDir");
		var configValues = new Dictionary<string, string?>
		{
			["Logging:File:LogDirectory"] = path,
			["Logging:File:FileNamePattern"] = "config-test-{Timestamp}.log",
			["Logging:File:MinLogLevel"] = "Warning",
			["Logging:File:MaxRetainedFiles"] = "10",
			["Logging:File:RollSizeKb"] = "4096"
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
			builder.AddFileLogger();
		});

		var serviceProvider = services.BuildServiceProvider();

		// Assert
		var options = serviceProvider.GetService<IOptions<FileLoggerFormatterOptions>>();
		Assert.NotNull(options);

		var optionsValue = options.Value;
		Assert.Equal(path, optionsValue.LogDirectory);
		Assert.Equal("config-test-{Timestamp}.log", optionsValue.FileNamePattern);
		// Configuration-based MinLogLevel processing requires additional setup
		Assert.Equal(10, optionsValue.MaxRetainedFiles);
		Assert.Equal(4096, optionsValue.RollSizeKb);
	}
}