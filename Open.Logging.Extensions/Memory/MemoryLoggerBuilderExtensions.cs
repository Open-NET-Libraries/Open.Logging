using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Open.Logging.Extensions.Memory;

/// <summary>
/// Extension methods for adding and configuring <see cref="MemoryLoggerProvider"/> to the logging builder.
/// </summary>
public static class MemoryLoggerBuilderExtensions
{
	/// <summary>
	/// Adds a memory logger provider to the logging builder.
	/// </summary>
	/// <param name="builder">The logging builder to add the memory logger provider to.</param>
	/// <returns>The memory logger provider that was added.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
	public static ILoggingBuilder AddMemoryLogger(this ILoggingBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);
		builder.Services.TryAddSingleton<IMemoryLoggerProvider, MemoryLoggerProvider>();
		return builder;
	}
}