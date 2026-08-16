// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System.ComponentModel;

using Ingenium.BuildCli.Git;
using Ingenium.BuildCli.Rendering;
using Ingenium.BuildCli.Submodule;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Ingenium.BuildCli.Commands;

/// <summary>
/// Adds the Ingenium Build submodule to a parent repository.
/// </summary>
public sealed class InitCommand : AsyncCommand<InitCommand.Settings>
{
	private readonly IAnsiConsole _console;
	private readonly IBuildSubmoduleService _service;
	private readonly IGitTrace _trace;

	/// <summary>
	/// Initializes a new instance of the <see cref="InitCommand"/> class.
	/// </summary>
	public InitCommand(IAnsiConsole console, IBuildSubmoduleService service, IGitTrace trace)
	{
		_console = console;
		_service = service;
		_trace = trace;
	}

	/// <inheritdoc />
	public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
	{
		_trace.Enabled = settings.Verbose;
		ConsoleWriter.WriteHeader(_console, "init");

		var request = settings.ToRequest(settings.Tag, settings.Force);
		var change = await _console.Status()
			.Spinner(Spinner.Known.Dots)
			.StartAsync("Adding the Build submodule...", async _ =>
				await _service.InitAsync(request));

		_console.MarkupLine(change.Added
			? "[green]Added[/] the Build submodule."
			: "[green]Initialized[/] the existing Build submodule.");
		_console.WriteLine();
		ConsoleWriter.WriteChange(_console, change);
		return ExitCodes.Success;
	}

	/// <summary>
	/// Settings for <see cref="InitCommand"/>.
	/// </summary>
	public sealed class Settings : RepositorySettings
	{
		[CommandOption("-t|--tag <REF>")]
		[Description("A tag, branch, or commit to check out. Defaults to the latest tag.")]
		public string? Tag { get; init; }

		[CommandOption("-f|--force")]
		[Description("Replace or re-initialize an existing Build submodule.")]
		public bool Force { get; init; }
	}
}
