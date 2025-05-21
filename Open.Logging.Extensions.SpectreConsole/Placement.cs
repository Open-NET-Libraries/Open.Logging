namespace Open.Logging.Extensions.SpectreConsole;

/// <summary>
/// An enumeration representing the placement of a log items.
/// </summary>
[Flags]
public enum Placement
{
	/// <summary>
	/// Represents the absence of any specific value or option.
	/// </summary>
	None = 0,

	/// <summary>
	/// Will be placed before.
	/// </summary>
	Before = 1,

	/// <summary>
	/// Will be placed after.
	/// </summary>
	After = 2,

	/// <summary>
	/// Will be placed both before and after.
	/// </summary>
	Both = Before | After,
}
