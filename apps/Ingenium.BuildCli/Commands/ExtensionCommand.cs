// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System.ComponentModel;

using Ingenium.BuildCli.Extensions;
using Ingenium.BuildCli.Git;
using Ingenium.BuildCli.Rendering;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Ingenium.BuildCli.Commands;

/// <summary>
/// Scaffolds a Cake build-extension project for the current repository.
/// </summary>
public sealed class ExtensionCommand : AsyncCommand<ExtensionCommand.Settings>
{
	private readonly IAnsiConsole _console;
	private readonly IBuildExtensionService _extensions;
	private readonly IGitTrace _trace;

	/// <summary>
	/// Initializes a new instance of the <see cref="ExtensionCommand"/> class.
	/// </summary>
	public ExtensionCommand(IAnsiConsole console, IBuildExtensionService extensions, IGitTrace trace)
	{
		_console = console;
		_extensions = extensions;
		_trace = trace;
	}

	/// <inheritdoc />
	public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
	{
		_trace.Enabled = settings.Verbose;
		ConsoleWriter.WriteHeader(_console, "extension");

		var scaffold = await _extensions.CreateAsync(new BuildExtensionRequest
		{
			RepositoryPath = string.IsNullOrWhiteSpace(settings.Path) ? Environment.CurrentDirectory : settings.Path,
			Name = settings.Name,
			SubmodulePath = settings.SubmodulePath,
			Force = settings.Force
		});

		_console.MarkupLine($"[green]Created[/] build extension [bold]{Markup.Escape(scaffold.ProjectName)}[/].");
		_console.WriteLine();
		ConsoleWriter.WriteExtension(_console, scaffold);
		return ExitCodes.Success;
	}

	/// <summary>
	/// Settings for <see cref="ExtensionCommand"/>.
	/// </summary>
	public sealed class Settings : RepositorySettings
	{
		[CommandArgument(0, "[NAME]")]
		[Description("Extension name. Defaults to the repository folder name, with a BuildExtensions suffix.")]
		public string? Name { get; init; }

		[CommandOption("-f|--force")]
		[Description("Overwrite an existing extension project.")]
		public bool Force { get; init; }
	}
}
