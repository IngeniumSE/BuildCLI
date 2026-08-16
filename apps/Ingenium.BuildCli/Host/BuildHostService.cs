// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using Ingenium.BuildCli.Execution;
using Ingenium.BuildCli.Submodule;

namespace Ingenium.BuildCli.Host;

/// <summary>
/// Locates <c>apps/Build</c> in the submodule and runs it with <c>dotnet</c>.
/// </summary>
public sealed class BuildHostService : IBuildHostService
{
	private readonly IBuildSubmoduleService _submodules;
	private readonly IProcessRunner _processes;

	/// <summary>
	/// Initializes a new instance of the <see cref="BuildHostService"/> class.
	/// </summary>
	public BuildHostService(IBuildSubmoduleService submodules, IProcessRunner processes)
	{
		_submodules = submodules;
		_processes = processes;
	}

	/// <inheritdoc />
	public async Task<int> RunAsync(BuildHostRequest request, CancellationToken cancellationToken = default)
	{
		if (!_processes.IsAvailable("dotnet"))
		{
			throw new BuildCliException(
				"dotnet was not found on PATH. Install the .NET SDK and try again.",
				ExitCodes.BuildFailed);
		}

		var status = await _submodules.GetStatusAsync(request.Repository, cancellationToken);
		BuildSubmoduleGuard.EnsureInitialized(status);
		if (string.IsNullOrWhiteSpace(status.RelativePath))
		{
			throw new BuildCliException(
				$"The Build submodule path could not be resolved. Run '{CliInfo.Name} init' first.",
				ExitCodes.SubmoduleNotFound);
		}

		var submoduleRoot = Path.Combine(status.RepositoryRoot, status.RelativePath.Replace('/', Path.DirectorySeparatorChar));
		var project = FindBuildProject(submoduleRoot);
		var arguments = BuildHostArguments.Create(request.Target, request.Configuration, request.ExtraArguments);

		await RestoreToolsAsync(submoduleRoot, cancellationToken);

		var result = await _processes.RunAsync(
			"dotnet",
			arguments.Prepend(project).Prepend("--project").Prepend("run").ToArray(),
			submoduleRoot,
			inheritOutput: true,
			cancellationToken);

		return result.ExitCode;
	}

	/// <summary>
	/// Finds the Cake host project under <c>apps/Build</c>.
	/// </summary>
	public static string FindBuildProject(string submoduleRoot)
	{
		var apps = Path.Combine(submoduleRoot, "apps");
		if (Directory.Exists(apps))
		{
			var matches = Directory.GetFiles(apps, "Build.csproj", SearchOption.AllDirectories);
			var preferred = matches.FirstOrDefault(path =>
				string.Equals(Path.GetFileName(Path.GetDirectoryName(path)), "Build", StringComparison.OrdinalIgnoreCase));
			if (preferred is not null)
			{
				return preferred;
			}

			if (matches.Length > 0)
			{
				return matches[0];
			}
		}

		throw new BuildCliException(
			$"Could not find apps/Build/Build.csproj under '{submoduleRoot}'.",
			ExitCodes.SubmoduleNotFound);
	}

	private async Task RestoreToolsAsync(string submoduleRoot, CancellationToken cancellationToken)
	{
		var manifest = Path.Combine(submoduleRoot, ".config", "dotnet-tools.json");
		if (!File.Exists(manifest))
		{
			return;
		}

		var result = await _processes.RunAsync(
			"dotnet",
			["tool", "restore"],
			submoduleRoot,
			inheritOutput: true,
			cancellationToken);

		if (!result.IsSuccess)
		{
			throw new BuildCliException(
				"dotnet tool restore failed in the Build submodule.",
				ExitCodes.BuildFailed);
		}
	}
}

/// <summary>
/// Builds the <c>dotnet run</c> argument list for the Cake host.
/// </summary>
public static class BuildHostArguments
{
	/// <summary>
	/// Creates Cake host arguments, omitting a duplicate <c>--target</c> when extra arguments already supply one.
	/// </summary>
	public static IReadOnlyList<string> Create(string? target, string? configuration, IReadOnlyList<string>? extraArguments)
	{
		var extras = extraArguments ?? [];
		var args = new List<string> { "--no-launch-profile", "--" };

		if (!HasSwitch(extras, "--target") && !HasSwitch(extras, "-t"))
		{
			args.Add("--target");
			args.Add(string.IsNullOrWhiteSpace(target) ? "Default" : target);
		}

		if (!string.IsNullOrWhiteSpace(configuration) && !HasSwitch(extras, "--configuration"))
		{
			args.Add("--configuration");
			args.Add(configuration);
		}

		args.AddRange(extras);
		return args;
	}

	private static bool HasSwitch(IReadOnlyList<string> arguments, string name)
	{
		return arguments.Any(argument => argument.Equals(name, StringComparison.OrdinalIgnoreCase));
	}
}
