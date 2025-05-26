using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace Open.Logging.Extensions.FileSystem;

/// <summary>
/// Extension methods for adding and configuring <see cref="FileLoggerProvider"/> to the logging builder.
/// </summary>
public static class FileLoggerExtensions
{
	/// <summary>
	/// Adds a file logger that writes logs to a file.
	/// </summary>
	/// <param name="builder">The logging builder to add the file logger provider to.</param>
	/// <returns>The logging builder instance to enable method chaining.</returns>
	public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);

		builder.AddConfiguration();
		builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>());
		builder.Services.TryAddSingleton<IConfigureOptions<FileLoggerFormatterOptions>, FileLoggerOptionsSetup>();

		LoggerProviderOptions.RegisterProviderOptions<FileLoggerFormatterOptions, FileLoggerProvider>(builder.Services);

		return builder;
	}

	/// <summary>
	/// Adds a file logger that writes logs to a file with the specified options.
	/// </summary>
	/// <param name="builder">The logging builder to add the file logger provider to.</param>
	/// <param name="configure">A callback to configure the file logger options.</param>
	/// <returns>The logging builder instance to enable method chaining.</returns>
	public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder, Action<FileLoggerFormatterOptions> configure)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentNullException.ThrowIfNull(configure);

		builder.AddFileLogger();
		builder.Services.Configure(configure);

		return builder;
	}

	/// <summary>
	/// Sets up default options for the file logger.
	/// </summary>
	private sealed class FileLoggerOptionsSetup : IConfigureOptions<FileLoggerFormatterOptions>
	{
		/// <summary>
		/// Configures the specified options.
		/// </summary>
		/// <param name="options">The options to configure.</param>
		public void Configure(FileLoggerFormatterOptions options)
		{
			// Set default options here if needed (most are already set with default values in the class)
			// This allows for override from configuration
		}
	}
}