using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

namespace Open.Logging.Extensions.Memory;

/// <summary>
/// Extension methods for adding and configuring <see cref="MemoryLoggerProvider"/> to the logging builder.
/// </summary>
public static class MemoryLoggerBuilderExtensions
{   /// <summary>
	/// Adds a memory logger provider to the logging builder.
	/// </summary>
	/// <param name="builder">The logging builder to add the memory logger provider to.</param>
	/// <returns>The logging builder instance to enable method chaining.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
	public static ILoggingBuilder AddMemoryLogger(this ILoggingBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);     // Add configuration support
		builder.AddConfiguration();

		// Register the concrete type first to ensure single instance
		builder.Services.TryAddSingleton<MemoryLoggerProvider>();

		// Register as ILoggerProvider using the singleton instance
		builder.Services.TryAddEnumerable(
			ServiceDescriptor.Singleton<ILoggerProvider, MemoryLoggerProvider>(sp =>
				sp.GetRequiredService<MemoryLoggerProvider>()));

		// Register as IMemoryLoggerProvider using the same singleton instance
		builder.Services.TryAddSingleton<IMemoryLoggerProvider>(sp =>
			sp.GetRequiredService<MemoryLoggerProvider>());

		// Register options for the provider - this handles configuration binding automatically
		LoggerProviderOptions.RegisterProviderOptions<
			MemoryLoggerOptions,
			MemoryLoggerProvider>(builder.Services);

		return builder;
	}

	/// <summary>
	/// Adds a memory logger provider to the logging builder with the specified options.
	/// </summary>
	/// <param name="builder">The logging builder to add the memory logger provider to.</param>
	/// <param name="configure">A callback to configure memory logger options.</param>
	/// <returns>The logging builder instance to enable method chaining.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="configure"/> is null.</exception>
	public static ILoggingBuilder AddMemoryLogger(
		this ILoggingBuilder builder,
		Action<MemoryLoggerOptions> configure)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentNullException.ThrowIfNull(configure);

		builder.AddMemoryLogger();
		builder.Services.Configure(configure);

		return builder;
	}
}