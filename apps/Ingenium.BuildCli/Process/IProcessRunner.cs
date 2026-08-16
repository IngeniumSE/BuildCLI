// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

namespace Ingenium.BuildCli.Execution;

/// <summary>
/// Runs child processes used by the CLI.
/// </summary>
public interface IProcessRunner
{
	/// <summary>
	/// Returns <c>true</c> when <paramref name="fileName"/> can be started.
	/// </summary>
	bool IsAvailable(string fileName);

	/// <summary>
	/// Runs a process in <paramref name="workingDirectory"/>.
	/// </summary>
	/// <param name="inheritOutput">
	/// When <c>true</c>, the child process writes directly to the current console.
	/// </param>
	Task<ProcessRunResult> RunAsync(
		string fileName,
		IReadOnlyList<string> arguments,
		string workingDirectory,
		bool inheritOutput,
		CancellationToken cancellationToken = default);
}
