using Spectre.Console;
using Spectre.Console.Rendering;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Open.Logging.Extensions.SpectreConsole;

/// <summary>
/// A custom implementation for displaying exceptions in Spectre.Console.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ExceptionDisplay"/> class.
/// </remarks>
/// <param name="exception">The exception to display.</param>
/// <param name="category">An optional category to filter the exception details.</param>
internal sealed partial class ExceptionDisplay(Exception exception, string? category = null) : IRenderable
{
	private readonly Exception _exception = exception ?? throw new ArgumentNullException(nameof(exception));

	private Markup Renderable => field ??= new Markup(TrimNewLineEnd(RenderExceptionToMarkup()).ToString());

	/// <inheritdoc/>
	public Measurement Measure(RenderOptions options, int maxWidth)
	{
		// Let the renderer handle the measurement
		return ((IRenderable)Renderable).Measure(options, maxWidth);
	}

	/// <inheritdoc/>
	public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
	{
		var text = TrimNewLineEnd(RenderExceptionToMarkup()).ToString();
		return ((IRenderable)Renderable).Render(options, maxWidth);
	}

	private static StringBuilder TrimNewLineEnd(StringBuilder builder)
	{
		for (var i = builder.Length - 1; i >= 0; i--)
		{
			switch (builder[i])
			{
				case '\r':
				case '\n':
				case '\t':
				case ' ':
					builder.Length--;
					break;

				default:
					return builder;
			}
		}

		return builder;
	}

	/// <summary>
	/// Renders the exception into markup format.
	/// </summary>
	public StringBuilder RenderExceptionToMarkup(StringBuilder? target = null)
	{
		var builder = target ?? new StringBuilder();
		// Exception type and message
		builder.Append(CultureInfo.InvariantCulture, $"[bold red]{_exception.GetType().Name}[/]: [yellow]");
		EscapeMarkup(builder, _exception.Message);
		builder.AppendLine("[/]");

		// Stack trace
		if (_exception.StackTrace != null)
		{
			var lines = _exception.StackTrace.Split([Environment.NewLine], StringSplitOptions.None);

			foreach (var line in lines)
			{
				if (string.IsNullOrWhiteSpace(line)) continue;
				var trimmedLine = line.AsSpan().TrimStart();
				if (trimmedLine.StartsWith("at ", StringComparison.Ordinal))
				{
					// Process the "at" line to split into "at" and "in" parts
					var methodPart = trimmedLine[3..];
					var match = AtRegex().Match(methodPart.ToString());

					// First line with method info
					builder.Append("  [dim]at[/] ");

					if (match.Success && match.Groups.Count >= 3)
					{
						var m = match.Groups[1].ValueSpan;
						// If m starts with the category, skip past that portion.
						if (!string.IsNullOrWhiteSpace(category)
							&& m.Length > category.Length
							&& m.StartsWith(category, StringComparison.OrdinalIgnoreCase)
							&& m[category.Length] == '.')
						{
							m = m.Slice(category.Length + 1); // Skip past the category and the dot
						}

						EscapeMarkup(builder, m);
						builder.AppendLine();

						// Second line with file/line info - convert to file:/// format if it contains spaces
						builder.Append("    [dim]in[/] ");
						FormatFilePath(builder, match.Groups[2].ValueSpan);
						builder.AppendLine();
					}
					else
					{
						// If no "in" part is found, just display the original format
						EscapeMarkup(builder, methodPart);
					}

					builder.AppendLine();
				}
				else
				{
					builder.Append("    [dim]");
					EscapeMarkup(builder, trimmedLine);
					builder.AppendLine("[/]");
				}
			}
		}

		// Inner exception
		if (_exception.InnerException != null)
		{
			builder.AppendLine();
			builder.AppendLine(CultureInfo.InvariantCulture, $"[bold]Inner Exception:[/]");
			var innerEx = new ExceptionDisplay(_exception.InnerException);
			innerEx.RenderExceptionToMarkup(builder);
		}

		return builder;
	}

	/// <summary>
	/// Formats a file path to use file:/// URI format if it contains spaces
	/// </summary>
	private static StringBuilder FormatFilePath(StringBuilder sb, ReadOnlySpan<char> path)
	{
		// Check if we need to process this path (contains spaces and isn't already a file:/// URI)
		if (path.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
		{
			return EscapeMarkup(sb, path);
		}

		var fullPath = path;
		// Extract the line number information if present (typically ":line XX")
		var lineInfo = ReadOnlySpan<char>.Empty;
		var lineMatch = LinePattern().Match(path.ToString());
		if (lineMatch.Success)
		{
			lineInfo = lineMatch.ValueSpan;
			path = path.Slice(0, path.Length - lineMatch.Length);
		}

		if (!path.Contains(' '))
		{
			return sb.Append(fullPath);
		}

		sb.Append("\"file:///");
		ReplaceFileChars(sb, path);
		sb.Append('\"');
		sb.Append(':').Append(lineInfo.Slice(6));
		return sb;
	}

	private static StringBuilder ReplaceFileChars(StringBuilder sb, ReadOnlySpan<char> filePath)
	{
		foreach (char c in filePath)
		{
			switch (c)
			{
				case '\\':
					sb.Append('/');
					break;
				case ' ':
					sb.Append("%20");
					break;
				case '[':
					sb.Append("[[");
					break;
				case ']':
					sb.Append("]]");
					break;
				default:
					sb.Append(c);
					break;

			}
		}

		return sb;
	}

	private static StringBuilder EscapeMarkup(StringBuilder sb, ReadOnlySpan<char> text)
	{
		foreach (char c in text)
		{
			switch (c)
			{
				case '[':
					sb.Append("[[");
					break;
				case ']':
					sb.Append("]]");
					break;
				default:
					sb.Append(c);
					break;
			}
		}

		return sb;
	}

	[GeneratedRegex(@"^(.*) in (.+)$", RegexOptions.Compiled)]
	private static partial Regex AtRegex();
	[GeneratedRegex(@":line\s+\d+$", RegexOptions.IgnoreCase, "en-US")]
	private static partial Regex LinePattern();
}
