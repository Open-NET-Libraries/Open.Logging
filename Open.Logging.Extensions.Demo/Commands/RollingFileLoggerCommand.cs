using Spectre.Console.Cli;

namespace Open.Logging.Extensions.Demo.Commands;

/// <summary>
/// Settings for the rolling file logger demo command.
/// </summary>
internal sealed class RollingFileLoggerCommandSettings : CommandSettings
{
    // No specific settings needed for this demo
}

/// <summary>
/// Command to run the rolling file logger demo.
/// </summary>
internal sealed class RollingFileLoggerCommand : BaseCommand<RollingFileLoggerCommandSettings>
{
    /// <summary>
    /// Executes the rolling file logger demo command.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The command settings.</param>
    /// <returns>Exit code (0 for success).</returns>
    protected override async Task<int> ExecuteCommandAsync(CommandContext context, RollingFileLoggerCommandSettings settings)
    {
        var emptyArgs = Array.Empty<string>();
        await FileLoggerRollingDemoProgram.RunAsync(emptyArgs).ConfigureAwait(false);
        return 0;
    }
}
