// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

namespace Ingenium.BuildCli.Submodule;

/// <summary>
/// Verifies that the Build submodule is present before a build can run.
/// </summary>
public static class BuildSubmoduleGuard
{
	/// <summary>
	/// Throws when the Build submodule has not been added or checked out.
	/// </summary>
	public static void EnsureInitialized(BuildSubmoduleStatus status)
	{
		ArgumentNullException.ThrowIfNull(status);

		if (!status.IsRegistered)
		{
			throw new BuildCliException(
				$"The Build submodule has not been added to this repository. Run '{CliInfo.Name} init' first.",
				ExitCodes.SubmoduleNotFound);
		}

		if (!status.IsInitialized)
		{
			throw new BuildCliException(
				$"The Build submodule is registered but not initialized. Run '{CliInfo.Name} init' or '{CliInfo.Name} repair --strategy reinit'.",
				ExitCodes.SubmoduleNotFound);
		}
	}
}
