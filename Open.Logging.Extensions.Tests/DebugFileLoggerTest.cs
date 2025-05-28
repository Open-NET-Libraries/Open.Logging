using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Open.Logging.Extensions.FileSystem;

namespace Open.Logging.Extensions.Tests;

public class DebugFileLoggerTest
{
	[Fact]
	public void TestFileLoggerConfiguration()
	{
		// Arrange
		var tempDir = Path.Combine(Path.GetTempPath(), "DebugFileLoggerTest");
		if (Directory.Exists(tempDir))
			Directory.Delete(tempDir, true);

		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Logging:LogLevel:Default"] = "Debug",
				["Logging:File:LogLevel:Default"] = "Debug",
				["Logging:File:LogDirectory"] = tempDir,
				["Logging:File:FileNamePattern"] = "debug-{Timestamp:yyyy-MM-dd-HH-mm-ss-fff}.log"
			})
			.Build();

		var services = new ServiceCollection();
		services.AddSingleton<IConfiguration>(configuration);
		services.AddLogging(builder =>
		{
			builder.ClearProviders();
			builder.AddConfiguration(configuration.GetSection("Logging"));
			builder.SetMinimumLevel(LogLevel.Debug);
			builder.AddFileLogger();
		});

		using var serviceProvider = services.BuildServiceProvider();
		var logger = serviceProvider.GetRequiredService<ILogger<DebugFileLoggerTest>>();

		// Act
		logger.LogInformation("Test message");

		// Give time for async operations
		System.Threading.Thread.Sleep(1000);

		// Check for files
		var logFiles = Directory.Exists(tempDir) ? Directory.GetFiles(tempDir, "*.log") : Array.Empty<string>();
		System.Console.WriteLine($"Temp directory: {tempDir}");
		System.Console.WriteLine($"Directory exists: {Directory.Exists(tempDir)}");
		System.Console.WriteLine($"Found {logFiles.Length} log files");
		foreach (var file in logFiles)
		{
			System.Console.WriteLine($"Log file: {file}");
		}

		// Assert
		Assert.True(Directory.Exists(tempDir), "Log directory should be created");
		Assert.NotEmpty(logFiles);
	}
}
