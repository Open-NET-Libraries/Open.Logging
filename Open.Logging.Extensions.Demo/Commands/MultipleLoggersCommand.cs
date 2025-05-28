using Spectre.Console.Cli;

namespace Open.Logging.Extensions.Demo.Commands;

/// <summary>
/// Settings for the multiple loggers verification command.
/// </summary>
internal sealed class MultipleLoggersCommandSettings : CommandSettings
{
	// No specific settings needed for this demo
}

/// <summary>
/// Command to verify that multiple loggers work together correctly.
/// </summary>
internal sealed class MultipleLoggersCommand : BaseCommand<MultipleLoggersCommandSettings>
{
	/// <summary>
	/// Executes the multiple loggers verification command.
	/// </summary>
	/// <param name="context">The command context.</param>
	/// <param name="settings">The command settings.</param>
	/// <returns>Exit code (0 for success).</returns>
	protected override async Task<int> ExecuteCommandAsync(CommandContext context, MultipleLoggersCommandSettings settings)
	{
		await Examples.MultipleLoggersVerificationDemo.RunVerificationAsync().ConfigureAwait(false);
		return 0;
	}
}
