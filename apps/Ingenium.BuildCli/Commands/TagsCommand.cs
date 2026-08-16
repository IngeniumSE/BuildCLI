// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using Ingenium.BuildCli.Git;
using Ingenium.BuildCli.Rendering;
using Ingenium.BuildCli.Submodule;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Ingenium.BuildCli.Commands;

/// <summary>
/// Lists tags available on the Build remote.
/// </summary>
public sealed class TagsCommand : AsyncCommand<TagsCommand.Settings>
{
	private readonly IAnsiConsole _console;
	private readonly IBuildSubmoduleService _service;
	private readonly IGitTrace _trace;

	/// <summary>
	/// Initializes a new instance of the <see cref="TagsCommand"/> class.
	/// </summary>
	public TagsCommand(IAnsiConsole console, IBuildSubmoduleService service, IGitTrace trace)
	{
		_console = console;
		_service = service;
		_trace = trace;
	}

	/// <inheritdoc />
	public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
	{
		_trace.Enabled = settings.Verbose;
		ConsoleWriter.WriteHeader(_console, "tags");

		var request = settings.ToRequest();
		var tags = await _console.Status()
			.Spinner(Spinner.Known.Dots)
			.StartAsync("Listing Build tags...", async _ =>
				await _service.ListTagsAsync(request));

		string? currentCommit = null;
		try
		{
			var status = await _service.GetStatusAsync(request);
			currentCommit = status.Commit;
		}
		catch (BuildCliException)
		{
			// Listing tags should still work outside a git repository.
		}

		ConsoleWriter.WriteTags(_console, tags, currentCommit);
		return ExitCodes.Success;
	}

	/// <summary>
	/// Settings for <see cref="TagsCommand"/>.
	/// </summary>
	public sealed class Settings : RepositorySettings
	{
	}
}
