// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

namespace Ingenium.BuildCli.Extensions;

/// <summary>
/// Describes the files created for a Build extension project.
/// </summary>
public sealed class BuildExtensionScaffold
{
	/// <summary>
	/// Gets the parent repository root.
	/// </summary>
	public required string RepositoryRoot { get; init; }

	/// <summary>
	/// Gets the generated project name.
	/// </summary>
	public required string ProjectName { get; init; }

	/// <summary>
	/// Gets the project directory.
	/// </summary>
	public required string ProjectDirectory { get; init; }

	/// <summary>
	/// Gets the files that were written.
	/// </summary>
	public IReadOnlyList<string> WrittenFiles { get; init; } = [];
}

/// <summary>
/// Options for scaffolding a Build extension.
/// </summary>
public sealed class BuildExtensionRequest
{
	/// <summary>
	/// Gets the parent repository path.
	/// </summary>
	public string RepositoryPath { get; init; } = Environment.CurrentDirectory;

	/// <summary>
	/// Gets the extension name. When omitted, the repository folder name is used.
	/// </summary>
	public string? Name { get; init; }

	/// <summary>
	/// Gets an optional Build submodule path used for project references.
	/// </summary>
	public string? SubmodulePath { get; init; }

	/// <summary>
	/// Gets a value indicating whether an existing extension project may be replaced.
	/// </summary>
	public bool Force { get; init; }
}
