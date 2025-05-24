using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Open.Logging.Extensions;

/// <summary>
/// A console formatter that uses a custom log handler to format and write log entries to a <see cref="TextWriter"/>.
/// </summary>
/// <remarks>This formatter allows for flexible log formatting by delegating the formatting logic to a
/// user-provided handler. The handler is invoked for each log entry, enabling custom templates or formatting logic to
/// be applied.</remarks>
public class ConsoleTemplateFormatter(
	ConsoleTemplateFormatterOptions options)
	: ConsoleFormatter(options?.Name!)
{
	private readonly ConsoleTemplateFormatterOptions _options
		= (options ?? throw new ArgumentNullException(nameof(options))) with { }; // clone the record.

	/// <inheritdoc />
	public override void Write<TState>(
		in LogEntry<TState> logEntry,
		IExternalScopeProvider? scopeProvider,
		TextWriter textWriter)
	{
		Debug.Assert(textWriter is not null);
		string message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception) ?? "";

		if (message.AsSpan().Trim().Length == 0 && logEntry.Exception is null)
			return;

		var output = string.Format(
			CultureInfo.InvariantCulture,
			_options.TemplateFormatString,
			Environment.NewLine,
			DateTimeOffset.Now - options.Timestamp,
			logEntry.Category,
			FormatScopes(scopeProvider.CaptureScope()),
			logEntry.LogLevel,
			message,
			logEntry.Exception)
			.Trim(' ', '\r', 'n');

		if (string.IsNullOrWhiteSpace(output)) return; // Nothing to log.
		textWriter.WriteLine(output);
	}

	private string FormatScopes(IReadOnlyList<object> scopes)
	{
		if (scopes.Count == 0) return string.Empty;
		if (scopes.Count == 1) return _options.ScopesSeparator + scopes[0];

		var sb = new StringBuilder();
		foreach (var scope in scopes)
		{
			sb.Append(_options.ScopesSeparator);
			sb.Append(scope);
		}

		return sb.ToString();
	}
}
