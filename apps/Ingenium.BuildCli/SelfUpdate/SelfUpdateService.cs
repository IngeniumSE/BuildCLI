// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using Ingenium.BuildCli.Execution;
using Ingenium.BuildCli.Git;

namespace Ingenium.BuildCli.SelfUpdate;

/// <summary>
/// Publishes this repository and replaces the installed <c>bld</c> binary.
/// </summary>
public sealed class SelfUpdateService : ISelfUpdateService
{
	private readonly IGitClient _git;
	private readonly IProcessRunner _processes;

	/// <summary>
	/// Initializes a new instance of the <see cref="SelfUpdateService"/> class.
	/// </summary>
	public SelfUpdateService(IGitClient git, IProcessRunner processes)
	{
		_git = git;
		_processes = processes;
	}

	/// <inheritdoc />
	public async Task<SelfUpdateResult> UpdateAsync(
		SelfUpdateRequest request,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(request);

		if (!_processes.IsAvailable("dotnet"))
		{
			throw new BuildCliException(
				"dotnet was not found on PATH. Install the .NET SDK and try again.",
				ExitCodes.BuildFailed);
		}

		var gitRef = string.IsNullOrWhiteSpace(request.Ref) ? SelfUpdatePaths.DefaultRef : request.Ref.Trim();
		var installDir = SelfUpdatePaths.GetInstallDirectory(request.InstallDirectory);
		var binDir = SelfUpdatePaths.GetBinDirectory(request.BinDirectory);
		var rid = SelfUpdatePaths.GetRuntimeIdentifier();
		var cloned = false;
		string source;

		if (!string.IsNullOrWhiteSpace(request.Source))
		{
			source = Path.GetFullPath(request.Source);
			if (!Directory.Exists(source))
			{
				throw new BuildCliException($"Source directory '{source}' does not exist.");
			}
		}
		else
		{
			if (!_git.IsGitAvailable())
			{
				throw new BuildCliException(
					"git was not found on PATH. Install Git and try again.",
					ExitCodes.GitNotFound);
			}

			source = Path.Combine(Path.GetTempPath(), "buildcli-src-" + Guid.NewGuid().ToString("N"));
			cloned = true;
			var url = SelfUpdatePaths.GetRepositoryUrl(request.Url);
			await _git.RunRequiredAsync(
				Path.GetTempPath(),
				["clone", "--depth", "1", "--branch", gitRef, url, source],
				$"Failed to clone BuildCLI from '{url}' at '{gitRef}'.",
				cancellationToken: cancellationToken);
		}

		var publishDir = Path.Combine(Path.GetTempPath(), "buildcli-publish-" + Guid.NewGuid().ToString("N"));
		try
		{
			var project = SelfUpdatePaths.GetProjectPath(source);
			if (!File.Exists(project))
			{
				throw new BuildCliException(
					$"Could not find '{project}'. Pass --source to a BuildCLI checkout.");
			}

			Directory.CreateDirectory(publishDir);
			var publish = await _processes.RunAsync(
				"dotnet",
				SelfUpdatePublishArguments.Create(project, rid, publishDir, request.FrameworkDependent),
				source,
				inheritOutput: request.Verbose,
				cancellationToken);

			if (!publish.IsSuccess)
			{
				var detail = string.IsNullOrWhiteSpace(publish.StandardError)
					? publish.StandardOutput
					: publish.StandardError;
				throw new BuildCliException(
					$"dotnet publish failed.{(string.IsNullOrWhiteSpace(detail) ? string.Empty : Environment.NewLine + detail.Trim())}",
					ExitCodes.BuildFailed);
			}

			var executableName = SelfUpdatePaths.GetExecutableFileName();
			var publishedExecutable = Path.Combine(publishDir, executableName);
			if (!File.Exists(publishedExecutable))
			{
				throw new BuildCliException(
					$"Publish succeeded but '{publishedExecutable}' was not produced.",
					ExitCodes.BuildFailed);
			}

			var installedExecutable = InstallPublishedFiles(publishDir, installDir, executableName);
			var binLink = TryCreateBinLink(installedExecutable, binDir);
			var version = await ReadInstalledVersionAsync(installedExecutable, installDir, cancellationToken);

			return new SelfUpdateResult
			{
				ExecutablePath = installedExecutable,
				Version = version,
				RuntimeIdentifier = rid,
				Ref = gitRef,
				Source = source,
				BinLink = binLink,
				BinDirectoryOnPath = SelfUpdatePaths.IsDirectoryOnPath(OperatingSystem.IsWindows() ? installDir : binDir)
			};
		}
		finally
		{
			TryDeleteDirectory(publishDir);
			if (cloned)
			{
				TryDeleteDirectory(source);
			}
		}
	}

	private async Task<string> ReadInstalledVersionAsync(
		string executablePath,
		string workingDirectory,
		CancellationToken cancellationToken)
	{
		try
		{
			var result = await _processes.RunAsync(
				executablePath,
				["--version"],
				workingDirectory,
				inheritOutput: false,
				cancellationToken);
			if (result.IsSuccess)
			{
				var version = result.StandardOutput.Trim();
				if (!string.IsNullOrWhiteSpace(version))
				{
					return version;
				}
			}
		}
		catch (BuildCliException)
		{
		}

		return "unknown";
	}

	private static string InstallPublishedFiles(string publishDir, string installDir, string executableName)
	{
		Directory.CreateDirectory(installDir);
		CopyDirectory(publishDir, installDir);

		var installed = Path.Combine(installDir, executableName);
		if (!File.Exists(installed))
		{
			throw new BuildCliException(
				$"Failed to install '{executableName}' into '{installDir}'.",
				ExitCodes.BuildFailed);
		}

		SetExecutable(installed);
		return installed;
	}

	private static string? TryCreateBinLink(string executablePath, string binDir)
	{
		if (OperatingSystem.IsWindows())
		{
			return null;
		}

		Directory.CreateDirectory(binDir);
		var link = Path.Combine(binDir, CliInfo.Name);
		try
		{
			if (File.Exists(link) || Directory.Exists(link))
			{
				File.Delete(link);
			}

			File.CreateSymbolicLink(link, executablePath);
			return link;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
		{
			return null;
		}
	}

	private static void CopyDirectory(string source, string destination)
	{
		foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
		{
			var relative = Path.GetRelativePath(source, file);
			var dest = Path.Combine(destination, relative);
			var destDirectory = Path.GetDirectoryName(dest);
			if (!string.IsNullOrEmpty(destDirectory))
			{
				Directory.CreateDirectory(destDirectory);
			}

			ReplaceFile(file, dest);
		}
	}

	private static void ReplaceFile(string source, string destination)
	{
		try
		{
			if (File.Exists(destination))
			{
				File.Delete(destination);
			}

			File.Copy(source, destination, overwrite: true);
		}
		catch (IOException) when (OperatingSystem.IsWindows())
		{
			var pending = destination + ".new";
			File.Copy(source, pending, overwrite: true);
			throw new BuildCliException(
				$"Could not replace '{destination}' because the file is in use. The new file was written to '{pending}'. Close running bld processes and rename it.");
		}
	}

	private static void SetExecutable(string path)
	{
		if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
		{
			File.SetUnixFileMode(
				path,
				UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
				UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
				UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
		}
	}

	private static void TryDeleteDirectory(string path)
	{
		try
		{
			if (Directory.Exists(path))
			{
				Directory.Delete(path, recursive: true);
			}
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
	}
}
