// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

namespace Ingenium.BuildCli.Extensions;

/// <summary>
/// Scaffolds a Cake build-extension project in the layout expected by the Build submodule.
/// </summary>
public interface IBuildExtensionService
{
	/// <summary>
	/// Creates <c>build-extensions/{Name}BuildExtensions</c> and the shared Directory.Build files.
	/// </summary>
	Task<BuildExtensionScaffold> CreateAsync(BuildExtensionRequest request, CancellationToken cancellationToken = default);
}
