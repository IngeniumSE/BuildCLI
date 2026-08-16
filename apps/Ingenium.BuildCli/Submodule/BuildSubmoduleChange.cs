// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

namespace Ingenium.BuildCli.Submodule;

/// <summary>
/// Describes the result of an init or update operation.
/// </summary>
public sealed class BuildSubmoduleChange
{
	/// <summary>
	/// Gets the parent repository root.
	/// </summary>
	public required string RepositoryRoot { get; init; }

	/// <summary>
	/// Gets the relative submodule path.
	/// </summary>
	public required string RelativePath { get; init; }

	/// <summary>
	/// Gets the submodule remote URL.
	/// </summary>
	public required string Url { get; init; }

	/// <summary>
	/// Gets the ref that was checked out (tag, branch, or commit).
	/// </summary>
	public required string CheckedOutRef { get; init; }

	/// <summary>
	/// Gets the commit SHA after the operation.
	/// </summary>
	public required string Commit { get; init; }

	/// <summary>
	/// Gets the commit SHA before the operation, when the submodule already existed.
	/// </summary>
	public string? PreviousCommit { get; init; }

	/// <summary>
	/// Gets a value indicating whether the submodule was newly added.
	/// </summary>
	public bool Added { get; init; }
}
