// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

namespace Ingenium.BuildCli.Submodule;

/// <summary>
/// A snapshot of the Build submodule in a parent repository.
/// </summary>
public sealed class BuildSubmoduleStatus
{
	/// <summary>
	/// Gets the absolute path of the parent repository.
	/// </summary>
	public required string RepositoryRoot { get; init; }

	/// <summary>
	/// Gets a value indicating whether the submodule is recorded in <c>.gitmodules</c>.
	/// </summary>
	public bool IsRegistered { get; init; }

	/// <summary>
	/// Gets a value indicating whether the submodule working tree has been checked out.
	/// </summary>
	public bool IsInitialized { get; init; }

	/// <summary>
	/// Gets the relative submodule path, when known.
	/// </summary>
	public string? RelativePath { get; init; }

	/// <summary>
	/// Gets the configured submodule URL, when known.
	/// </summary>
	public string? Url { get; init; }

	/// <summary>
	/// Gets the currently checked-out commit SHA, when available.
	/// </summary>
	public string? Commit { get; init; }

	/// <summary>
	/// Gets tags that point at the current commit.
	/// </summary>
	public IReadOnlyList<string> CurrentTags { get; init; } = [];

	/// <summary>
	/// Gets the latest remote tag name, when one exists.
	/// </summary>
	public string? LatestTag { get; init; }

	/// <summary>
	/// Gets the commit of the latest remote tag, when one exists.
	/// </summary>
	public string? LatestCommit { get; init; }

	/// <summary>
	/// Gets a value indicating whether the current commit matches the latest tag.
	/// </summary>
	public bool IsLatest
	{
		get
		{
			if (string.IsNullOrEmpty(Commit) || string.IsNullOrEmpty(LatestCommit))
			{
				return false;
			}

			return Commit.StartsWith(LatestCommit, StringComparison.OrdinalIgnoreCase)
				|| LatestCommit.StartsWith(Commit, StringComparison.OrdinalIgnoreCase);
		}
	}
}
