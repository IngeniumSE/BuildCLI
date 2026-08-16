// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

namespace Ingenium.BuildCli.Git;

/// <summary>
/// The captured result of a git process invocation.
/// </summary>
/// <param name="ExitCode">The process exit code.</param>
/// <param name="StandardOutput">Captured standard output.</param>
/// <param name="StandardError">Captured standard error.</param>
/// <param name="Command">The command line that was executed.</param>
public sealed record GitResult(
	int ExitCode,
	string StandardOutput,
	string StandardError,
	string Command)
{
	/// <summary>
	/// Gets a value indicating whether the process exited successfully.
	/// </summary>
	public bool IsSuccess => ExitCode == 0;

	/// <summary>
	/// Gets a trimmed combined error message suitable for display.
	/// </summary>
	public string ErrorMessage
	{
		get
		{
			var error = StandardError.Trim();
			if (!string.IsNullOrEmpty(error))
			{
				return error;
			}

			var output = StandardOutput.Trim();
			return string.IsNullOrEmpty(output) ? $"git exited with code {ExitCode}." : output;
		}
	}
}
