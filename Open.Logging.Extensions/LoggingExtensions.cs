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
	/// Formats an exception for logging, ensuring file paths are quoted and stack trace is simplified.
	/// </summary>
	public static string ToLogString(this Exception exception, string? category = null)
	{
		if (exception == null) return string.Empty;

		var exceptionString = exception.ToString();
		var result = new StringBuilder();
		
		ReadOnlySpan<char> exceptionSpan = exceptionString;
		ReadOnlySpan<char> newLine = Environment.NewLine;
		
		// Process each line without creating string arrays
		while (!exceptionSpan.IsEmpty)
		{
			// Find the next newline
			int lineEnd = exceptionSpan.IndexOf(newLine);
			
			// Get the current line
			ReadOnlySpan<char> line = lineEnd >= 0 
				? exceptionSpan.Slice(0, lineEnd) 
				: exceptionSpan;
				
			// Process the line
			ProcessLine(line, result, category);
			
			// Move to next line if there is one
			if (lineEnd >= 0)
			{
				// Append a newline if we're not at the end
				result.AppendLine();
				exceptionSpan = exceptionSpan.Slice(lineEnd + newLine.Length);
			}
			else
			{
				// We're done
				break;
			}
		}
		
		return result.ToString();
	}
	
	private static void ProcessLine(ReadOnlySpan<char> line, StringBuilder result, string? category)
	{
		ReadOnlySpan<char> inMarker = " in ";
		ReadOnlySpan<char> atPrefix = "at ";
		
		// Check if this line contains " in " (indicating a file path in stack trace)
		if (line.Contains(inMarker, StringComparison.Ordinal) && 
			line.TrimStart().StartsWith(atPrefix, StringComparison.Ordinal))
		{
			// Find the " in " part and split there
			int inIndex = line.LastIndexOf(inMarker, StringComparison.Ordinal);
			if (inIndex > 0)
			{
				ReadOnlySpan<char> beforeIn = line.Slice(0, inIndex);
				ReadOnlySpan<char> afterIn = line.Slice(inIndex);
				
				// Handle category simplification if needed
				if (!string.IsNullOrEmpty(category))
				{
					string beforeInStr = beforeIn.ToString(); // Convert only when needed
					if (beforeInStr.Contains(category, StringComparison.Ordinal))
					{
						// Extract just the class name from the category (last segment)
						var lastDotIndex = category.LastIndexOf('.');
						var className = lastDotIndex >= 0 ? category.Substring(lastDotIndex + 1) : category;
						
						// Replace the full namespace with just the class name
						beforeInStr = beforeInStr.Replace(category, className, StringComparison.Ordinal);
						result.AppendLine(beforeInStr);
					}
					else
					{
						result.AppendLine(beforeInStr);
					}
				}
				else
				{
					result.Append(beforeIn).AppendLine();
				}
				
				// Add the "in ..." part with additional indentation and ensure quotes around file path
				result.Append("     ");
				EnsureFilePathQuoted(afterIn, result);
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
	}

	/// <summary>
	/// Ensures that a file path in a stack trace "in" portion is properly quoted.
	/// Appends the result directly to the provided StringBuilder.
	/// </summary>
	private static void EnsureFilePathQuoted(ReadOnlySpan<char> inPortion, StringBuilder result)
	{
		if (inPortion.IsEmpty)
			return;

		// Look for pattern: " in [optional quote]path[optional quote]:line number"
		ReadOnlySpan<char> inPrefix = " in ";

		if (!inPortion.StartsWith(inPrefix, StringComparison.Ordinal))
		{
			result.Append(inPortion);
			return;
		}

		// Append the " in " prefix
		result.Append(inPrefix);
		
		ReadOnlySpan<char> pathPart = inPortion.Slice(inPrefix.Length);

		// If already properly quoted (starts and ends with quotes around path+line), return as-is
		if (pathPart.Length >= 2 && pathPart[0] == '"' && pathPart[^1] == '"')
		{
			result.Append(pathPart);
			return;
		}

		// Find the last colon that's followed by "line " (case insensitive)
		// Need to work around case insensitivity by using string methods
		string pathPartStr = pathPart.ToString();
		int lineIndex = pathPartStr.LastIndexOf(":line ", StringComparison.OrdinalIgnoreCase);
		
		if (lineIndex < 0)
		{
			// No ":line" found, quote the entire path part
			result.Append('"').Append(pathPart).Append('"');
			return;
		}

		// Split into path and line number parts
		ReadOnlySpan<char> filePath = pathPart.Slice(0, lineIndex);
		ReadOnlySpan<char> lineNumberPart = pathPart.Slice(lineIndex);

		// Remove any existing quotes from the file path
		if (filePath.Length >= 2 && filePath[0] == '"' && filePath[^1] == '"')
		{
			filePath = filePath.Slice(1, filePath.Length - 2);
		}

		// Return with properly quoted path + line number
		result.Append('"').Append(filePath).Append(lineNumberPart).Append('"');
	}
}
