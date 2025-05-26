using System.Threading.Channels;

namespace Open.Logging.Extensions;

/// <summary>
/// Provides utility methods for creating and configuring channels.
/// </summary>
internal static class ChannelFactory
{
	/// <summary>
	/// Creates a channel with specified buffer constraints.
	/// </summary>
	/// <typeparam name="T">The type of items in the channel.</typeparam>
	/// <param name="bufferSize">Size of the buffer. Set to 0 for unbounded.</param>
	/// <param name="singleWriter">Whether the channel has a single writer.</param>
	/// <param name="singleReader">Whether the channel has a single reader.</param>
	/// <param name="allowSynchronousContinuations">Whether to allow synchronous continuations.</param>
	/// <param name="fullMode">Behavior when the buffer is full (only applies to bounded channels).</param>
	/// <returns>A configured channel instance.</returns>
	public static Channel<T> Create<T>(
		int bufferSize = 10000,
		bool singleWriter = false,
		bool singleReader = false,
		bool allowSynchronousContinuations = false,
		BoundedChannelFullMode fullMode = BoundedChannelFullMode.Wait)
	{
		if (bufferSize > 0)
		{
			var boundedOptions = new BoundedChannelOptions(bufferSize)
			{
				FullMode = fullMode,
				SingleWriter = singleWriter,
				SingleReader = singleReader,
				AllowSynchronousContinuations = allowSynchronousContinuations
			};
			return Channel.CreateBounded<T>(boundedOptions);
		}
		else
		{
			var unboundedOptions = new UnboundedChannelOptions
			{
				SingleWriter = singleWriter,
				SingleReader = singleReader,
				AllowSynchronousContinuations = allowSynchronousContinuations
			};
			return Channel.CreateUnbounded<T>(unboundedOptions);
		}
	}
}