using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

namespace Open.Logging.Extensions.FileSystem;

/// <summary>
/// Extension methods for adding and configuring <see cref="FileLoggerProvider"/> to the logging builder.
/// </summary>
public static class FileLoggerBuilderExtensions
{
	/// <summary>
	/// Adds a file logger that writes logs to a file.
	/// </summary>
	/// <param name="builder">The logging builder to add the file logger provider to.</param>
	/// <returns>The logging builder instance to enable method chaining.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
	public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);

		builder.AddConfiguration();

		// Register the logger provider
		builder.Services.TryAddEnumerable(
			ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>());

		// Register options for the provider - this handles configuration binding automatically
		LoggerProviderOptions.RegisterProviderOptions<
			FileLoggerOptions,
			FileLoggerProvider>(builder.Services);

		return builder;
	}

	/// <summary>
	/// Adds a file logger that writes logs to a file with the specified options.
	/// </summary>
	/// <param name="builder">The logging builder to add the file logger provider to.</param>
	/// <param name="configure">A callback to configure the file logger options.</param>
	/// <returns>The logging builder instance to enable method chaining.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="configure"/> is null.</exception>
	public static ILoggingBuilder AddFileLogger(
		this ILoggingBuilder builder,
		Action<FileLoggerOptions> configure)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentNullException.ThrowIfNull(configure);

		builder.AddFileLogger();
		builder.Services.Configure(configure);

		return builder;
	}
}