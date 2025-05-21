namespace Open.Logging.Extensions;

/// <summary>
/// A collection of default values.
/// </summary>
public static class Default
{
	/// <summary>
	/// The default log level for the logger.
	/// </summary>
	public const Microsoft.Extensions.Logging.LogLevel LogLevel
#if DEBUG
		= Microsoft.Extensions.Logging.LogLevel.Trace;
#else
		= Microsoft.Extensions.Logging.LogLevel.Trace;
#endif

	public static LogLevelLabels LevelLabels => LogLevelLabels.Default;

	public static readonly Action<PreparedLogEntry, TextWriter> Formatter = (entry, writer) =>
	{
		// Write the required prefix.
		var elapsedSeconds = entry.GetElapsed().TotalSeconds;
		writer.Write($"{elapsedSeconds:000.000}s [{LevelLabels.GetLabelForLevel(entry.Level)}]"); // Brackets [xxxx] are easier to search for in logs.

		// Add the potential category name.
		if (!string.IsNullOrWhiteSpace(entry.Category))
		{
			writer.Write($" {entry.Category}");
		}

		// Add the separator between the category and the scope.
		writer.Write(':');

		// Add the scope information if it exists.
		if (entry.Scopes.Count > 0)
		{
			writer.Write(" (");
			for (var i = 0; i < entry.Scopes.Count; i++)
			{
				if (i > 0)
				{
					writer.Write(" > ");
				}

				writer.Write(entry.Scopes[i]);
			}

			writer.Write(')');
		}

		// Add the message text.
		if (!string.IsNullOrWhiteSpace(entry.Message))
		{
			writer.Write(' ');
			writer.Write(entry.Message);
		}

		writer.WriteLine();

		// Add the exception details if they exist.
		if (entry.Exception is not null)
		{
			writer.WriteLine(entry.Exception);
		}
	};
}
