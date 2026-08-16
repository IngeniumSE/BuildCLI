// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using Spectre.Console;

namespace Ingenium.BuildCli.Git;

/// <summary>
/// Writes git diagnostics to the console when verbose mode is enabled.
/// </summary>
public sealed class AnsiConsoleGitTrace : IGitTrace
{
	private readonly IAnsiConsole _console;

	/// <summary>
	/// Initializes a new instance of the <see cref="AnsiConsoleGitTrace"/> class.
	/// </summary>
	public AnsiConsoleGitTrace(IAnsiConsole console)
	{
		_console = console;
	}

	/// <inheritdoc />
	public bool Enabled { get; set; }

	/// <inheritdoc />
	public void Write(string message)
	{
		if (!Enabled || string.IsNullOrWhiteSpace(message))
		{
			return;
		}

		_console.MarkupLine($"[grey]{Markup.Escape(message)}[/]");
	}
}
