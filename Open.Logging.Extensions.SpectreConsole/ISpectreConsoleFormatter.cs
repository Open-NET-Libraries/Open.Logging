using Spectre.Console;

namespace Open.Logging.Extensions.SpectreConsole;

/// <summary>
/// This interface defines a contract for formatting log entries for output to the console using Spectre.Console.
/// </summary>
public interface ISpectreConsoleFormatter
{
	/// <summary>
	/// A method that accepts the <paramref name="entry"/> and may be used as a delegate for logging.
	/// </summary>
	/// <param name="entry">The prepared log entry to write.</param>
	void Write(PreparedLogEntry entry);
}

/// <summary>
/// Allows for the creation of a formatter with a specific type <typeparamref name="TFormatter"/>.
/// </summary>
public interface ISpectreConsoleFormatter<TFormatter> : ISpectreConsoleFormatter
	where TFormatter : ISpectreConsoleFormatter
{
	/// <summary>
	/// Factory method to create a new instance of the formatter.
	/// </summary>
	public static abstract TFormatter Create(
		SpectreConsoleLogTheme? theme = null,
		LogLevelLabels? labels = null,
		IAnsiConsole? writer = null);
}
