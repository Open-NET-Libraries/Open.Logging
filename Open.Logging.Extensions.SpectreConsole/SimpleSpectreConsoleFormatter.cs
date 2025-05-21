using Spectre.Console;

namespace Open.Logging.Extensions.SpectreConsole;

public sealed class SimpleSpectreConsoleFormatter(
	SpectreConsoleLogTheme? theme = null,
	LogLevelLabels? labels = null,
	IAnsiConsole? writer = null)
{
	private readonly SpectreConsoleLogTheme _theme = theme ?? SpectreConsoleLogTheme.Default;
	private readonly LogLevelLabels _labels = labels ?? Default.LevelLabels;
	private readonly IAnsiConsole _writer = writer ?? AnsiConsole.Console;

	/// <summary>
	/// Creates a new console formatter with the specified name and timestamp.
	/// </summary>
	public ConsoleDelegateFormatter GetConsoleFormatter(
		string name, DateTimeOffset? timestamp = null)
		=> new(name, Write, timestamp);

	/// <summary>
	/// A method that writes the log entry to the console using Spectre.Console.
	/// </summary>
	/// <param name="entry">The prepared log entry to write.</param>
	public void Write(PreparedLogEntry entry)
	{
		// Timestamp/
		var elapsedSeconds = entry.GetElapsed().TotalSeconds;
		_writer.Write(new Text($"{elapsedSeconds:000.000}s", _theme.Timestamp));

		// Level
		_writer.Write(" [");
		_writer.Write(_theme.GetTextForLevel(entry.Level, _labels));
		_writer.Write("]"); // Brackets [xxxx] are easier to search for in logs.

		// Add the potential category name.
		if (!string.IsNullOrWhiteSpace(entry.Category))
		{
			_writer.Write(" ");
			_writer.WriteStyled(entry.Category, _theme.Category, true);
		}

		// Add the separator between the category and the scope.
		_writer.Write(":");

		// Add the scope information if it exists.
		if (entry.Scopes.Count > 0)
		{
			var style = _theme.Scopes;
			_writer.Write(" ");
			_writer.WriteStyled("(", style);
			for (var i = 0; i < entry.Scopes.Count; i++)
			{
				if (i > 0)
				{
					_writer.WriteStyled(" > ", style);
				}

				_writer.WriteStyled(entry.Scopes[i].ToString(), style);
			}

			_writer.WriteStyled(")", style);
		}

		// Add the message text.
		if (!string.IsNullOrWhiteSpace(entry.Message))
		{
			_writer.Write(" ");
			_writer.WriteStyled(entry.Message, _theme.Message);
		}
		// Add the exception details if they exist.
		if (entry.Exception is not null)
		{
			_writer.Write(Environment.NewLine);
			try
			{
				_writer.WriteException(entry.Exception);
			}
			catch (Exception ex)
			{
				// Fallback if WriteException fails
				_writer.Write($"Exception: {entry.Exception.Message}");
				_writer.Write(Environment.NewLine);
				_writer.Write($"Stack Trace: {entry.Exception.StackTrace}");
			}
		}
	}

	/// <remarks>Uses a lock on the writer to ensure only one log entry at a time.</remarks>
	/// <inheritdoc cref="Write" />
	public void WriteSynchronized(PreparedLogEntry entry)
	{
		// By using the injected writer, other locks can be applied to the same writer.
		lock (_writer)
		{
			Write(entry);
		}
	}
}
