// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System.Diagnostics;
using System.Text;

namespace Ingenium.BuildCli.Git;

/// <summary>
/// Invokes the git executable as a child process.
/// </summary>
public sealed class GitClient : IGitClient
{
	private readonly string? _gitExecutable;
	private readonly IReadOnlyList<string> _globalArguments;
	private readonly IGitTrace? _trace;

	/// <summary>
	/// Initializes a new instance of the <see cref="GitClient"/> class.
	/// </summary>
	/// <param name="gitExecutable">
	/// An optional explicit path to git. When omitted, <c>git</c> is resolved from PATH.
	/// </param>
	/// <param name="globalArguments">
	/// Optional arguments inserted before every git command, such as <c>-c protocol.file.allow=always</c>.
	/// </param>
	/// <param name="trace">Optional diagnostic sink used when verbose mode is enabled.</param>
	public GitClient(string? gitExecutable = null, IEnumerable<string>? globalArguments = null, IGitTrace? trace = null)
	{
		_gitExecutable = gitExecutable;
		_globalArguments = globalArguments?.ToArray() ?? [];
		_trace = trace;
	}

	/// <inheritdoc />
	public bool IsGitAvailable()
	{
		try
		{
			using var process = Start(Environment.CurrentDirectory, ["--version"]);
			process.WaitForExit(5000);
			return process.ExitCode == 0;
		}
		catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException or InvalidOperationException)
		{
			return false;
		}
	}

	/// <inheritdoc />
	public async Task<string> GetRepositoryRootAsync(string path, CancellationToken cancellationToken = default)
	{
		var workingDirectory = ResolveWorkingDirectory(path);
		var result = await RunAsync(workingDirectory, ["rev-parse", "--show-toplevel"], cancellationToken);
		if (!result.IsSuccess)
		{
			throw new BuildCliException(
				$"'{path}' is not inside a git repository.",
				ExitCodes.NotAGitRepository);
		}

		var root = result.StandardOutput.Trim();
		if (string.IsNullOrWhiteSpace(root))
		{
			throw new BuildCliException(
				$"'{path}' is not inside a git repository.",
				ExitCodes.NotAGitRepository);
		}

		return Path.GetFullPath(root);
	}

	/// <inheritdoc />
	public async Task<GitResult> RunAsync(
		string workingDirectory,
		IReadOnlyList<string> arguments,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);
		ArgumentNullException.ThrowIfNull(arguments);

		if (!Directory.Exists(workingDirectory))
		{
			throw new BuildCliException($"Working directory '{workingDirectory}' does not exist.");
		}

		using var process = Start(workingDirectory, arguments);
		var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
		var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

		try
		{
			await process.WaitForExitAsync(cancellationToken);
		}
		catch (OperationCanceledException)
		{
			TryKill(process);
			throw;
		}

		var stdout = await stdoutTask;
		var stderr = await stderrTask;
		var result = new GitResult(process.ExitCode, stdout, stderr, FormatCommand(arguments));
		_trace?.Write($"$ {result.Command}");
		if (!string.IsNullOrWhiteSpace(result.StandardOutput))
		{
			_trace?.Write(result.StandardOutput.TrimEnd());
		}

		if (!string.IsNullOrWhiteSpace(result.StandardError))
		{
			_trace?.Write(result.StandardError.TrimEnd());
		}

		return result;
	}

	/// <inheritdoc />
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

	private Process Start(string workingDirectory, IReadOnlyList<string> arguments)
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = _gitExecutable ?? "git",
			WorkingDirectory = workingDirectory,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
			StandardOutputEncoding = Encoding.UTF8,
			StandardErrorEncoding = Encoding.UTF8
		};

		foreach (var argument in _globalArguments)
		{
			startInfo.ArgumentList.Add(argument);
		}

		foreach (var argument in arguments)
		{
			startInfo.ArgumentList.Add(argument);
		}

		// Keep git non-interactive so the CLI never hangs waiting for a prompt.
		startInfo.Environment["GIT_TERMINAL_PROMPT"] = "0";
		startInfo.Environment["GCM_INTERACTIVE"] = "never";

		var process = new Process { StartInfo = startInfo };
		if (!process.Start())
		{
			throw new BuildCliException("Failed to start git.", ExitCodes.GitNotFound);
		}

		return process;
	}

	private static string ResolveWorkingDirectory(string path)
	{
		var fullPath = Path.GetFullPath(path);
		if (Directory.Exists(fullPath))
		{
			return fullPath;
		}

		var directory = Path.GetDirectoryName(fullPath);
		if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
		{
			return directory;
		}

		throw new BuildCliException($"Path '{path}' does not exist.");
	}

	private static string FormatCommand(IReadOnlyList<string> arguments)
	{
		return "git " + string.Join(' ', arguments.Select(QuoteIfNeeded));
	}

	private static string QuoteIfNeeded(string value)
	{
		return value.Contains(' ', StringComparison.Ordinal) ? $"\"{value}\"" : value;
	}

	private static void TryKill(Process process)
	{
		try
		{
			if (!process.HasExited)
			{
				process.Kill(entireProcessTree: true);
			}
		}
		catch (InvalidOperationException)
		{
		}
	}
}
