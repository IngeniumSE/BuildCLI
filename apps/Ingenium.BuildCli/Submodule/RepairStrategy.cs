// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

namespace Ingenium.BuildCli.Submodule;

/// <summary>
/// Strategies for repairing a broken or dirty Build submodule.
/// </summary>
public enum RepairStrategy
{
	/// <summary>
	/// Stash local submodule changes, then restore the parent-recorded commit.
	/// </summary>
	Stash = 0,

	/// <summary>
	/// Discard local submodule changes and restore the parent-recorded HEAD.
	/// </summary>
	Reset = 1,

	/// <summary>
	/// Deinitialize the submodule and check it out again at a tagged version.
	/// </summary>
	Reinit = 2
}

/// <summary>
/// Parses repair strategy names, including aliases such as <c>head</c> and <c>re-init</c>.
/// </summary>
public static class RepairStrategyParser
{
	/// <summary>
	/// Attempts to parse a strategy name.
	/// </summary>
	public static bool TryParse(string? value, out RepairStrategy strategy)
	{
		strategy = default;
		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}

		switch (value.Trim().ToLowerInvariant())
		{
			case "stash":
				strategy = RepairStrategy.Stash;
				return true;
			case "reset":
			case "head":
				strategy = RepairStrategy.Reset;
				return true;
			case "reinit":
			case "re-init":
			case "reinitialize":
			case "reinitialise":
				strategy = RepairStrategy.Reinit;
				return true;
			default:
				return false;
		}
	}
}
