using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Open.Logging.Extensions.FileSystem;

/// <summary>
/// A simple file logger provider that writes logs to a single file.
/// This provider does not support file rolling or retention policies.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SingleFileLoggerProvider"/> class with the specified options.
/// </remarks>
public sealed class SingleFileLoggerProvider : FileLoggerProviderBase
{
	readonly Lazy<StreamWriter> _streamWriter;

	/// <param name="options">The options for configuring the single file logger.</param>
	public SingleFileLoggerProvider(
		FileLoggerOptions options) : base(options)
	{
		// Ensure the log directory exists
		if (!Directory.Exists(LogDirectory))
			Directory.CreateDirectory(LogDirectory);

		Debug.Assert(options is not null);
		var filePath = options.GetFormattedFilePath();
		_streamWriter = new Lazy<StreamWriter>(() => new(filePath, true, options.Encoding, options.FileBufferSize));
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SingleFileLoggerProvider"/> class with options from the DI container.
	/// </summary>
	/// <param name="options">The options for configuring the single file logger.</param>
	public SingleFileLoggerProvider(IOptionsSnapshot<FileLoggerOptions> options)
		: this(options?.Value ?? new FileLoggerOptions()) { }

	/// <inheritdoc />
	protected override async ValueTask OnDisposeAsync()
	{
		// The following will only be called once because of how AsyncDisposableBase works.
		await base.OnDisposeAsync().ConfigureAwait(false);
		if (!_streamWriter.IsValueCreated) return;
		await _streamWriter.Value.DisposeAsync().ConfigureAwait(false);
	}

	/// <inheritdoc />
	protected override ValueTask WriteLogEntryAsync(PreparedLogEntry entry)
	{
		Formatter.Write(in entry, _streamWriter.Value);
		return ValueTask.CompletedTask;
	}

	/// <inheritdoc />
	protected override async ValueTask OnBufferEmptied()
	{
		// Are we in the middle of disposing or have we already disposed?
		if (WasDisposed) return;
		// Flush the stream writer to ensure all buffered data is written to the file
		await _streamWriter.Value.FlushAsync().ConfigureAwait(false);
	}
}
