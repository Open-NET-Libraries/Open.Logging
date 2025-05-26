using Microsoft.Extensions.Logging;
using System.Collections.Frozen;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Open.Logging.Extensions;

/// <summary>
/// Options for configuring the file logger.
/// </summary>
public record TemplateFormatterOptions
{
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
			ArgumentNullException.ThrowIfNull(value);
			var formatString = TemplateTokenPattern.Replace(value, m =>
			{
				var token = m.Groups[1].Value;
				var format = m.Groups[2].Value;
				return TokenMap.TryGetValue(token, out var tokenValue)
					? $"{{{tokenValue}{format ?? string.Empty}}}"
					: m.Value;
			});

			// Validate the format string
			_ = string.Format(
				CultureInfo.InvariantCulture,
				formatString,
				Environment.NewLine,
				DateTimeOffset.Now,
				TimeSpan.FromSeconds(30),
				nameof(TemplateFormatterOptions),
				FormatScopes(["A", "B"]),
				"WARN",
				"The Message",
				"The exception details.");

			field = value;
			TemplateFormatString = formatString;
		}
	} = "{Elapsed:HH:mm:ss.fff} {Category}{Scopes}{NewLine}[{Level}]: {Message}{NewLine}{Exception}";

	/// <summary>
	/// Gets or sets the string used to separate log entries.
	/// </summary>
	/// <remarks>
	/// Empty string will eliminate the separation between log entries.
	/// Any other string will be added as a separate line between log entries.
	/// </remarks>
	public string? EntrySeparator { get; set; } = Environment.NewLine;

	private readonly static Regex TemplateTokenPattern
		= new(@"\{(\w+)(:[^}]+)?}", RegexOptions.Compiled | RegexOptions.NonBacktracking);

	/// <summary>
	/// The format string for the template.
	/// </summary>
	public string TemplateFormatString { get; private set; }
		= string.Empty;

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
	/// Gets a read-only dictionary that maps tokens to their corresponding integer values.
	/// </summary>
	internal static readonly FrozenDictionary<string, int> TokenMap = GetTokenMap().ToFrozenDictionary();

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
}
