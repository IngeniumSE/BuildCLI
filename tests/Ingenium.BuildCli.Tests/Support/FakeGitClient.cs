// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using Ingenium.BuildCli.Git;

using Ingenium.BuildCli;

namespace Ingenium.BuildCli.Tests.Support;

/// <summary>
/// Records git invocations and materializes a minimal BuildCLI layout on clone.
/// </summary>
internal sealed class FakeGitClient : IGitClient
{
	public bool Available { get; set; } = true;

	public List<IReadOnlyList<string>> Commands { get; } = [];

	public int CloneExitCode { get; set; }

	public string CloneError { get; set; } = "clone failed";

	public bool WriteProjectOnClone { get; set; } = true;

	public bool IsGitAvailable()
	{
		return Available;
	}

	public Task<string> GetRepositoryRootAsync(string path, CancellationToken cancellationToken = default)
	{
		throw new NotSupportedException();
	}

	public Task<GitResult> RunAsync(
		string workingDirectory,
		IReadOnlyList<string> arguments,
		CancellationToken cancellationToken = default)
	{
		Commands.Add(arguments.ToArray());

		if (arguments.Count > 0 && arguments[0] == "clone")
		{
			if (CloneExitCode != 0)
			{
				return Task.FromResult(new GitResult(CloneExitCode, string.Empty, CloneError, "git clone"));
			}

			var destination = arguments[^1];
			Directory.CreateDirectory(destination);
			if (WriteProjectOnClone)
			{
				WriteProject(destination);
			}

			return Task.FromResult(new GitResult(0, string.Empty, string.Empty, "git clone"));
		}

		return Task.FromResult(new GitResult(0, string.Empty, string.Empty, "git"));
	}

	public async Task<GitResult> RunRequiredAsync(
		string workingDirectory,
		IReadOnlyList<string> arguments,
		string failureMessage,
		int exitCode = ExitCodes.GeneralError,
		CancellationToken cancellationToken = default)
	{
		var result = await RunAsync(workingDirectory, arguments, cancellationToken);
		if (!result.IsSuccess)
		{
			throw new BuildCliException($"{failureMessage}{Environment.NewLine}{result.ErrorMessage}", exitCode);
		}

		return result;
	}

	public static void WriteProject(string sourceRoot)
	{
		var projectDir = Path.Combine(sourceRoot, "apps", "Ingenium.BuildCli");
		Directory.CreateDirectory(projectDir);
		File.WriteAllText(Path.Combine(projectDir, "Ingenium.BuildCli.csproj"), "<Project />");
	}
}
