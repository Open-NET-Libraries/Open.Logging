using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Open.Logging.Extensions.Demo;

internal sealed class FileLoggerDemoProgram
{
	public static void RunDemo()
	{
		System.Console.Clear();
		var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
		var fileNamePattern = "demo_{Timestamp}.log";
		AnsiConsole.Write(
		  new Rule("[bold green]File Logger Demo[/]")
		  {
			  Style = Style.Parse("green")
		  });
		AnsiConsole.WriteLine();
		string actualLogFilePath = "Unknown";

		AnsiConsole.Status()
			.Spinner(Spinner.Known.Star)
			.SpinnerStyle(Style.Parse("green"))
			.Start("Setting up logging...", ctx =>
			{
				// Configure services
				var services = new ServiceCollection();

				// Add logging with file logger
				services.AddLogging(builder =>
				{
					builder.AddFileLog(options =>
					{
						options.LogDirectory = logDirectory;
						options.FileNamePattern = fileNamePattern;
						options.MinLogLevel = LogLevel.Debug;
					});
				});

				ctx.Status("Creating service provider...");

				// Build the service provider
				using var serviceProvider = services.BuildServiceProvider();

				// Create a logger factory and get a logger
				var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
				var logger = loggerFactory.CreateLogger<FileLoggerDemoProgram>();

				// Get the file logger provider to access the actual file path
				var fileLoggerProvider = serviceProvider.GetServices<ILoggerProvider>()
					.OfType<FileLoggerProvider>()
					.FirstOrDefault();

				// Store the actual file path for later display
				actualLogFilePath = fileLoggerProvider?.FilePath ?? "Unknown";

				ctx.Status("Writing log entries...");

				// Log some messages with different levels
				using (logger.BeginScope("Outer Scope"))
				{
					logger.LogInformation("Starting file logger demo");

					// Log with inner scope
					using (logger.BeginScope("Inner Scope"))
					{
						logger.LogDebug("This is a debug message");
						logger.LogInformation("This is an information message");
						logger.LogWarning("This is a warning message");
						try
						{
							// Create a deeper stack trace
							SimulateDeepOperation();
						}
						catch (Exception ex)
						{
							logger.LogError(ex, "An error occurred during deep operation");
						}
					}

					logger.LogInformation("Completed file logger demo");
				}

				ctx.Status("Completing demo...");

				// The service provider disposal will close the channel and flush all logs
			});        // Display a summary of the log entries created
		AnsiConsole.WriteLine();
		AnsiConsole.Write(
			new Table()
				.AddColumn(new TableColumn("Log Information").Centered())
				.AddRow($"[green]File logger demo completed[/]")
				.AddRow($"Log directory: [yellow]{logDirectory.EscapeMarkup()}[/]")
				.AddRow($"Log file pattern: [yellow]{fileNamePattern.EscapeMarkup()}[/]")
				.AddRow($"Actual log file: [blue]{actualLogFilePath.EscapeMarkup()}[/]")
				.BorderColor(Color.Green)
		);

		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine($"[bold]Log file location:[/] [underline blue]{actualLogFilePath.EscapeMarkup()}[/]");
		AnsiConsole.Markup("[dim]You can copy and paste the path above to open the log file in your editor.[/]");
		AnsiConsole.WriteLine();
		AnsiConsole.WriteLine();

		// Try to read and display the actual log content if the file exists
		if (File.Exists(actualLogFilePath))
		{
			try
			{
				var actualLogContent = File.ReadAllText(actualLogFilePath);
				AnsiConsole.Write(
					new Panel(actualLogContent.EscapeMarkup())
					.Border(BoxBorder.Rounded)
					.Header($"[bold green]Actual Log Content from: {Path.GetFileName(actualLogFilePath)}[/]")
				);
			}
			catch (Exception ex)
			{
				AnsiConsole.MarkupLine($"[red]Could not read log file: {ex.Message}[/]");

				// Fallback to showing the expected format
				AnsiConsole.Markup("[bold]Expected log format:[/]");
				AnsiConsole.WriteLine();

				var sampleLogContent = """
					00:00:01.123 FileLoggerDemoProgram > Outer Scope > Inner Scope
					[info] This is an information message

					00:00:02.123 FileLoggerDemoProgram > Outer Scope > Inner Scope
					[warn] This is a warning message

					00:00:03.123 FileLoggerDemoProgram > Outer Scope > Inner Scope
					[err!] An error occurred
					System.InvalidOperationException: This is a test exception
					   at Open.Logging.Extensions.Demo.FileLoggerDemoProgram...
					""";

				AnsiConsole.Write(
					new Panel(sampleLogContent.EscapeMarkup())
					.Border(BoxBorder.Rounded)
					.Header("Expected Log Format")
				);
			}
		}
		else
		{
			AnsiConsole.MarkupLine($"[red]Log file not found at: {actualLogFilePath}[/]");
		}
	}

	/// <summary>
	/// Simulates a deep operation to create a more complex stack trace.
	/// </summary>
	private static void SimulateDeepOperation()
	{
		ProcessBusinessLogic();
	}

	/// <summary>
	/// Simulates business logic processing.
	/// </summary>
	private static void ProcessBusinessLogic()
	{
		ValidateInputData();
	}

	/// <summary>
	/// Simulates input validation that eventually fails.
	/// </summary>
	private static void ValidateInputData()
	{
		CheckDataIntegrity();
	}

	/// <summary>
	/// Simulates data integrity check that throws an exception.
	/// </summary>
	private static void CheckDataIntegrity()
	{
		PerformDatabaseOperation();
	}

	/// <summary>
	/// Simulates a database operation that fails.
	/// </summary>
	private static void PerformDatabaseOperation()
	{
		throw new InvalidOperationException("Database connection failed during integrity check. The operation could not be completed due to a constraint violation in the user_data table.");
	}
}
