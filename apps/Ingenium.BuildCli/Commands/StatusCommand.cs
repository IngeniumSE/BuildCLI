// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using Ingenium.BuildCli.Git;
using Ingenium.BuildCli.Rendering;
using Ingenium.BuildCli.Submodule;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Ingenium.BuildCli.Commands;

/// <summary>
/// Shows the current Build submodule state.
/// </summary>
public sealed class StatusCommand : AsyncCommand<StatusCommand.Settings>
{
	private readonly IAnsiConsole _console;
	private readonly IBuildSubmoduleService _service;
	private readonly IGitTrace _trace;

	/// <summary>
	/// Initializes a new instance of the <see cref="StatusCommand"/> class.
	/// </summary>
	public StatusCommand(IAnsiConsole console, IBuildSubmoduleService service, IGitTrace trace)
	{
		_console = console;
		_service = service;
		_trace = trace;
	}

	/// <inheritdoc />
	public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
	{
		_trace.Enabled = settings.Verbose;
		ConsoleWriter.WriteHeader(_console, "status");

		var status = await _console.Status()
			.Spinner(Spinner.Known.Dots)
			.StartAsync("Reading Build submodule status...", async _ =>
				await _service.GetStatusAsync(settings.ToRequest()));

		ConsoleWriter.WriteStatus(_console, status);
		return ExitCodes.Success;
	}

	/// <summary>
	/// Settings for <see cref="StatusCommand"/>.
	/// </summary>
	public sealed class Settings : RepositorySettings
	{
	}
}
