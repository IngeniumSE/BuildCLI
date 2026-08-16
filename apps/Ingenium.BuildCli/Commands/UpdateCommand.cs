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
/// Updates the Build submodule to the latest tag or a specific ref.
/// </summary>
public sealed class UpdateCommand : AsyncCommand<UpdateCommand.Settings>
{
	private readonly IAnsiConsole _console;
	private readonly IBuildSubmoduleService _service;
	private readonly IGitTrace _trace;

	/// <summary>
	/// Initializes a new instance of the <see cref="UpdateCommand"/> class.
	/// </summary>
	public UpdateCommand(IAnsiConsole console, IBuildSubmoduleService service, IGitTrace trace)
	{
		_console = console;
		_service = service;
		_trace = trace;
	}

	/// <inheritdoc />
	public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
	{
		_trace.Enabled = settings.Verbose;
		ConsoleWriter.WriteHeader(_console, "update");

		var request = settings.ToRequest(settings.Tag);
		var change = await _console.Status()
			.Spinner(Spinner.Known.Dots)
			.StartAsync(
				string.IsNullOrWhiteSpace(settings.Tag)
					? "Updating the Build submodule to the latest tag..."
					: $"Updating the Build submodule to {settings.Tag}...",
				async _ => await _service.UpdateAsync(request));

		if (!string.IsNullOrEmpty(change.PreviousCommit) &&
			string.Equals(change.PreviousCommit, change.Commit, StringComparison.OrdinalIgnoreCase))
		{
			_console.MarkupLine("[green]Build submodule is already at the requested ref.[/]");
		}
		else
		{
			_console.MarkupLine("[green]Updated[/] the Build submodule.");
		}

		_console.WriteLine();
		ConsoleWriter.WriteChange(_console, change);
		return ExitCodes.Success;
	}

	/// <summary>
	/// Settings for <see cref="UpdateCommand"/>.
	/// </summary>
	public sealed class Settings : RepositorySettings
	{
		[CommandOption("-t|--tag <REF>")]
		[Description("A tag, branch, or commit to check out. Defaults to the latest tag.")]
		public string? Tag { get; init; }
	}
}
