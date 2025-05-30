using Microsoft.Extensions.Logging;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Open.Logging.Extensions;

/// <summary>
/// Options for configuring the file logger.
/// </summary>
public partial record TemplateFormatterOptions
{
	/// <summary>
	/// Gets a read-only dictionary that maps tokens to their corresponding integer values.
	/// </summary>
	public static readonly FrozenDictionary<string, int> TokenMap
		= GetTokenMap().ToFrozenDictionary();

	private const string DefaultTemplate
		= "{Elapsed:hh:mm:ss.fff} {Category}{Scopes}{NewLine}[{Level}]: {Message}{NewLine}{Exception}";

	private static readonly string DefaultFormatString
		= TemplateToFormatString(DefaultTemplate);

	/// <summary>
	/// The time when the application started logging.
	/// </summary>
	/// <remarks>
	/// Is used to calculate the elapsed time for log entries.
	/// </remarks>
	public DateTimeOffset StartTime { get; set; } = DateTimeOffset.Now;

	/// <summary>
	/// Gets or sets the template for formatting log entries.
	/// </summary>
	/// <remarks>
	/// The template can include the following placeholders:<br/>
	/// 
	/// <c>{Timestamp}</c> - The timestamp of the log entry<br/>
	/// <c>{Elapsed}</c> - The time elapsed since the logger was started<br/>
	/// <c>{Category}</c> - The logger category<br/>
	/// <c>{Scopes}</c> - The log scopes<br/>
	/// <c>{Level}</c> - The log level<br/>
	/// <c>{Message}</c> - The log message<br/>
	/// <c>{Exception}</c> - The exception details<br/>
	/// </remarks>
	public string Template
	{
		get => field;
		set
		{
			TemplateFormatString = TemplateToFormatString(value);
			field = value;
		}
	} = DefaultTemplate;

	private static string TemplateToFormatString(string value)
	{
		ArgumentNullException.ThrowIfNull(value);

		var formatString = TemplateTokenPattern().Replace(value, m =>
		{
			var precedingText = m.Groups["preceding"].ValueSpan;
			var token = m.Groups["token"].ValueSpan;
			var format = m.Groups["format"].ValueSpan;

			var alternativeLookup = TokenMap.GetAlternateLookup<ReadOnlySpan<char>>();
			// If it doesn't match a known token, leave it unchanged
			if (!alternativeLookup.TryGetValue(token, out var tokenValue))
				return m.Value;

			if (format.Length == 0)
				return $"{precedingText}{{{tokenValue}}}"; // No format specified, just return the token.

			var delimiter = format[0];
			Debug.Assert(delimiter is ':' or ',');
			format = format.Slice(1);
			if (format.Length == 0) return $"{precedingText}{{{tokenValue}}}"; // No format specified, just return the token.

			var sb = new StringBuilder();
			sb.Append(precedingText).Append('{').Append(tokenValue).Append(delimiter);

			if (delimiter is ',')
			{
				var i = format.IndexOf(':');
				if (i == -1)
				{
					sb.Append(format);
					goto close;
				}

				delimiter = ':';
				sb.Append(format.Slice(0, i)).Append(delimiter);
				format = format.Slice(i + 1);
			}

			// Using the FormatExcapeCharacters regex, iterate through the segments in formatSpan and replace the unescaped characters.
			var matches = FormatExcapeCharacters().EnumerateMatches(format);
			if (matches.MoveNext())
			{
				var lastIndex = 0;
				do
				{
					var match = matches.Current;
					if (match.Index > lastIndex)
					{
						sb.Append(format.Slice(lastIndex, match.Index - lastIndex));
					}

					sb.Append('\\').Append(format.Slice(match.Index, match.Length));
					lastIndex = match.Index + match.Length;
				}
				while (matches.MoveNext());

				if (lastIndex < format.Length)
				{
					sb.Append(format.Slice(lastIndex)); // Append the remaining part of the format string
				}
			}
			else
			{
				sb.Append(format);
			}

		close:
			sb.Append('}');
			return sb.ToString();
		});

		// Validate the format string
		_ = string.Format(
			CultureInfo.InvariantCulture,
			formatString,
			Environment.NewLine,
			DateTimeOffset.Now,
			TimeSpan.FromSeconds(30),
			nameof(TemplateFormatterOptions),
			"> A > B",
			"WARN",
			"The Message",
			"The exception details.");

		return formatString;
	}

	/// <summary>
	/// Gets or sets the string used to separate log entries.
	/// </summary>
	/// <remarks>
	/// Empty string will eliminate the separation between log entries.
	/// Any other string will be added as a separate line between log entries.
	/// </remarks>
	public string? EntrySeparator { get; set; } = Environment.NewLine;

	/// <summary>
	/// The format string for the template.
	/// </summary>
	public string TemplateFormatString { get; private set; } = DefaultFormatString;

	/// <summary>
	/// Gets or sets custom labels for different log levels.
	/// </summary>
	/// <remarks>
	/// These labels are used in the log output when the {Level} placeholder is used in the template.
	/// The default labels will be used if this property is not set.
	/// </remarks>
	public LogLevelLabels? LevelLabels { get; set; }

	/// <summary>
	/// Gets or sets the log level filter.
	/// </summary>
	public LogLevel MinLogLevel { get; set; } = Defaults.LogLevel;

	/// <summary>
	/// Gets or sets the string used to separate scopes.
	/// </summary>
	public string ScopesSeparator { get; set; } = " > ";

	private enum Tokens
	{
		NewLine = 0,
		Timestamp = 1,
		Elapsed = 2,
		Category = 3,
		Scopes = 4,
		Level = 5,
		Message = 6,
		Exception = 7,
	}

	private static IEnumerable<KeyValuePair<string, int>> GetTokenMap()
	{
		foreach (var token in Enum.GetValues<Tokens>())
		{
			yield return new KeyValuePair<string, int>(token.ToString(), (int)token);
		}
	}

	/// <summary>
	/// Formats the scopes for logging in a simple separator first format.
	/// </summary>
	public string FormatScopes(IReadOnlyList<object> scopes)
	{
		if (scopes is null || scopes.Count == 0) return string.Empty;
		if (scopes.Count == 1) return ScopesSeparator + scopes[0];

		var sb = new StringBuilder();
		foreach (var scope in scopes)
		{
			sb.Append(ScopesSeparator);
			sb.Append(scope);
		}

		return sb.ToString();
	}

	[GeneratedRegex(@"(?<!\{)(?<preceding>(?:\{\{)*)\{(?<token>\w+)(?<format>[,:][^}]+)?}", RegexOptions.Compiled)]
	private static partial Regex TemplateTokenPattern();

	[GeneratedRegex(@"(?<!\\)[.:]", RegexOptions.Compiled)]
	private static partial Regex FormatExcapeCharacters();
}
