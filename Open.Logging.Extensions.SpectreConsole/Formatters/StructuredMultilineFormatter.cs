using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.Globalization;

namespace Open.Logging.Extensions.SpectreConsole.Formatters;

/// <summary>
/// A formatter that outputs log entries in a structured multi-line format with clear visual organization.
/// </summary>
/// <param name="theme">The theme to use for console output styling. If null, uses <see cref="SpectreConsoleLogTheme.Default"/>.</param>
/// <param name="labels">The labels to use for different log levels. If null, uses <see cref="Defaults.LevelLabels"/>.</param>
/// <param name="writer">The console writer to use. If null, uses <see cref="AnsiConsole.Console"/>.</param>
public sealed class StructuredMultilineFormatter(
    SpectreConsoleLogTheme? theme = null,
    LogLevelLabels? labels = null,
    IAnsiConsole? writer = null)
    : SpectreConsoleFormatterBase(theme, labels, writer)
    , ISpectreConsoleFormatter<StructuredMultilineFormatter>
{
    /// <inheritdoc />
    public static StructuredMultilineFormatter Create(
        SpectreConsoleLogTheme? theme = null,
        LogLevelLabels? labels = null,
        IAnsiConsole? writer = null)
        => new(theme, labels, writer);

    /// <inheritdoc />
    public override void Write(PreparedLogEntry entry)
    {
        var levelStyle = Theme.GetStyleForLevel(entry.Level);
        
        // Create a layout with columns
        var grid = new Grid();
        grid.AddColumn(new GridColumn().Width(15).NoWrap()); // Labels column
        grid.AddColumn(new GridColumn().Width(65).LeftAligned()); // Content column        // Add a header row with timestamp and log level
        var timestamp = DateTimeOffset.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
        var levelText = Theme.GetTextForLevel(entry.Level, Labels);
        var header = new Rule($"[{timestamp}] {levelText}");
        header.Style = levelStyle;
        header.LeftJustified();
        
        Writer.Write(header);
        Writer.WriteLine();
        
        // Source/Category
        grid.AddRow(
            new Text("Category:", Style.Parse("dim italic")),
            new Text(!string.IsNullOrWhiteSpace(entry.Category) ? entry.Category : "-", Theme.Category)
        );
        
        // Scopes
        if (entry.Scopes.Count > 0)
        {
            var scopesText = string.Join(" → ", entry.Scopes);
            grid.AddRow(
                new Text("Scopes:", Style.Parse("dim italic")),
                new Text(scopesText, Theme.Scopes)
            );
        }
        
        // Message
        if (!string.IsNullOrWhiteSpace(entry.Message))
        {
            grid.AddRow(
                new Text("Message:", Style.Parse("dim italic")),
                new Text(entry.Message, Theme.Message)
            );
        }
        
        // Elapsed
        var elapsedSeconds = entry.Elapsed.TotalSeconds;
        grid.AddRow(
            new Text("Elapsed:", Style.Parse("dim italic")),
            new Text($"{elapsedSeconds:0.000}s", Theme.Timestamp)
        );
        
        // Render the grid
        Writer.Write(grid);
        Writer.WriteLine();
        
        // Exception
        if (entry.Exception is not null)
        {
            var exceptionPanel = new Panel(new ExceptionDisplay(entry.Exception))
            {
                Header = new PanelHeader("Exception"),
                Border = BoxBorder.Rounded,
                BorderStyle = levelStyle,
                Expand = true,
                Padding = new Padding(1, 0, 1, 0)
            };
            
            Writer.Write(exceptionPanel);
            Writer.WriteLine();
        }
        
        // Add a bottom separator
        var footer = new Rule();
        footer.Style = Style.Parse("dim");
        Writer.Write(footer);
        Writer.WriteLine();
    }
}
