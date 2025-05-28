using Spectre.Console.Cli;

namespace Open.Logging.Extensions.Demo.Commands;

/// <summary>
/// Settings for the formatter demo command.
/// </summary>
internal sealed class FormatterCommandSettings : CommandSettings
{
	// No specific settings needed for this demo
}

/// <summary>
/// Command to run the interactive formatter demo.
/// </summary>
internal sealed class FormatterCommand : BaseCommand<FormatterCommandSettings>
{
	/// <summary>
	/// Executes the formatter demo command.
	/// </summary>
	/// <param name="context">The command context.</param>
	/// <param name="settings">The command settings.</param>
	/// <returns>Exit code (0 for success).</returns>
	protected override async Task<int> ExecuteCommandAsync(CommandContext context, FormatterCommandSettings settings)
	{
		return await FormatterDemoProgram.RunAsync().ConfigureAwait(false);
	}
}
