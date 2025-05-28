using Spectre.Console.Cli;

namespace Open.Logging.Extensions.Demo.Commands;

/// <summary>
/// Base class for all demo commands providing common functionality.
/// </summary>
internal abstract class BaseCommand<TSettings> : AsyncCommand<TSettings>
	where TSettings : CommandSettings
{
	/// <summary>
	/// Executes the command asynchronously.
	/// </summary>
	/// <param name="context">The command context.</param>
	/// <param name="settings">The command settings.</param>
	/// <returns>Exit code (0 for success).</returns>
	public sealed override async Task<int> ExecuteAsync(CommandContext context, TSettings settings)
	{
		try
		{
			return await ExecuteCommandAsync(context, settings).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			Spectre.Console.AnsiConsole.WriteException(ex);
			return 1;
		}
	}

	/// <summary>
	/// Executes the specific command implementation.
	/// </summary>
	/// <param name="context">The command context.</param>
	/// <param name="settings">The command settings.</param>
	/// <returns>Exit code (0 for success).</returns>
	protected abstract Task<int> ExecuteCommandAsync(CommandContext context, TSettings settings);
}
