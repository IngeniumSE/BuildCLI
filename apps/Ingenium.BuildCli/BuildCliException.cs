// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

namespace Ingenium.BuildCli;

/// <summary>
/// Represents a user-facing CLI failure with a specific process exit code.
/// </summary>
public sealed class BuildCliException : Exception
{
	/// <summary>
	/// Initializes a new instance of the <see cref="BuildCliException"/> class.
	/// </summary>
	/// <param name="message">The error message shown to the user.</param>
	/// <param name="exitCode">The process exit code to return.</param>
	public BuildCliException(string message, int exitCode = ExitCodes.GeneralError)
		: base(message)
	{
		ExitCode = exitCode;
	}

	/// <summary>
	/// Gets the process exit code associated with this failure.
	/// </summary>
	public int ExitCode { get; }
}

/// <summary>
/// Well-known process exit codes used by the CLI.
/// </summary>
public static class ExitCodes
{
	public const int Success = 0;
	public const int GeneralError = 1;
	public const int NotAGitRepository = 2;
	public const int GitNotFound = 3;
	public const int SubmoduleNotFound = 4;
	public const int AlreadyInitialized = 5;
	public const int RefNotFound = 6;
}
