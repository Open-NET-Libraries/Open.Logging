using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Open.Logging.Extensions.SpectreConsole.Formatters;

/// <summary>
/// A formatter that outputs log entries in a tree structure using Spectre.Console.
/// </summary>
/// <param name="theme">The theme to use for console output styling. If null, uses <see cref="SpectreConsoleLogTheme.Default"/>.</param>
/// <param name="labels">The labels to use for different log levels. If null, uses <see cref="Defaults.LevelLabels"/>.</param>
/// <param name="writer">The console writer to use. If null, uses <see cref="AnsiConsole.Console"/>.</param>
public sealed class TreeSpectreConsoleFormatter(
	SpectreConsoleLogTheme? theme = null,
	LogLevelLabels? labels = null,
	IAnsiConsole? writer = null)
	: SpectreConsoleFormatterBase(theme, labels, writer)
	, ISpectreConsoleFormatter<TreeSpectreConsoleFormatter>
{
	/// <inheritdoc />
	public static TreeSpectreConsoleFormatter Create(
		SpectreConsoleLogTheme? theme = null,
		LogLevelLabels? labels = null,
		IAnsiConsole? writer = null)
		=> new(theme, labels, writer);

	private readonly Dictionary<string, TreeNode> _categoryNodes = [];
	private readonly Tree _logTree = new(new Text("Log Entries", Style.Parse("bold underline")));
	private readonly Lock _lock = new();
	private bool _isPending;
	private DateTimeOffset _lastRenderTime = DateTimeOffset.MinValue;

	/// <inheritdoc />
	public override void Write(PreparedLogEntry entry)
	{
		lock (_lock)
		{
			// Get or create the category node
			if (!_categoryNodes.TryGetValue(entry.Category ?? "Global", out var categoryNode))
			{
				categoryNode = _logTree.AddNode(new Text(entry.Category ?? "Global", Theme.Category));
				_categoryNodes[entry.Category ?? "Global"] = categoryNode;
			}

			// Create the message node
			var messageText = new Text(entry.Message ?? string.Empty, Theme.Message);
			var levelText = Theme.GetTextForLevel(entry.Level, Labels);
			var timestamp = new Text($"[{DateTime.Now:HH:mm:ss.fff}]", Theme.Timestamp);

			var messageNode = categoryNode.AddNode(new Rows(
				new Markup($"{timestamp} {levelText}"),
				new Padder(messageText, new Padding(2, 0, 0, 0))
			));

			// Add scopes if present
			if (entry.Scopes.Count > 0)
			{
				var scopeTree = new Tree(new Text("Scopes", Style.Parse("dim")))
				{
					Style = Theme.Scopes
				};

				TreeNode? currentNode = scopeTree;
				foreach (var scope in entry.Scopes)
				{
					currentNode = currentNode.AddNode(scope?.ToString() ?? "null");
				}

				messageNode.AddNode(scopeTree);
			}

			// Add exception if present
			if (entry.Exception != null)
			{
				var exceptionPanel = new Panel(new ExceptionDisplay(entry.Exception))
				{
					Border = BoxBorder.Rounded,
					BorderStyle = Theme.GetStyleForLevel(LogLevel.Error),
					Expand = false
				};

				messageNode.AddNode(exceptionPanel);
			}

			// Schedule render if not already pending
			if (!_isPending)
			{
				_isPending = true;

				// Render immediately if it's been more than 1 second since last render
				// or if there's an exception
				bool renderNow = (DateTimeOffset.Now - _lastRenderTime).TotalSeconds > 1 ||
								entry.Exception != null;

				if (renderNow)
					RenderTree();
				else
				{
					Task.Delay(300).ContinueWith(_ =>
					{
						lock (_lock)
						{
							if (_isPending)
								RenderTree();
						}
					});
				}
			}
		}
	}

	private void RenderTree()
	{
		if (_logTree.Nodes.Count > 0)
		{
			_logTree.Guide = TreeGuide.Line;
			Writer.Write(_logTree);
			Writer.WriteLine();

			// Reset the tree and nodes
			_logTree.Nodes.Clear();
			_categoryNodes.Clear();
		}

		_isPending = false;
		_lastRenderTime = DateTimeOffset.Now;
	}
}
