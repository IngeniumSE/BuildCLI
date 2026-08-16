// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System.Diagnostics;
using System.Text;

using Ingenium.BuildCli.Git;

namespace Ingenium.BuildCli.Execution;

/// <summary>
/// Starts child processes with optional output capture.
/// </summary>
public sealed class ProcessRunner : IProcessRunner
{
	private readonly IGitTrace? _trace;

	/// <summary>
	/// Initializes a new instance of the <see cref="ProcessRunner"/> class.
	/// </summary>
	public ProcessRunner(IGitTrace? trace = null)
	{
		_trace = trace;
	}

	/// <inheritdoc />
	public bool IsAvailable(string fileName)
	{
		try
		{
			using var process = Start(ResolveFileName(fileName), ["--version"], Environment.CurrentDirectory, inheritOutput: false);
			process.WaitForExit(5000);
			return process.ExitCode == 0;
		}
		catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException or InvalidOperationException)
		{
			return false;
		}
	}

	/// <inheritdoc />
	public async Task<ProcessRunResult> RunAsync(
		string fileName,
		IReadOnlyList<string> arguments,
		string workingDirectory,
		bool inheritOutput,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
		ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);
		ArgumentNullException.ThrowIfNull(arguments);

		if (!Directory.Exists(workingDirectory))
		{
			throw new BuildCliException($"Working directory '{workingDirectory}' does not exist.");
		}

		fileName = ResolveFileName(fileName);
		_trace?.Write($"$ {fileName} {string.Join(' ', arguments)}");

		using var process = Start(fileName, arguments, workingDirectory, inheritOutput);
		Task<string>? stdoutTask = null;
		Task<string>? stderrTask = null;
		if (!inheritOutput)
		{
			stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
			stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
		}

		try
		{
			await process.WaitForExitAsync(cancellationToken);
		}
		catch (OperationCanceledException)
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

			throw;
		}

		var stdout = stdoutTask is null ? string.Empty : await stdoutTask;
		var stderr = stderrTask is null ? string.Empty : await stderrTask;
		if (!inheritOutput)
		{
			if (!string.IsNullOrWhiteSpace(stdout))
			{
				_trace?.Write(stdout.TrimEnd());
			}

			if (!string.IsNullOrWhiteSpace(stderr))
			{
				_trace?.Write(stderr.TrimEnd());
			}
		}

		return new ProcessRunResult(process.ExitCode, stdout, stderr);
	}

	private static System.Diagnostics.Process Start(
		string fileName,
		IReadOnlyList<string> arguments,
		string workingDirectory,
		bool inheritOutput)
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = fileName,
			WorkingDirectory = workingDirectory,
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = !inheritOutput,
			RedirectStandardError = !inheritOutput
		};

		if (!inheritOutput)
		{
			startInfo.StandardOutputEncoding = Encoding.UTF8;
			startInfo.StandardErrorEncoding = Encoding.UTF8;
		}

		foreach (var argument in arguments)
		{
			startInfo.ArgumentList.Add(argument);
		}

		startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
		startInfo.Environment["DOTNET_NOLOGO"] = "1";
		ConfigureDotnetEnvironment(startInfo, fileName);

		var process = new System.Diagnostics.Process { StartInfo = startInfo };
		if (!process.Start())
		{
			throw new BuildCliException($"Failed to start '{fileName}'.");
		}

		return process;
	}

	private static string ResolveFileName(string fileName)
	{
		if (!DotnetMuxer.IsMuxerName(fileName) || Path.IsPathRooted(fileName))
		{
			return fileName;
		}

		return DotnetMuxer.Resolve() ?? fileName;
	}

	private static void ConfigureDotnetEnvironment(ProcessStartInfo startInfo, string fileName)
	{
		if (!DotnetMuxer.IsMuxerName(fileName))
		{
			return;
		}

		string fullPath;
		try
		{
			fullPath = Path.GetFullPath(fileName);
		}
		catch (ArgumentException)
		{
			return;
		}

		if (!File.Exists(fullPath))
		{
			return;
		}

		var root = Path.GetDirectoryName(fullPath);
		if (string.IsNullOrEmpty(root))
		{
			return;
		}

		startInfo.Environment["DOTNET_ROOT"] = root;
		startInfo.Environment["DOTNET_HOST_PATH"] = fullPath;
	}
}
