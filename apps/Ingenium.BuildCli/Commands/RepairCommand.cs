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
/// Repairs a dirty or broken Build submodule.
/// </summary>
public sealed class RepairCommand : AsyncCommand<RepairCommand.Settings>
{
	private readonly IAnsiConsole _console;
	private readonly IBuildSubmoduleService _service;
	private readonly IGitTrace _trace;

	/// <summary>
	/// Initializes a new instance of the <see cref="RepairCommand"/> class.
	/// </summary>
	public RepairCommand(IAnsiConsole console, IBuildSubmoduleService service, IGitTrace trace)
	{
		_console = console;
		_service = service;
		_trace = trace;
	}

	/// <inheritdoc />
	public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
	{
		_trace.Enabled = settings.Verbose;
		ConsoleWriter.WriteHeader(_console, "repair");

		var strategy = ResolveStrategy(settings);
		if (strategy is null)
		{
			return ExitCodes.Cancelled;
		}

		if (!Confirm(settings, strategy.Value))
		{
			_console.MarkupLine("[yellow]Repair cancelled.[/]");
			return ExitCodes.Cancelled;
		}

		var request = settings.ToRequest(settings.Tag);
		var result = await _console.Status()
			.Spinner(Spinner.Known.Dots)
			.StartAsync($"Repairing the Build submodule ({strategy.Value.ToString().ToLowerInvariant()})...", async _ =>
				await _service.RepairAsync(request, strategy.Value));

		_console.MarkupLine("[green]Repaired[/] the Build submodule.");
		_console.WriteLine();
		ConsoleWriter.WriteRepair(_console, result);
		return ExitCodes.Success;
	}

	private RepairStrategy? ResolveStrategy(Settings settings)
	{
		if (RepairStrategyParser.TryParse(settings.Strategy, out var parsed))
		{
			return parsed;
		}

		if (!string.IsNullOrWhiteSpace(settings.Strategy))
		{
			throw new BuildCliException("Unknown repair strategy. Use stash, reset, or reinit.");
		}

		if (!_console.Profile.Capabilities.Interactive)
		{
			throw new BuildCliException("A repair strategy is required. Use --strategy stash, reset, or reinit.");
		}

		return _console.Prompt(
			new SelectionPrompt<RepairStrategy>()
				.Title("How should the Build submodule be repaired?")
				.AddChoices(RepairStrategy.Stash, RepairStrategy.Reset, RepairStrategy.Reinit)
				.UseConverter(strategy => strategy switch
				{
					RepairStrategy.Stash => "stash — save local changes, then restore the parent-recorded commit",
					RepairStrategy.Reset => "reset — discard local changes and restore the parent-recorded HEAD",
					RepairStrategy.Reinit => "reinit — remove and clone the submodule again at a tagged version",
					_ => strategy.ToString()
				}));
	}

	private bool Confirm(Settings settings, RepairStrategy strategy)
	{
		if (settings.Yes || strategy == RepairStrategy.Stash)
		{
			return true;
		}

		if (!_console.Profile.Capabilities.Interactive)
		{
			throw new BuildCliException(
				$"Refusing to run a destructive '{strategy.ToString().ToLowerInvariant()}' repair without --yes.");
		}

		var message = strategy == RepairStrategy.Reset
			? "Discard local Build submodule changes and restore the parent-recorded HEAD?"
			: "Delete and re-initialize the Build submodule at a tagged version?";

		return _console.Confirm(message, defaultValue: false);
	}

	/// <summary>
	/// Settings for <see cref="RepairCommand"/>.
	/// </summary>
	public sealed class Settings : RepositorySettings
	{
		[CommandOption("-s|--strategy <STRATEGY>")]
		[Description("Repair strategy: stash, reset (alias: head), or reinit.")]
		public string? Strategy { get; init; }

		[CommandOption("-t|--tag <REF>")]
		[Description("A tag, branch, or commit to check out after repair. Defaults to the parent HEAD for stash/reset, or the latest tag for reinit.")]
		public string? Tag { get; init; }

		[CommandOption("-y|--yes")]
		[Description("Do not prompt before a destructive reset or reinit.")]
		public bool Yes { get; init; }
	}
}
