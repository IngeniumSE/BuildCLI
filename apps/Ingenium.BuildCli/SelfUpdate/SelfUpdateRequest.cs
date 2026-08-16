// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

namespace Ingenium.BuildCli.SelfUpdate;

/// <summary>
/// Options for republishing and reinstalling the <c>bld</c> CLI.
/// </summary>
public sealed class SelfUpdateRequest
{
	/// <summary>
	/// Git branch or tag of BuildCLI to install. Defaults to <c>main</c>.
	/// </summary>
	public string? Ref { get; init; }

	/// <summary>
	/// Existing BuildCLI checkout to publish. When omitted, the repository is cloned.
	/// </summary>
	public string? Source { get; init; }

	/// <summary>
	/// Clone URL used when <see cref="Source"/> is omitted.
	/// </summary>
	public string? Url { get; init; }

	/// <summary>
	/// When <c>true</c>, publish a framework-dependent binary instead of a self-contained single file.
	/// </summary>
	public bool FrameworkDependent { get; init; }

	/// <summary>
	/// When <c>true</c>, stream publish output to the console.
	/// </summary>
	public bool Verbose { get; init; }

	/// <summary>
	/// Override for the published-binary directory.
	/// </summary>
	public string? InstallDirectory { get; init; }

	/// <summary>
	/// Override for the Unix symlink directory.
	/// </summary>
	public string? BinDirectory { get; init; }
}
