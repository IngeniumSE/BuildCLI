// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

namespace Ingenium.BuildCli.Submodule;

/// <summary>
/// Adds and updates the Ingenium Build git submodule in a parent repository.
/// </summary>
public interface IBuildSubmoduleService
{
	/// <summary>
	/// Adds the Build submodule, or initializes it when it is already registered.
	/// </summary>
	Task<BuildSubmoduleChange> InitAsync(BuildSubmoduleRequest request, CancellationToken cancellationToken = default);

	/// <summary>
	/// Updates an existing Build submodule to the latest tag or a specific ref.
	/// </summary>
	Task<BuildSubmoduleChange> UpdateAsync(BuildSubmoduleRequest request, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns the current Build submodule state.
	/// </summary>
	Task<BuildSubmoduleStatus> GetStatusAsync(BuildSubmoduleRequest request, CancellationToken cancellationToken = default);

	/// <summary>
	/// Lists tags advertised by the Build remote.
	/// </summary>
	Task<IReadOnlyList<RemoteTag>> ListTagsAsync(BuildSubmoduleRequest request, CancellationToken cancellationToken = default);
}
