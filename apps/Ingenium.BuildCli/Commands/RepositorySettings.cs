// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System.ComponentModel;

using Ingenium.BuildCli.Submodule;

using Spectre.Console.Cli;

namespace Ingenium.BuildCli.Commands;

/// <summary>
/// Shared options that identify the parent repository and Build submodule.
/// </summary>
public class RepositorySettings : CommandSettings
{
	[CommandOption("-p|--path <PATH>")]
	[Description("Path to the parent git repository. Defaults to the current directory.")]
	public string? Path { get; init; }

	[CommandOption("--submodule-path <PATH>")]
	[Description("Relative path of the Build submodule. Defaults to an existing Build entry, or 'build'.")]
	public string? SubmodulePath { get; init; }

	[CommandOption("--url <URL>")]
	[Description("Override the Build submodule URL.")]
	public string? Url { get; init; }

	[CommandOption("--https")]
	[Description("Use the HTTPS Build URL instead of SSH.")]
	public bool UseHttps { get; init; }

	[CommandOption("--verbose")]
	[Description("Write the git commands that are executed.")]
	public bool Verbose { get; init; }

	/// <summary>
	/// Creates a service request from these settings.
	/// </summary>
	public BuildSubmoduleRequest ToRequest(string? tag = null, bool force = false)
	{
		return new BuildSubmoduleRequest
		{
			RepositoryPath = string.IsNullOrWhiteSpace(Path) ? Environment.CurrentDirectory : Path,
			SubmodulePath = SubmodulePath,
			Url = Url,
			UseHttps = UseHttps,
			Tag = tag,
			Force = force
		};
	}
}
