// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using Ingenium.BuildCli.Execution;

namespace Ingenium.BuildCli.Tests.Support;

/// <summary>
/// Records process invocations and optionally writes a fake published binary.
/// </summary>
internal sealed class FakeProcessRunner : IProcessRunner
{
	public List<(string FileName, IReadOnlyList<string> Arguments, string WorkingDirectory)> Calls { get; } = [];

	public bool DotnetAvailable { get; set; } = true;

	public string PublishedVersion { get; set; } = "0.1.0";

	public int PublishExitCode { get; set; }

	public string PublishError { get; set; } = string.Empty;

	public bool WritePublishedBinary { get; set; } = true;

	public bool IsAvailable(string fileName)
	{
		return fileName == "dotnet" ? DotnetAvailable : true;
	}

	public Task<ProcessRunResult> RunAsync(
		string fileName,
		IReadOnlyList<string> arguments,
		string workingDirectory,
		bool inheritOutput,
		CancellationToken cancellationToken = default)
	{
		Calls.Add((fileName, arguments.ToArray(), workingDirectory));

		if (fileName == "dotnet" && arguments.Count > 0 && arguments[0] == "publish")
		{
			if (PublishExitCode != 0)
			{
				return Task.FromResult(new ProcessRunResult(PublishExitCode, string.Empty, PublishError));
			}

			if (WritePublishedBinary)
			{
				var output = OutputDirectory(arguments);
				Directory.CreateDirectory(output);
				var executable = Path.Combine(
					output,
					OperatingSystem.IsWindows() ? "bld.exe" : "bld");
				File.WriteAllText(executable, "fake-bld");
			}

			return Task.FromResult(new ProcessRunResult(0, string.Empty, string.Empty));
		}

		if (arguments.Contains("--version"))
		{
			return Task.FromResult(new ProcessRunResult(0, PublishedVersion + Environment.NewLine, string.Empty));
		}

		return Task.FromResult(new ProcessRunResult(0, string.Empty, string.Empty));
	}

	private static string OutputDirectory(IReadOnlyList<string> arguments)
	{
		for (var i = 0; i < arguments.Count - 1; i++)
		{
			if (arguments[i] == "-o")
			{
				return arguments[i + 1];
			}
		}

		throw new InvalidOperationException("dotnet publish was invoked without -o.");
	}
}
