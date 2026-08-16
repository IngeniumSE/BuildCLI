// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using Ingenium.BuildCli.Submodule;

namespace Ingenium.BuildCli.Host;

/// <summary>
/// Runs the Cake Frosting host inside the Build submodule.
/// </summary>
public interface IBuildHostService
{
	/// <summary>
	/// Invokes the Build host with the supplied Cake target and extra arguments.
	/// </summary>
	/// <returns>The process exit code from <c>dotnet run</c>.</returns>
	Task<int> RunAsync(BuildHostRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for invoking the Build submodule host.
/// </summary>
public sealed class BuildHostRequest
{
	/// <summary>
	/// Gets the parent repository and submodule location.
	/// </summary>
	public required BuildSubmoduleRequest Repository { get; init; }

	/// <summary>
	/// Gets the Cake target to run. Defaults to <c>Default</c>.
	/// </summary>
	public string Target { get; init; } = "Default";

	/// <summary>
	/// Gets an optional Cake/MSBuild configuration.
	/// </summary>
	public string? Configuration { get; init; }

	/// <summary>
	/// Gets additional arguments forwarded to the Build host after <c>--</c>.
	/// </summary>
	public IReadOnlyList<string> ExtraArguments { get; init; } = [];
}
