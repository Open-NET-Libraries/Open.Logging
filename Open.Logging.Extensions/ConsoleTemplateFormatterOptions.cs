using Microsoft.Extensions.Logging;
using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace Open.Logging.Extensions;

/// <summary>
/// Options for configuring the file logger.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Style", "IDE0032:Use auto property",
	Justification = "Specialized setters.")]
public record ConsoleTemplateFormatterOptions
{
	/// <summary>
	/// The logger name.
	/// </summary>
	public string Name => $"[{nameof(ConsoleTemplateFormatter)}]{_template}";

	/// <summary>
	/// The beginning timestamp for the logs.
	/// </summary>
	public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

	private string _template = "{Elapsed:HH:mm:ss.fff} {Category}{Scopes}{NewLine}[{Level}]: {Message}{NewLine}{Exception}{NewLine}";
	/// <summary>
	/// Gets or sets the template for formatting log entries.
	/// </summary>
	/// <remarks>
	/// The template can include the following placeholders:
	/// {Elapsed} - The time elapsed since the logger was started
	/// {Category} - The logger category
	/// {Scopes} - The log scopes
	/// {Level} - The log level
	/// {Message} - The log message
	/// {Exception} - The exception details
	/// </remarks>
	public string Template
	{
		get => _template;
		set
		{
			ArgumentNullException.ThrowIfNull(value);
			_template = value;
			TemplateFormatString = TemplateTokenPattern.Replace(value, m =>
			{
				var token = m.Groups[1].Value;
				var format = m.Groups[2].Value;
				return TokenMap.TryGetValue(token, out var tokenValue)
					? $"{{{tokenValue}{format ?? string.Empty}}}"
					: m.Value;
			});
		}
	}

	private readonly static Regex TemplateTokenPattern
		= new(@"\{(\w+)(:[^}]+)?}", RegexOptions.Compiled | RegexOptions.NonBacktracking);	/// <summary>
	/// The format string for the template.
	/// </summary>
	public string TemplateFormatString { get; private set; }
		= "{1:HH:mm:ss.fff} {2}{3}{0}[{4}]: {5}{6}";

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
		Elapsed = 1,
		Category = 2,
		Scopes = 3,
		Level = 4,
		Message = 5,
		Exception = 6,
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
}
