// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

namespace Ingenium.BuildCli.Submodule;

/// <summary>
/// The outcome of a submodule repair.
/// </summary>
public sealed class RepairResult
{
	/// <summary>
	/// Gets the strategy that was applied.
	/// </summary>
	public required RepairStrategy Strategy { get; init; }

	/// <summary>
	/// Gets the resulting submodule state.
	/// </summary>
	public required BuildSubmoduleChange Change { get; init; }

	/// <summary>
	/// Gets the parent-recorded submodule commit, when one was available.
	/// </summary>
	public string? RecordedCommit { get; init; }

	/// <summary>
	/// Gets the created stash reference, when the stash strategy saved changes.
	/// </summary>
	public string? StashRef { get; init; }

	/// <summary>
	/// Gets the human-readable actions that were performed.
	/// </summary>
	public IReadOnlyList<string> Actions { get; init; } = [];
}
