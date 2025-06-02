using Spectre.Console.Cli;

namespace Open.Logging.Extensions.Demo.Commands;

/// <summary>
/// Settings for the simple file logger demo command.
/// </summary>
internal sealed class SimpleFileLoggerCommandSettings : CommandSettings
{
    // No specific settings needed for this demo
}

/// <summary>
/// Command to run the simple file logger demo with default settings.
/// </summary>
internal sealed class SimpleFileLoggerCommand : BaseCommand<SimpleFileLoggerCommandSettings>
{
    /// <summary>
    /// Executes the simple file logger demo command.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The command settings.</param>
    /// <returns>Exit code (0 for success).</returns>
    protected override Task<int> ExecuteCommandAsync(CommandContext context, SimpleFileLoggerCommandSettings settings)
    {
        SimpleFileLoggerDemo.RunDemo();
        return Task.FromResult(0);
    }
}