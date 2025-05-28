using Spectre.Console.Cli;

namespace Open.Logging.Extensions.Demo.Commands;

/// <summary>
/// Settings for the file logger demo command.
/// </summary>
internal sealed class FileLoggerCommandSettings : CommandSettings
{
    // No specific settings needed for this demo
}

/// <summary>
/// Command to run the file logger demo.
/// </summary>
internal sealed class FileLoggerCommand : BaseCommand<FileLoggerCommandSettings>
{
    /// <summary>
    /// Executes the file logger demo command.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The command settings.</param>
    /// <returns>Exit code (0 for success).</returns>
    protected override Task<int> ExecuteCommandAsync(CommandContext context, FileLoggerCommandSettings settings)
    {
        FileLoggerDemoProgram.RunDemo();
        return Task.FromResult(0);
    }
}
