// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System.Diagnostics;

using Ingenium.BuildCli.Git;

namespace Ingenium.BuildCli.Tests.Support;

/// <summary>
/// Creates temporary git repositories used by integration tests.
/// </summary>
public sealed class GitTestWorkspace : IDisposable
{
	private GitTestWorkspace(string root, string buildRepo, string parentRepo)
	{
		Root = root;
		BuildRepo = buildRepo;
		ParentRepo = parentRepo;
	}

	public string Root { get; }

	public string BuildRepo { get; }

	public string ParentRepo { get; }

	public static GitTestWorkspace Create(bool includeTags = true)
	{
		var root = Path.Combine(Path.GetTempPath(), "buildcli-tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(root);

		var buildWorking = Path.Combine(root, "build-src");
		var buildRepo = Path.Combine(root, "build.git");
		var parentRepo = Path.Combine(root, "parent");

		Directory.CreateDirectory(buildWorking);
		Git(buildWorking, "init", "-b", "main");
		ConfigureIdentity(buildWorking);
		File.WriteAllText(Path.Combine(buildWorking, "README.md"), "build v1");
		Git(buildWorking, "add", ".");
		Git(buildWorking, "commit", "-m", "Initial build");
		if (includeTags)
		{
			Git(buildWorking, "tag", "-a", "v1.0.0", "-m", "v1.0.0");
		}

		File.WriteAllText(Path.Combine(buildWorking, "README.md"), "build v2");
		Git(buildWorking, "add", ".");
		Git(buildWorking, "commit", "-m", "Second build");
		if (includeTags)
		{
			Git(buildWorking, "tag", "-a", "v1.1.0", "-m", "v1.1.0");
		}

		Git(buildWorking, "clone", "--bare", buildWorking, buildRepo);

		Directory.CreateDirectory(parentRepo);
		Git(parentRepo, "init", "-b", "main");
		ConfigureIdentity(parentRepo);
		File.WriteAllText(Path.Combine(parentRepo, "README.md"), "parent");
		Git(parentRepo, "add", ".");
		Git(parentRepo, "commit", "-m", "Initial parent");

		return new GitTestWorkspace(root, buildRepo, parentRepo);
	}

	public static GitClient CreateClient()
	{
		return new GitClient(globalArguments: ["-c", "protocol.file.allow=always"]);
	}

	public string Git(params string[] arguments)
	{
		return Git(ParentRepo, arguments);
	}

	public static string Git(string workingDirectory, params string[] arguments)
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = "git",
			WorkingDirectory = workingDirectory,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false
		};

		startInfo.ArgumentList.Add("-c");
		startInfo.ArgumentList.Add("protocol.file.allow=always");
		foreach (var argument in arguments)
		{
			startInfo.ArgumentList.Add(argument);
		}

		using var process = Process.Start(startInfo)
			?? throw new InvalidOperationException("Failed to start git.");
		var stdout = process.StandardOutput.ReadToEnd();
		var stderr = process.StandardError.ReadToEnd();
		process.WaitForExit();
		if (process.ExitCode != 0)
		{
			throw new InvalidOperationException($"git {string.Join(' ', arguments)} failed: {stderr}");
		}

		return stdout.Trim();
	}

	public void Dispose()
	{
		try
		{
			Directory.Delete(Root, recursive: true);
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
	}

	private static void ConfigureIdentity(string workingDirectory)
	{
		Git(workingDirectory, "config", "user.email", "buildcli@ingenium.local");
		Git(workingDirectory, "config", "user.name", "BuildCLI Tests");
		Git(workingDirectory, "config", "commit.gpgsign", "false");
		Git(workingDirectory, "config", "tag.gpgsign", "false");
		Git(workingDirectory, "config", "init.defaultBranch", "main");
		Git(workingDirectory, "config", "protocol.file.allow", "always");
	}
}
