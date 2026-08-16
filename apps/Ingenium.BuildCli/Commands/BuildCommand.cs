// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System.ComponentModel;

using Ingenium.BuildCli.Git;
using Ingenium.BuildCli.Host;
using Ingenium.BuildCli.Rendering;
using Ingenium.BuildCli.Submodule;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Ingenium.BuildCli.Commands;

/// <summary>
/// Triggers a build through the Build submodule Cake host.
/// </summary>
public sealed class BuildCommand : AsyncCommand<BuildCommand.Settings>
{
	private readonly IAnsiConsole _console;
	private readonly IBuildSubmoduleService _submodules;
	private readonly IBuildHostService _host;
	private readonly IGitTrace _trace;

	/// <summary>
	/// Initializes a new instance of the <see cref="BuildCommand"/> class.
	/// </summary>
	public BuildCommand(IAnsiConsole console, IBuildSubmoduleService submodules, IBuildHostService host, IGitTrace trace)
	{
		_console = console;
		_submodules = submodules;
		_host = host;
		_trace = trace;
	}

	/// <inheritdoc />
	public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
	{
		_trace.Enabled = settings.Verbose;
		ConsoleWriter.WriteHeader(_console, "build");

		var request = settings.ToRequest();
		var status = await _submodules.GetStatusAsync(request);
		BuildSubmoduleGuard.EnsureInitialized(status);
		_console.MarkupLine($"[grey]Build submodule initialized at {Markup.Escape(status.RelativePath ?? "build")} ({Markup.Escape(DescribeRef(status))}).[/]");
		_console.WriteLine();

		var target = string.IsNullOrWhiteSpace(settings.Target) ? "Default" : settings.Target;
		_console.MarkupLine($"Running Build target [bold]{Markup.Escape(target)}[/]...");
		_console.WriteLine();

		var extra = context.Remaining.Raw.ToArray();
		var exitCode = await _host.RunAsync(new BuildHostRequest
		{
			Repository = request,
			Target = target,
			Configuration = settings.Configuration,
			ExtraArguments = extra
		});

		_console.WriteLine();
		if (exitCode == 0)
		{
			_console.MarkupLine($"[green]Build target[/] [bold]{Markup.Escape(target)}[/] [green]completed.[/]");
			return ExitCodes.Success;
		}

		_console.MarkupLine($"[red]Build target[/] [bold]{Markup.Escape(target)}[/] [red]failed with exit code {exitCode}.[/]");
		return ExitCodes.BuildFailed;
	}

	private static string DescribeRef(BuildSubmoduleStatus status)
	{
		if (status.CurrentTags.Count > 0)
		{
			return string.Join(", ", status.CurrentTags);
		}

		return string.IsNullOrEmpty(status.Commit) ? "unknown" : ConsoleWriter.ShortSha(status.Commit);
	}

	/// <summary>
	/// Settings for <see cref="BuildCommand"/>.
	/// </summary>
	public sealed class Settings : RepositorySettings
	{
		[CommandArgument(0, "[TARGET]")]
		[Description("The Cake target to run. Defaults to Default.")]
		public string? Target { get; init; }

		[CommandOption("-c|--configuration <CONFIGURATION>")]
		[Description("The build configuration forwarded to the Build host.")]
		public string? Configuration { get; init; }
	}
}
