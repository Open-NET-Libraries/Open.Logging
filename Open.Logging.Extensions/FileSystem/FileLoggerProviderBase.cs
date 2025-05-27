using Microsoft.Extensions.Logging;
using Open.Disposable;
using Open.Logging.Extensions.Writers;

namespace Open.Logging.Extensions.FileSystem;

/// <summary>
/// Base class for file logger providers that writes logs to files with configurable formatting.
/// </summary>
/// <remarks>
/// This is the absolute minimum to implement a file logger provider.
/// </remarks>
[SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "AsyncDisposableBase does all the work.")]
public abstract class FileLoggerProviderBase
	: AsyncDisposableBase
	, ILoggerProvider
{
	private readonly DateTimeOffset _startTime;
	[SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Disposed in OnDisposeAsync")]
	private readonly BufferedLogEntryWriter _bufferedWriter;

	/// <summary>
	/// Initializes a new instance of the <see cref="FileLoggerProviderBase"/> class with the specified options.
	/// </summary>
	/// <param name="options">The options for configuring the file logger.</param>
	protected FileLoggerProviderBase(
		FileLoggerOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);
		options.Validate();

		// Create the template writer
		Formatter = new TemplateTextLogEntryWriter(options);

		// Ensure the log directory exists
		Directory.CreateDirectory(options.LogDirectory);

		LogDirectory = options.LogDirectory;
		FileNamePattern = options.FileNamePattern;
		UseUtcTimestamp = options.UseUtcTimestamp;
		MinLogLevel = options.MinLogLevel;
		_startTime = options.StartTime;

		// Create a buffered writer that uses the template writer to format logs
		_bufferedWriter = new BufferedLogEntryWriter(
			WriteLogEntryAsync,
			options.BufferSize,
			options.AllowSynchronousContinuations,
			OnBufferEmptied);
	}

	/// <summary>
	/// The formatter used to format log entries before writing them to the file.
	/// </summary>
	protected TemplateTextLogEntryWriter Formatter { get; }

	/// <summary>
	/// Gets the directory where log files are stored.
	/// </summary>
	protected string LogDirectory { get; }

	/// <summary>
	/// Gets the file name pattern for log files.
	/// </summary>
	protected string FileNamePattern { get; }

	/// <summary>
	/// Gets a value indicating whether to use UTC time for file naming patterns.
	/// </summary>
	protected bool UseUtcTimestamp { get; }

	/// <summary>
	/// Gets the minimum log level for this provider.
	/// </summary>
	protected LogLevel MinLogLevel { get; }

	/// <inheritdoc />
	protected override async ValueTask OnDisposeAsync()
	{
		// Flush any remaining logs in the buffer
		await _bufferedWriter.DisposeAsync().ConfigureAwait(false);
	}

	/// <inheritdoc />
	[SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "There is no need for a finalizer.")]
	[SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "There is no need for a finalizer.")]
	public void Dispose()
	{
		// Dispose asynchronously to ensure proper cleanup
		DisposeAsync().AsTask().Wait();
	}

	/// <summary>
	/// Creates a logger with the specified category name.
	/// </summary>
	/// <param name="categoryName">The category name for the logger.</param>
	/// <returns>An instance of <see cref="ILogger"/>.</returns>
	public ILogger CreateLogger(string categoryName)
	{
		AssertIsAlive();
		return new FileLogger(categoryName, MinLogLevel, WriteLog, _startTime);
	}

	/// <summary>
	/// This is called by loggers to write a prepared log entry to the underlying target.
	/// </summary>
	protected virtual void WriteLog(PreparedLogEntry entry)
	{
		// The buffered writer will throw if the channel is closed (buffered writer is disposed).
		_bufferedWriter.Write(in entry);
	}

	/// <summary>
	/// This method is called by the <see cref="BufferedLogEntryWriter"/> to signal writing to the underlying target.
	/// </summary>
	/// <param name="entry">The prepared log entry to be written.</param>
	protected abstract ValueTask WriteLogEntryAsync(PreparedLogEntry entry);

	/// <summary>
	/// Signals that the buffer has been emptied and any necessary actions can be taken.
	/// </summary>
	/// <remarks>
	/// Doesn't not guarantee that new messages will not be added to the buffer after this method is called.
	/// </remarks>
	protected virtual ValueTask OnBufferEmptied() => ValueTask.CompletedTask;

	/// <summary>
	/// Implementation of <see cref="ILogger"/> that writes logs to a file.
	/// </summary>
	private sealed class FileLogger(
		string category,
		LogLevel minLogLevel,
		Action<PreparedLogEntry> handler,
		DateTimeOffset startTime)
		: PreparedLoggerBase(category, minLogLevel, new LoggerExternalScopeProvider(), startTime)
	{
		/// <inheritdoc/>
		protected override void WriteLog(PreparedLogEntry entry)
			=> handler(entry);
	}
}
