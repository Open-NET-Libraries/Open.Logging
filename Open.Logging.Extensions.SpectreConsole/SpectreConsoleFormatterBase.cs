using Microsoft.Extensions.Logging;
using Open.Logging.Extensions.Console;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Open.Logging.Extensions.SpectreConsole;

/// <summary>
/// A formatter that outputs log entries to the console using Spectre.Console for enhanced visual styling.
/// </summary>
/// <param name="theme">The theme to use for console output styling. If null, uses <see cref="SpectreConsoleLogTheme.Default"/>.</param>
/// <param name="labels">The labels to use for different log levels. If null, uses <see cref="Defaults.LevelLabels"/>.</param>
/// <param name="newLine">Whether to add a new line after each log entry. Defaults to <see langword="false"/>.</param>
/// <param name="writer">The console writer to use. If null, uses <see cref="AnsiConsole.Console"/>.</param>
public abstract class SpectreConsoleFormatterBase(
	SpectreConsoleLogTheme? theme = null,
	LogLevelLabels? labels = null,
	bool newLine = false,
	IAnsiConsole? writer = null)
	: ISpectreConsoleFormatter
{
	/// <summary>
	/// The theme to use for console output styling. If null, uses <see cref="SpectreConsoleLogTheme.Default"/>.
	/// </summary>
	protected SpectreConsoleLogTheme Theme { get; } = theme ?? SpectreConsoleLogTheme.Default;

	/// <summary>
	/// The labels to use for different log levels. If null, uses <see cref="Defaults.LevelLabels"/>.
	/// </summary>
	protected LogLevelLabels Labels { get; } = labels ?? Defaults.LevelLabels;

	/// <summary>
	/// The console writer to use. If null, uses <see cref="AnsiConsole.Console"/>.
	/// </summary>
	protected IAnsiConsole Writer { get; } = writer ?? AnsiConsole.Console;

	/// <summary>
	/// Gets a value indicating whether a new line should be added after the current operation.
	/// </summary>
	protected bool NewLine { get; } = newLine;

	private static readonly IRenderable DefaultSeparator
		= new Text(Environment.NewLine);

	/// <summary>
	/// A basic horizontal rule.
	/// </summary>
	protected virtual IRenderable EntrySeparator => DefaultSeparator;

	/// <summary>
	/// Creates a new console formatter with the specified name and timestamp.
	/// </summary>
	public ConsoleDelegateFormatter GetConsoleFormatter(
		string name, DateTimeOffset? timestamp = null)
		=> new(name, Write, timestamp);

	/// <inheritdoc />
	public abstract void Write(PreparedLogEntry entry);

	/// <remarks>Uses a lock on the writer to ensure only one log entry at a time.</remarks>
	/// <inheritdoc cref="Write(PreparedLogEntry)" />
	public void WriteSynchronized(PreparedLogEntry entry)
	{
		// By using the injected writer, other locks can be applied to the same writer.
		lock (Writer)
		{
			Write(entry);
		}
	}

	/// <summary>
	/// Writes any renderable object to the console.
	/// </summary>
	protected void Write(IRenderable renderable)
		=> Writer.Write(renderable);

	/// <summary>
	/// Writes a string to the console with the optional style.
	/// </summary>
	protected virtual void Write(string? text, Style? style = null, bool trim = false)
	{
		if (trim ? string.IsNullOrWhiteSpace(text) : string.IsNullOrEmpty(text))
			return;

		if (trim) text = text.Trim();
		if (style is null) Writer.Write(text);
		else Writer.Write(new Text(text, style));
	}

	/// <summary>
	/// Writes a line to the console with the optional style.
	/// </summary>
	protected virtual void WriteLine(string? text = null, Style? style = null, bool trim = false)
	{
		if (trim ? string.IsNullOrWhiteSpace(text) : string.IsNullOrEmpty(text))
		{
			Writer.WriteLine();
			return;
		}

		if (trim) text = text.Trim();
		if (style is null) Writer.WriteLine(text);
		else Writer.WriteLine(text, style);
	}

	/// <summary>
	/// Writes a timestamp to the console.
	/// </summary>
	protected virtual void WriteTimestamp(DateTimeOffset timestamp, string format = "yyyy-MM-dd HH:mm:ss.fff")
	{
		var text = timestamp.ToString(format, System.Globalization.CultureInfo.InvariantCulture);

		Write(text, Theme.Timestamp);
	}

	/// <summary>
	/// Writes the elapsed time to the console.
	/// </summary>
	protected virtual void WriteElapsed(TimeSpan elapsed, string format = "000.000s")
	{
		var elapsedSeconds = elapsed.TotalSeconds;
		var text = elapsedSeconds.ToString(format, System.Globalization.CultureInfo.InvariantCulture);

		Write(text, Theme.Timestamp);
	}

	/// <summary>
	/// Writes the category to the console.
	/// </summary>
	protected virtual void WriteLevel(LogLevel level, Placement whiteSpace = Placement.None)
	{
		if (whiteSpace.HasFlag(Placement.Before)) Write(" ");
		Writer.Write(Theme.GetTextForLevel(level, Labels));
		if (whiteSpace.HasFlag(Placement.After)) Write(" ");
	}

	/// <summary>
	/// Writes the category to the console.
	/// </summary>
	protected virtual bool WriteCategory(string? category, Placement whiteSpace = Placement.None)
	{
		if (string.IsNullOrWhiteSpace(category))
			return false;

		if (whiteSpace.HasFlag(Placement.Before)) Write(" ");
		Write(category, Theme.Category, true);
		if (whiteSpace.HasFlag(Placement.After)) Write(" ");
		return true;
	}

	/// <summary>
	/// Writes the scope to the console.
	/// </summary>
	protected virtual bool WriteScope(string? scope, bool trim = false)
	{
		if (string.IsNullOrWhiteSpace(scope))
			return false;

		Write(scope, Theme.Scopes, trim);
		return true;
	}

	/// <summary>
	/// Writes the message to the console.
	/// </summary>
	protected virtual bool WriteMessage(string? message, bool trim = false, Placement whiteSpace = Placement.None)
	{
		if (string.IsNullOrWhiteSpace(message))
			return false;

		if (whiteSpace.HasFlag(Placement.Before)) Write(" ");
		Write(message, Theme.Message, trim);
		if (whiteSpace.HasFlag(Placement.After)) Write(" ");
		return true;
	}

	/// <summary>
	/// Writes the exception details to the console.
	/// </summary>
	protected virtual bool WriteException(Exception? exception, string? category)
	{
		if (exception is null) return false;

		try
		{
			Writer.Write(new ExceptionDisplay(exception, category));
			Writer.WriteLine();
		}
		catch
		{
			// Fall-back if WriteException fails.  Not likely, but not a bad idea.
			Writer.WriteLine($"Exception: {exception.Message}");
			var st = exception.StackTrace;
			if (!string.IsNullOrWhiteSpace(st))
				Writer.WriteLine(st);
		}

		return true;
	}

	/// <summary>
	/// Writes the exception details to the console with optional horizontal rules before and after.
	/// </summary>
	protected bool WriteException(Exception? exception, string? category, Placement hrs)
	{
		if (exception is null) return false;

		if (hrs.HasFlag(Placement.Before))
			Write(EntrySeparator);

		try
		{
			return WriteException(exception, category);
		}
		finally
		{
			if (hrs.HasFlag(Placement.After))
				Write(EntrySeparator);
		}
	}
}
