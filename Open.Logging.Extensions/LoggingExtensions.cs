using Microsoft.Extensions.Logging;
using System.Text;

namespace Open.Logging.Extensions;

/// <summary>
/// Extension helpers for logging.
/// </summary>
public static class LoggingExtensions
{
	/// <summary>
	/// Captures the current scope of the logger and returns it as a list of objects.
	/// </summary>
	/// <param name="provider">The logger provider to capture the scope from.</param>
	/// <returns>A list of objects representing the current scope. Empty if none.</returns>
	public static IReadOnlyList<object> CaptureScope(this IExternalScopeProvider? provider)
	{
		if (provider is null) return [];

		var scopes = new List<object>();
		provider.ForEachScope(static (scope, list) =>
		{
			if (scope is null) return;
			list.Add(scope);
		}, scopes);

		return scopes.Count > 0 ? scopes : [];
	}

	/// <summary>
	/// Wraps a logger in a thread-safe buffered logger that processes log messages in the background
	/// </summary>
	/// <inheritdoc cref="BufferedLogger(ILogger, int, bool)"/>
	public static BufferedLogger AsBuffered(
		this ILogger logger, int maxQueueSize = 10000,
		bool allowSynchronousContinuations = false)
		=> new(logger, maxQueueSize, allowSynchronousContinuations);

	/// <summary>
	/// Formats an exception for logging, ensuring file paths are quoted and stack trace is simplified.
	/// </summary>
	public static string ToLogString(this Exception exception, string? category = null)
	{
		if (exception == null) return string.Empty;

		var exceptionString = exception.ToString();
		var lines = exceptionString.Split([Environment.NewLine], StringSplitOptions.None);
		var result = new StringBuilder();

		for (int i = 0; i < lines.Length; i++)
		{
			var line = lines[i];

			// Check if this line contains " in " (indicating a file path in stack trace)
			if (line.Contains(" in ", StringComparison.Ordinal) && line.TrimStart().StartsWith("at ", StringComparison.Ordinal))
			{
				// Find the " in " part and split there
				var inIndex = line.LastIndexOf(" in ", StringComparison.Ordinal);
				if (inIndex > 0)
				{
					var beforeIn = line.Substring(0, inIndex);
					var afterIn = line.Substring(inIndex);

					// Optimize: if category matches the beginning of the stack trace, simplify it
					if (!string.IsNullOrEmpty(category) && beforeIn.Contains(category, StringComparison.Ordinal))
					{
						// Extract just the class name from the category (last segment)
						var lastDotIndex = category.LastIndexOf('.');
						var className = lastDotIndex >= 0 ? category.Substring(lastDotIndex + 1) : category;

						// Replace the full namespace with just the class name
						beforeIn = beforeIn.Replace(category, className, StringComparison.Ordinal);
					}
					// Add the "at ..." part first
					result.AppendLine(beforeIn);
					// Add the "in ..." part with additional indentation and ensure quotes around file path
					var formattedAfterIn = EnsureFilePathQuoted(afterIn);
					result.Append("     ").Append(formattedAfterIn);
				}
				else
				{
					result.Append(line);
				}
			}
			else
			{
				result.Append(line);
			}

			// Add newline for all but the last line
			if (i < lines.Length - 1)
			{
				result.AppendLine();
			}
		}

		return result.ToString();
	}

	/// <summary>
	/// Ensures that a file path in a stack trace "in" portion is properly quoted.
	/// </summary>
	/// <param name="inPortion">The "in" portion of a stack trace line (e.g., " in D:\path\file.cs:line 123")</param>
	/// <returns>The properly quoted "in" portion</returns>
	private static string EnsureFilePathQuoted(string inPortion)
	{
		if (string.IsNullOrEmpty(inPortion))
			return inPortion;

		// Look for pattern: " in [optional quote]path[optional quote]:line number"
		const string inPrefix = " in ";

		if (!inPortion.StartsWith(inPrefix, StringComparison.Ordinal))
			return inPortion;

		var pathPart = inPortion.Substring(inPrefix.Length);

		// If already properly quoted (starts and ends with quotes around path+line), return as-is
		if (pathPart.StartsWith('"') && pathPart.EndsWith('"'))
			return inPortion;

		// Find the last colon that's followed by "line " (case insensitive)
		var lineIndex = pathPart.LastIndexOf(":line ", StringComparison.OrdinalIgnoreCase);
		if (lineIndex < 0)
		{
			// No ":line" found, quote the entire path part
			return $"{inPrefix}\"{pathPart}\"";
		}

		// Split into path and line number parts
		var filePath = pathPart.Substring(0, lineIndex);
		var lineNumberPart = pathPart.Substring(lineIndex);

		// Remove any existing quotes from the file path
		filePath = filePath.Trim('"');

		// Return with properly quoted path + line number
		return $"{inPrefix}\"{filePath}{lineNumberPart}\"";
	}

}
