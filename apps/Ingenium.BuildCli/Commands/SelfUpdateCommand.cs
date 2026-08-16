// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System.ComponentModel;

using Ingenium.BuildCli.Git;
using Ingenium.BuildCli.Rendering;
using Ingenium.BuildCli.SelfUpdate;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Ingenium.BuildCli.Commands;

/// <summary>
/// Republishes and reinstalls the <c>bld</c> CLI itself.
/// </summary>
public sealed class SelfUpdateCommand : AsyncCommand<SelfUpdateCommand.Settings>
{
	private readonly IAnsiConsole _console;
	private readonly ISelfUpdateService _service;
	private readonly IGitTrace _trace;

	/// <summary>
	/// Initializes a new instance of the <see cref="SelfUpdateCommand"/> class.
	/// </summary>
	public SelfUpdateCommand(IAnsiConsole console, ISelfUpdateService service, IGitTrace trace)
	{
		_console = console;
		_service = service;
		_trace = trace;
	}

	/// <inheritdoc />
	public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
	{
		_trace.Enabled = settings.Verbose;
		ConsoleWriter.WriteHeader(_console, "self-update");

		var request = settings.ToRequest();
		var result = settings.Verbose
			? await _service.UpdateAsync(request)
			: await _console.Status()
				.Spinner(Spinner.Known.Dots)
				.StartAsync("Updating the installed bld CLI...", async _ => await _service.UpdateAsync(request));

		_console.MarkupLine("[green]Updated[/] the installed bld CLI.");
		_console.WriteLine();
		ConsoleWriter.WriteSelfUpdate(_console, result);
		return ExitCodes.Success;
	}

	/// <summary>
	/// Settings for <see cref="SelfUpdateCommand"/>.
	/// </summary>
	public sealed class Settings : CommandSettings
	{
		[CommandOption("--ref <REF>")]
		[Description("Git branch or tag of BuildCLI to install. Defaults to main.")]
		public string? Ref { get; init; }

		[CommandOption("--source <PATH>")]
		[Description("Existing BuildCLI checkout to publish instead of cloning.")]
		public string? Source { get; init; }

		[CommandOption("--url <URL>")]
		[Description("BuildCLI git URL used when cloning. Defaults to the public HTTPS repository.")]
		public string? Url { get; init; }

		[CommandOption("--install-dir <PATH>")]
		[Description("Directory that receives the published bld binary.")]
		public string? InstallDirectory { get; init; }

		[CommandOption("--bin-dir <PATH>")]
		[Description("Directory that receives the bld symlink on macOS and Linux.")]
		public string? BinDirectory { get; init; }

		[CommandOption("--framework-dependent")]
		[Description("Publish a framework-dependent binary instead of a self-contained single file.")]
		public bool FrameworkDependent { get; init; }

		[CommandOption("--verbose")]
		[Description("Write the git and dotnet commands that are executed.")]
		public bool Verbose { get; init; }

		/// <summary>
		/// Creates a service request from these settings.
		/// </summary>
		public SelfUpdateRequest ToRequest()
		{
			return new SelfUpdateRequest
			{
				Ref = Ref,
				Source = Source,
				Url = Url,
				FrameworkDependent = FrameworkDependent,
				Verbose = Verbose,
				InstallDirectory = InstallDirectory,
				BinDirectory = BinDirectory
			};
		}
	}
}
