// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using Ingenium.BuildCli.Git;
using Ingenium.BuildCli.Submodule;

namespace Ingenium.BuildCli.Extensions;

/// <summary>
/// Writes the <c>build-extensions</c> project layout imported by <c>apps/Build/Build.csproj</c>.
/// </summary>
public sealed class BuildExtensionService : IBuildExtensionService
{
	private readonly IGitClient _git;

	/// <summary>
	/// Initializes a new instance of the <see cref="BuildExtensionService"/> class.
	/// </summary>
	public BuildExtensionService(IGitClient git)
	{
		_git = git;
	}

	/// <inheritdoc />
	public async Task<BuildExtensionScaffold> CreateAsync(BuildExtensionRequest request, CancellationToken cancellationToken = default)
	{
		if (!_git.IsGitAvailable())
		{
			throw new BuildCliException("git was not found on PATH. Install Git and try again.", ExitCodes.GitNotFound);
		}

		var root = await _git.GetRepositoryRootAsync(request.RepositoryPath, cancellationToken);
		var submodulePath = ResolveSubmodulePath(root, request.SubmodulePath);
		var projectName = BuildExtensionNames.ToProjectName(request.Name ?? Path.GetFileName(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));
		var extensionsRoot = Path.Combine(root, BuildExtensionNames.FolderName);
		var projectDirectory = Path.Combine(extensionsRoot, projectName);
		var projectFile = Path.Combine(projectDirectory, projectName + ".csproj");

		if (File.Exists(projectFile) && !request.Force)
		{
			throw new BuildCliException(
				$"A build extension already exists at '{Path.Combine(BuildExtensionNames.FolderName, projectName)}'. Use --force to replace it.",
				ExitCodes.AlreadyExists);
		}

		Directory.CreateDirectory(projectDirectory);

		var written = new List<string>();
		WriteIfMissingOrForced(
			Path.Combine(extensionsRoot, "Directory.Build.props"),
			BuildExtensionTemplates.DirectoryBuildProps(submodulePath),
			overwrite: false,
			written);
		WriteIfMissingOrForced(
			Path.Combine(extensionsRoot, "Directory.Build.targets"),
			BuildExtensionTemplates.DirectoryBuildTargets(submodulePath),
			overwrite: false,
			written);
		WriteIfMissingOrForced(
			projectFile,
			BuildExtensionTemplates.Project(submodulePath),
			overwrite: request.Force,
			written);
		WriteIfMissingOrForced(
			Path.Combine(projectDirectory, "SampleTask.cs"),
			BuildExtensionTemplates.SampleTask(projectName),
			overwrite: request.Force,
			written);

		return new BuildExtensionScaffold
		{
			RepositoryRoot = root,
			ProjectName = projectName,
			ProjectDirectory = projectDirectory,
			WrittenFiles = written
		};
	}

	private static string ResolveSubmodulePath(string repositoryRoot, string? requested)
	{
		if (!string.IsNullOrWhiteSpace(requested))
		{
			return requested.Replace('\\', '/').Trim('/');
		}

		var entries = GitmodulesParser.ParseFile(Path.Combine(repositoryRoot, ".gitmodules"));
		var build = entries.FirstOrDefault(entry => BuildRepositoryUrls.IsBuildRepository(entry.Url))
			?? entries.FirstOrDefault(entry =>
				entry.Path.Equals(BuildRepositoryUrls.DefaultPath, StringComparison.OrdinalIgnoreCase)
				|| entry.Path.Equals("Build", StringComparison.Ordinal));

		return build?.Path ?? BuildRepositoryUrls.DefaultPath;
	}

	private static void WriteIfMissingOrForced(string path, string contents, bool overwrite, List<string> written)
	{
		if (File.Exists(path) && !overwrite)
		{
			return;
		}

		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, contents);
		written.Add(path);
	}
}
