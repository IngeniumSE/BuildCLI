// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

namespace Ingenium.BuildCli.Submodule;

/// <summary>
/// Shared options for Build submodule operations.
/// </summary>
public sealed class BuildSubmoduleRequest
{
	/// <summary>
	/// Gets the path of the parent repository, or a directory inside it.
	/// Defaults to the current working directory.
	/// </summary>
	public string RepositoryPath { get; init; } = Environment.CurrentDirectory;

	/// <summary>
	/// Gets an optional relative submodule path. When omitted, an existing Build
	/// submodule is detected or <see cref="BuildRepositoryUrls.DefaultPath"/> is used.
	/// </summary>
	public string? SubmodulePath { get; init; }

	/// <summary>
	/// Gets an optional clone URL override.
	/// </summary>
	public string? Url { get; init; }

	/// <summary>
	/// Gets a value indicating whether the HTTPS clone URL should be used.
	/// </summary>
	public bool UseHttps { get; init; }

	/// <summary>
	/// Gets a tag, branch, or commit to check out. When omitted, the latest tag is used.
	/// </summary>
	public string? Tag { get; init; }

	/// <summary>
	/// Gets a value indicating whether an existing submodule may be replaced or updated in place.
	/// </summary>
	public bool Force { get; init; }
}
