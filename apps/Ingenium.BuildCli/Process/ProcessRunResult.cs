// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

namespace Ingenium.BuildCli.Execution;

/// <summary>
/// The captured result of a child process.
/// </summary>
/// <param name="ExitCode">The process exit code.</param>
/// <param name="StandardOutput">Captured standard output, when redirected.</param>
/// <param name="StandardError">Captured standard error, when redirected.</param>
public sealed record ProcessRunResult(int ExitCode, string StandardOutput, string StandardError)
{
	/// <summary>
	/// Gets a value indicating whether the process exited successfully.
	/// </summary>
	public bool IsSuccess => ExitCode == 0;
}
