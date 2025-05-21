using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Open.Logging.Extensions.Tests;

public class ConsoleDelegateFormatterTests
{
	[Fact]
	public void Write_FormatsAndInvokesHandler()
	{
		// Arrange
		using var textWriter = new StringWriter();
		PreparedLogEntry? capturedEntry = null;
		TextWriter? capturedWriter = null;

		var formatter = new ConsoleDelegateFormatter(
			"test-formatter",
			(writer, entry) =>
			{
				capturedWriter = writer;
				capturedEntry = entry;
				writer.Write("Formatted: " + entry.Message);
			});

		var logEntry = new LogEntry<string>(
			LogLevel.Information,
			"test-category",
			new EventId(1, "TestEvent"),
			"Test message",
			null,
			(state, ex) => "Test message");

		// Act
		formatter.Write(in logEntry, null, textWriter);

		// Assert
		Assert.NotNull(capturedEntry);
		Assert.Same(textWriter, capturedWriter);
		Assert.Equal("Test message", capturedEntry!.Value.Message);
		Assert.Equal("Formatted: Test message", textWriter.ToString());
	}

	[Fact]
	public void Write_WithScopeProvider_PassesScopesToHandler()
	{
		// Arrange
		using var textWriter = new StringWriter();
		PreparedLogEntry? capturedEntry = null;
		var scopeProvider = new LoggerExternalScopeProvider();

		var formatter = new ConsoleDelegateFormatter(
			"test-formatter",
			(writer, entry) =>
			{
				capturedEntry = entry;
				writer.Write($"Scopes: {entry.Scopes.Count}");
			});

		var logEntry = new LogEntry<string>(
			LogLevel.Information,
			"test-category",
			new EventId(1, "TestEvent"),
			"Test message",
			null,
			(state, ex) => "Test message");

		// Add scopes
		var stringScope = "Scope1";
		object objScope = new { Id = 2, Name = "Object" };
		scopeProvider.Push(stringScope);
		scopeProvider.Push(objScope);

		// Act
		formatter.Write(in logEntry, scopeProvider, textWriter);

		// Assert
		Assert.NotNull(capturedEntry);
		Assert.Equal(2, capturedEntry!.Value.Scopes.Count);
		Assert.Equal("Scope1", capturedEntry.Value.Scopes[0].ToString());
		Assert.Contains("Scopes: 2", textWriter.ToString(), StringComparison.Ordinal);
	}

	[Fact]
	public void Write_WithException_PassesExceptionToHandler()
	{
		// Arrange
		using var textWriter = new StringWriter();
		PreparedLogEntry? capturedEntry = null;
		var expectedException = new InvalidOperationException("Test exception");

		var formatter = new ConsoleDelegateFormatter(
			"test-formatter",
			(writer, entry) =>
			{
				capturedEntry = entry;
				writer.Write(entry.Exception?.Message ?? "No exception");
			});

		var logEntry = new LogEntry<string>(
			LogLevel.Error,
			"test-category",
			new EventId(1, "TestEvent"),
			"Error message",
			expectedException,
			(state, ex) => "Error message");

		// Act
		formatter.Write(in logEntry, null, textWriter);

		// Assert
		Assert.NotNull(capturedEntry);
		Assert.Same(expectedException, capturedEntry!.Value.Exception);
		Assert.Equal("Test exception", textWriter.ToString());
	}

	[Fact]
	public void Write_WithEmptyMessage_DoesNotInvokeHandler()
	{
		// Arrange
		using var textWriter = new StringWriter();
		var handlerInvoked = false;

		var formatter = new ConsoleDelegateFormatter(
			"test-formatter",
			(writer, entry) =>
			{
				handlerInvoked = true;
				writer.Write("Handler invoked");
			});

		var logEntry = new LogEntry<string>(
			LogLevel.Information,
			"test-category",
			new EventId(1, "TestEvent"),
			"",
			null,
			(state, ex) => "");

		// Act
		formatter.Write(in logEntry, null, textWriter);

		// Assert
		Assert.False(handlerInvoked);
		Assert.Equal("", textWriter.ToString());
	}

	[Fact]
	public void Constructor_WithAlternateSignature_WrapsHandlerCorrectly()
	{
		// Arrange
		using var textWriter = new StringWriter();
		PreparedLogEntry? capturedEntry = null;

		var formatter = new ConsoleDelegateFormatter(
			"test-formatter",
			(entry) => capturedEntry = entry);

		var logEntry = new LogEntry<string>(
			LogLevel.Information,
			"test-category",
			new EventId(1, "TestEvent"),
			"Test message",
			null,
			(state, ex) => "Test message");

		// Act
		formatter.Write(in logEntry, null, textWriter);

		// Assert
		Assert.NotNull(capturedEntry);
		Assert.Equal("Test message", capturedEntry!.Value.Message);
	}

	[Fact]
	public void Constructor_WithCustomTimestamp_UsesProvidedTimestamp()
	{
		// Arrange
		using var textWriter = new StringWriter();
		PreparedLogEntry? capturedEntry = null;
		var timestamp = new DateTimeOffset(2023, 1, 1, 12, 0, 0, TimeSpan.Zero);

		var formatter = new ConsoleDelegateFormatter(
			"test-formatter",
			(entry) => capturedEntry = entry,
			timestamp);

		var logEntry = new LogEntry<string>(
			LogLevel.Information,
			"test-category",
			new EventId(1, "TestEvent"),
			"Test message",
			null,
			(state, ex) => "Test message");

		// Act
		formatter.Write(in logEntry, null, textWriter);

		// Assert
		Assert.NotNull(capturedEntry);
		Assert.Equal(timestamp, capturedEntry!.Value.StartTime);
	}

	[Fact]
	public void Constructor_WithNullHandler_ThrowsArgumentNullException()
	{
		// Act & Assert
		Assert.Throws<ArgumentNullException>(() =>
			new ConsoleDelegateFormatter("test-formatter", (Action<TextWriter, PreparedLogEntry>)null!));
	}
}