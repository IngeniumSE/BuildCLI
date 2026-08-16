// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

namespace Ingenium.BuildCli.SelfUpdate;

/// <summary>
/// The outcome of a successful <c>bld self-update</c>.
/// </summary>
public sealed class SelfUpdateResult
{
	/// <summary>
	/// Gets the installed executable path.
	/// </summary>
	public required string ExecutablePath { get; init; }

	/// <summary>
	/// Gets the version reported by the newly installed binary.
	/// </summary>
	public required string Version { get; init; }

	/// <summary>
	/// Gets the runtime identifier that was published.
	/// </summary>
	public required string RuntimeIdentifier { get; init; }

	/// <summary>
	/// Gets the git ref that was installed, when a clone was used.
	/// </summary>
	public required string Ref { get; init; }

	/// <summary>
	/// Gets the source checkout that was published.
	/// </summary>
	public required string Source { get; init; }

	/// <summary>
	/// Gets the Unix symlink path, when one was created.
	/// </summary>
	public string? BinLink { get; init; }

	/// <summary>
	/// Gets a value indicating whether the bin directory is already on <c>PATH</c>.
	/// </summary>
	public bool BinDirectoryOnPath { get; init; }
}
