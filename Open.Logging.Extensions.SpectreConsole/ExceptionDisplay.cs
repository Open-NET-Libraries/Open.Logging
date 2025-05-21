using Spectre.Console;
using Spectre.Console.Rendering;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Open.Logging.Extensions.SpectreConsole;

/// <summary>
/// A custom implementation for displaying exceptions in Spectre.Console.
/// </summary>
internal sealed class ExceptionDisplay : IRenderable
{
    private readonly Exception _exception;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionDisplay"/> class.
    /// </summary>
    /// <param name="exception">The exception to display.</param>
    public ExceptionDisplay(Exception exception)
    {
        _exception = exception ?? throw new ArgumentNullException(nameof(exception));
    }

    /// <inheritdoc/>
    public Measurement Measure(RenderOptions options, int maxWidth)
    {
        // Let the renderer handle the measurement
        var text = RenderExceptionToText();
        return ((IRenderable)new Markup(text)).Measure(options, maxWidth);
    }

    /// <inheritdoc/>
    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        var text = RenderExceptionToText();
        return ((IRenderable)new Markup(text)).Render(options, maxWidth);
    }

    private string RenderExceptionToText()
    {
        var builder = new StringBuilder();
          // Exception type and message
        builder.AppendLine(CultureInfo.InvariantCulture, $"[bold red]{_exception.GetType().Name}[/]: [yellow]{EscapeMarkup(_exception.Message)}[/]");
        
        // Stack trace
        if (_exception.StackTrace != null)
        {
            builder.AppendLine();
            var lines = _exception.StackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                  var trimmedLine = line.TrimStart();                if (trimmedLine.StartsWith("at ", StringComparison.Ordinal))
                {
                    builder.AppendLine(CultureInfo.InvariantCulture, $"  [dim]at[/] {EscapeMarkup(trimmedLine[3..])}");
                }
                else
                {
                    builder.AppendLine(CultureInfo.InvariantCulture, $"  [dim]{EscapeMarkup(trimmedLine)}[/]");
                }
            }
        }
          // Inner exception
        if (_exception.InnerException != null)
        {            builder.AppendLine();
            builder.AppendLine(CultureInfo.InvariantCulture, $"[bold]Inner Exception:[/]");
            builder.AppendLine(new ExceptionDisplay(_exception.InnerException).RenderExceptionToText());
        }
        
        return builder.ToString();
    }    private static string EscapeMarkup(string text)
    {
        return text
            .Replace("[", "[[", StringComparison.Ordinal)
            .Replace("]", "]]", StringComparison.Ordinal);
    }
}
