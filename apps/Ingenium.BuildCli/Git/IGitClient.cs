// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

namespace Ingenium.BuildCli.Git;

/// <summary>
/// Runs git commands and inspects the local environment.
/// </summary>
public interface IGitClient
{
	/// <summary>
	/// Returns <c>true</c> when a git executable can be located.
	/// </summary>
	bool IsGitAvailable();

	/// <summary>
	/// Resolves the git repository root that contains <paramref name="path"/>.
	/// </summary>
	/// <param name="path">A file or directory inside the repository.</param>
	/// <param name="cancellationToken">A token used to cancel the operation.</param>
	/// <returns>The absolute repository root path.</returns>
	Task<string> GetRepositoryRootAsync(string path, CancellationToken cancellationToken = default);

	/// <summary>
	/// Runs git in <paramref name="workingDirectory"/> with the supplied arguments.
	/// </summary>
	Task<GitResult> RunAsync(
		string workingDirectory,
		IReadOnlyList<string> arguments,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Runs git and throws <see cref="BuildCliException"/> when the command fails.
	/// </summary>
	Task<GitResult> RunRequiredAsync(
		string workingDirectory,
		IReadOnlyList<string> arguments,
		string failureMessage,
		int exitCode = ExitCodes.GeneralError,
		CancellationToken cancellationToken = default);
}
