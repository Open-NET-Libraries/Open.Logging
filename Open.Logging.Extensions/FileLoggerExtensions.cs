using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

namespace Open.Logging.Extensions;

/// <summary>
/// Extension methods for adding file logging.
/// </summary>
/// <remarks>
/// The file logger provides a flexible and efficient way to log application events to files.
/// It supports features such as:
/// <list type="bullet">
/// <item><description>Customizable log formatting via templates</description></item>
/// <item><description>Automatic file size-based rolling</description></item>
/// <item><description>Log file retention policies</description></item>
/// <item><description>Efficient asynchronous writing via channels</description></item>
/// </list>
/// </remarks>
public static class FileLoggerExtensions
{
	/// <summary>
	/// Adds a file logger to the factory with default options.
	/// </summary>
	/// <param name="builder">The <see cref="ILoggingBuilder"/> to add the file logger to.</param>
	/// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
	/// <remarks>
	/// With default options, logs will be written to a directory named "logs" in the current directory,
	/// using files named like "log_20250523_143000.log" with timestamps in their names.
	/// </remarks>
	/// <example>
	/// <code>
	/// services.AddLogging(builder =>
	/// {
	///     builder.AddFile();
	/// });
	/// </code>
	/// </example>
	public static ILoggingBuilder AddFile(this ILoggingBuilder builder)
		=> builder.AddFile(_ => { });

	/// <summary>
	/// Adds a file logger to the factory with the given configure action.
	/// </summary>
	/// <param name="builder">The <see cref="ILoggingBuilder"/> to add the file logger to.</param>
	/// <param name="configure">A delegate to configure the <see cref="FileFormatterOptions"/>.</param>
	/// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
	/// <remarks>
	/// <para>
	/// This method allows customizing all aspects of the file logger, including directory, file naming,
	/// and advanced features like file rolling and retention policies.
	/// </para>
	/// </remarks>
	/// <example>
	/// Basic configuration:
	/// <code>
	/// services.AddLogging(builder =>
	/// {
	///     builder.AddFile(options =>
	///     {
	///         options.LogDirectory = Path.Combine(Directory.GetCurrentDirectory(), "app-logs");
	///         options.FileNamePattern = "app-{Timestamp:yyyyMMdd}.log";
	///         options.MinLogLevel = LogLevel.Information;
	///     });
	/// });
	/// </code>
	/// 
	/// Advanced configuration with rolling and retention:
	/// <code>
	/// services.AddLogging(builder =>
	/// {
	///     builder.AddFile(options =>
	///     {
	///         options.LogDirectory = Path.Combine(Directory.GetCurrentDirectory(), "app-logs");
	///         options.FileNamePattern = "app-{Timestamp:yyyyMMdd_HHmmss}.log";
	///         options.MinLogLevel = LogLevel.Warning;
	///         
	///         // Enable file rolling at 10MB
	///         options.MaxFileSize = 10 * 1024 * 1024;
	///         
	///         // Keep only the 7 most recent log files
	///         options.MaxRetainedFiles = 7;
	///         
	///         // Custom template 
	///         options.Template = "{Timestamp:HH:mm:ss.fff} [{Level}] {Category}: {Message}{NewLine}{Exception}";
	///     });
	/// });
	/// </code>
	/// </example>
	public static ILoggingBuilder AddFile(this ILoggingBuilder builder, Action<FileFormatterOptions> configure)
	{
		if (builder == null)
			throw new ArgumentNullException(nameof(builder));

		builder.AddConfiguration();

		builder.Services.TryAddEnumerable(
			ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>());

		LoggerProviderOptions.RegisterProviderOptions<FileFormatterOptions, FileLoggerProvider>(builder.Services);

		if (configure != null)
		{
			builder.Services.Configure(configure);
		}

		return builder;
	}

	/// <summary>
	/// Adds a file logger to the factory with options from the configuration.
	/// </summary>
	/// <param name="builder">The <see cref="ILoggingBuilder"/> to add the file logger to.</param>
	/// <param name="sectionName">The name of the configuration section.</param>
	/// <returns>The <see cref="ILoggingBuilder"/> so that additional calls can be chained.</returns>
	/// <remarks>
	/// <para>
	/// This method allows configuring the file logger using application configuration (e.g., appsettings.json).
	/// All FileFormatterOptions properties can be set through configuration.
	/// </para>
	/// </remarks>
	/// <example>
	/// Using with appsettings.json:
	/// <code>
	/// // In Program.cs or Startup.cs
	/// services.AddLogging(builder =>
	/// {
	///     builder.AddFileWithConfiguration("Logging:FileLogger");
	/// });
	/// </code>
	/// 
	/// // In appsettings.json:
	/// <code>
	/// {
	///   "Logging": {
	///     "FileLogger": {
	///       "LogDirectory": "app-logs",
	///       "FileNamePattern": "app-{Timestamp:yyyyMMdd}.log",
	///       "MinLogLevel": "Information",
	///       "MaxFileSize": 10485760,
	///       "MaxRetainedFiles": 7,
	///       "Template": "{Timestamp:HH:mm:ss.fff} [{Level}] {Category}: {Message}{NewLine}{Exception}"
	///     }
	///   }
	/// }
	/// </code>
	/// </example>
	public static ILoggingBuilder AddFileWithConfiguration(this ILoggingBuilder builder, string sectionName)
	{
		if (builder == null)
			throw new ArgumentNullException(nameof(builder));

		if (string.IsNullOrWhiteSpace(sectionName))
			throw new ArgumentException("Section name must be provided.", nameof(sectionName));

		builder.AddConfiguration();

		builder.Services.TryAddEnumerable(
			ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>());

		builder.Services.AddOptions<FileFormatterOptions>()
			.BindConfiguration(sectionName);

		return builder;
	}
}