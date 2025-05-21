using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Open.Logging.Extensions.SpectreConsole.Formatters;

/// <summary>
/// A formatter that outputs log entries in a table format using Spectre.Console styling.
/// </summary>
/// <param name="theme">The theme to use for console output styling. If null, uses <see cref="SpectreConsoleLogTheme.Default"/>.</param>
/// <param name="labels">The labels to use for different log levels. If null, uses <see cref="Defaults.LevelLabels"/>.</param>
/// <param name="writer">The console writer to use. If null, uses <see cref="AnsiConsole.Console"/>.</param>
public sealed class TableSpectreConsoleFormatter(
	SpectreConsoleLogTheme? theme = null,
	LogLevelLabels? labels = null,
	IAnsiConsole? writer = null)
	: SpectreConsoleFormatterBase(theme, labels, writer)
	, ISpectreConsoleFormatter<TableSpectreConsoleFormatter>
{
	/// <inheritdoc />
	public static TableSpectreConsoleFormatter Create(
		SpectreConsoleLogTheme? theme = null,
		LogLevelLabels? labels = null,
		IAnsiConsole? writer = null)
		=> new(theme, labels, writer);

	private readonly List<PreparedLogEntry> _batchedEntries = [];
	private readonly object _lock = new();
	private bool _isTablePending;

	/// <inheritdoc />
	public override void Write(PreparedLogEntry entry)
	{
		lock (_lock)
		{
			// Add entry to batch
			_batchedEntries.Add(entry);

			// If we have an exception, or reached sufficient entries, render the table
			if (entry.Exception != null || _batchedEntries.Count >= 5)
				RenderTable();
			// Otherwise, set a flag to render the table on the next write			else if (!_isTablePending)
			{
				_isTablePending = true;
				Task.Delay(200).ContinueWith(_ =>
				{
					lock (_lock)
					{
						if (_isTablePending)
							RenderTable();
					}
				}, TaskScheduler.Default);
			}
		}
	}

	private void RenderTable()
	{
		if (_batchedEntries.Count == 0)
		{
			_isTablePending = false;
			return;
		}

		var table = new Table();

		// Add columns
		table.AddColumn(new TableColumn("Time").Centered());
		table.AddColumn(new TableColumn("Level").Centered());
		table.AddColumn("Source");
		table.AddColumn("Scope");
		table.AddColumn("Message");

		// Style the table
		table.Border = TableBorder.Rounded;
		table.BorderStyle = Style.Parse("grey");
		// Add rows
		foreach (var entry in _batchedEntries)
		{
			table.AddRow(
				new Text(DateTime.Now.ToString("HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture), Theme.Timestamp),
				Theme.GetTextForLevel(entry.Level, Labels),
				new Text(entry.Category ?? string.Empty, Theme.Category),
				new Text(FormatScopes(entry.Scopes), Theme.Scopes),
				new Text(entry.Message ?? string.Empty, Theme.Message)
			);
		}

		Writer.Write(table);

		// Handle exceptions
		foreach (var entry in _batchedEntries)
		{
			if (entry.Exception != null)
			{
				var exceptionStyle = Theme.GetStyleForLevel(LogLevel.Error);
				var panel = new Panel(new ExceptionDisplay(entry.Exception))
				{
					Header = new PanelHeader($"Exception in {entry.Category}"),
					Border = BoxBorder.Rounded,
					BorderStyle = exceptionStyle
				};

				Writer.Write(panel);
				Writer.WriteLine();
			}
		}

		// Clear batch
		_batchedEntries.Clear();
		_isTablePending = false;
	}

	private static string FormatScopes(IReadOnlyList<object?> scopes)
	{
		if (scopes.Count == 0) return string.Empty;

		return string.Join(" > ", scopes);
	}
}
