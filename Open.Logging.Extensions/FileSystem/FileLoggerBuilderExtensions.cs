using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

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

		// Register options setup for configuration binding
		builder.Services.TryAddSingleton<
			IConfigureOptions<FileLoggerOptions>,
			FileLoggerOptionsSetup>();

		// Register options for the provider
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

	/// <summary>
	/// Sets up default options for the file logger from configuration.
	/// </summary>
	/// <remarks>
	/// Initializes a new instance of the <see cref="FileLoggerOptionsSetup"/> class.
	/// </remarks>
	/// <param name="providerConfiguration">The provider configuration.</param>
	/// <remarks>
	/// Initializes a new instance of the <see cref="FileLoggerOptionsSetup"/> class.
	/// </remarks>
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1812 // Avoid uninstantiated internal classes
	private sealed class FileLoggerOptionsSetup(
		ILoggerProviderConfiguration<FileLoggerProvider> providerConfiguration)
		: IConfigureOptions<FileLoggerOptions>
	{
		/// <summary>
		/// Configures the specified options from configuration.
		/// </summary>
		/// <param name="options">The options to configure.</param>
		public void Configure(FileLoggerOptions options)
		{
			// Load settings from configuration
			providerConfiguration.Configuration.Bind(options);
		}
	}
#pragma warning restore CA1812 // Avoid uninstantiated internal classes
#pragma warning restore IDE0079 // Remove unnecessary suppression
}