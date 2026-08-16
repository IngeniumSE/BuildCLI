// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using Ingenium.BuildCli.Extensions;
using Ingenium.BuildCli.SelfUpdate;
using Ingenium.BuildCli.Submodule;

using Spectre.Console;

namespace Ingenium.BuildCli.Rendering;

/// <summary>
/// Shared Spectre.Console layout used by CLI commands.
/// </summary>
public static class ConsoleWriter
{
	/// <summary>
	/// Writes the standard command header.
	/// </summary>
	public static void WriteHeader(IAnsiConsole console, string title)
	{
		console.Write(new Rule($"[teal]Ingenium Build CLI[/] {Markup.Escape(title)}").LeftJustified());
		console.WriteLine();
	}

	/// <summary>
	/// Writes a successful init or update summary.
	/// </summary>
	public static void WriteChange(IAnsiConsole console, BuildSubmoduleChange change)
	{
		var table = new Table()
			.Border(TableBorder.Rounded)
			.HideHeaders()
			.AddColumn(new TableColumn("Key").PadRight(2))
			.AddColumn("Value");

		table.AddRow("[grey]Path[/]", Markup.Escape(change.RelativePath));
		table.AddRow("[grey]URL[/]", Markup.Escape(change.Url));
		table.AddRow("[grey]Ref[/]", Markup.Escape(change.CheckedOutRef));
		table.AddRow("[grey]Commit[/]", Markup.Escape(ShortSha(change.Commit)));

		if (!string.IsNullOrEmpty(change.PreviousCommit) &&
			!string.Equals(change.PreviousCommit, change.Commit, StringComparison.OrdinalIgnoreCase))
		{
			table.AddRow("[grey]Previous[/]", Markup.Escape(ShortSha(change.PreviousCommit)));
		}

		console.Write(table);
		console.WriteLine();
		console.MarkupLine("[grey]The submodule change is staged. Commit it in the parent repository when ready.[/]");
	}

	/// <summary>
	/// Writes the current submodule status as a table.
	/// </summary>
	public static void WriteStatus(IAnsiConsole console, BuildSubmoduleStatus status)
	{
		var table = new Table()
			.Border(TableBorder.Rounded)
			.AddColumn("Property")
			.AddColumn("Value");

		table.AddRow("Repository", Markup.Escape(status.RepositoryRoot));
		table.AddRow("Submodule", Markup.Escape(status.RelativePath ?? BuildRepositoryUrls.DefaultPath));
		table.AddRow("URL", Markup.Escape(status.Url ?? "-"));
		table.AddRow("Registered", status.IsRegistered ? "[green]yes[/]" : "[yellow]no[/]");
		table.AddRow("Initialized", status.IsInitialized ? "[green]yes[/]" : "[yellow]no[/]");
		table.AddRow("Commit", Markup.Escape(status.Commit is null ? "-" : ShortSha(status.Commit)));
		table.AddRow("Current tag", status.CurrentTags.Count == 0 ? "-" : Markup.Escape(string.Join(", ", status.CurrentTags)));
		table.AddRow("Latest tag", Markup.Escape(status.LatestTag ?? "-"));
		table.AddRow("Up to date", FormatUpToDate(status));

		console.Write(table);
	}

	/// <summary>
	/// Writes a repair summary.
	/// </summary>
	public static void WriteRepair(IAnsiConsole console, RepairResult result)
	{
		var table = new Table()
			.Border(TableBorder.Rounded)
			.HideHeaders()
			.AddColumn(new TableColumn("Key").PadRight(2))
			.AddColumn("Value");

		table.AddRow("[grey]Strategy[/]", Markup.Escape(result.Strategy.ToString().ToLowerInvariant()));
		table.AddRow("[grey]Path[/]", Markup.Escape(result.Change.RelativePath));
		table.AddRow("[grey]Ref[/]", Markup.Escape(result.Change.CheckedOutRef));
		table.AddRow("[grey]Commit[/]", Markup.Escape(ShortSha(result.Change.Commit)));
		if (!string.IsNullOrEmpty(result.StashRef))
		{
			table.AddRow("[grey]Stash[/]", Markup.Escape(result.StashRef));
		}

		console.Write(table);
		if (result.Actions.Count > 0)
		{
			console.WriteLine();
			foreach (var action in result.Actions)
			{
				console.MarkupLine($"[grey]•[/] {Markup.Escape(action)}");
			}
		}

		console.WriteLine();
		console.MarkupLine("[grey]The submodule change is staged. Commit it in the parent repository when ready.[/]");
	}

	/// <summary>
	/// Writes the files created for a build extension.
	/// </summary>
	public static void WriteExtension(IAnsiConsole console, BuildExtensionScaffold scaffold)
	{
		var table = new Table()
			.Border(TableBorder.Rounded)
			.HideHeaders()
			.AddColumn(new TableColumn("Key").PadRight(2))
			.AddColumn("Value");

		table.AddRow("[grey]Project[/]", Markup.Escape(scaffold.ProjectName));
		table.AddRow("[grey]Path[/]", Markup.Escape(Path.GetRelativePath(scaffold.RepositoryRoot, scaffold.ProjectDirectory)));
		console.Write(table);

		if (scaffold.WrittenFiles.Count > 0)
		{
			console.WriteLine();
			console.MarkupLine("[grey]Files:[/]");
			foreach (var file in scaffold.WrittenFiles)
			{
				console.MarkupLine($"[grey]•[/] {Markup.Escape(Path.GetRelativePath(scaffold.RepositoryRoot, file))}");
			}
		}

		console.WriteLine();
		console.MarkupLine("[grey]The Build host imports every project under build-extensions/ automatically.[/]");
	}

	/// <summary>
	/// Writes a successful self-update summary.
	/// </summary>
	public static void WriteSelfUpdate(IAnsiConsole console, SelfUpdateResult result)
	{
		var table = new Table()
			.Border(TableBorder.Rounded)
			.HideHeaders()
			.AddColumn(new TableColumn("Key").PadRight(2))
			.AddColumn("Value");

		table.AddRow("[grey]Version[/]", Markup.Escape(result.Version));
		table.AddRow("[grey]Runtime[/]", Markup.Escape(result.RuntimeIdentifier));
		table.AddRow("[grey]Ref[/]", Markup.Escape(result.Ref));
		table.AddRow("[grey]Installed[/]", Markup.Escape(result.ExecutablePath));
		if (!string.IsNullOrEmpty(result.BinLink))
		{
			table.AddRow("[grey]Link[/]", Markup.Escape(result.BinLink));
		}

		console.Write(table);

		if (!result.BinDirectoryOnPath)
		{
			var hint = OperatingSystem.IsWindows()
				? Path.GetDirectoryName(result.ExecutablePath) ?? result.ExecutablePath
				: Path.GetDirectoryName(result.BinLink ?? result.ExecutablePath) ?? result.ExecutablePath;
			console.WriteLine();
			console.MarkupLine($"[yellow]bld may not be on PATH.[/] Add {Markup.Escape(hint)} to PATH and reopen the terminal.");
		}
	}

	/// <summary>
	/// Writes advertised remote tags.
	/// </summary>
	public static void WriteTags(IAnsiConsole console, IReadOnlyList<RemoteTag> tags, string? currentCommit)
	{
		if (tags.Count == 0)
		{
			console.MarkupLine("[yellow]No tags were found on the Build remote.[/]");
			return;
		}

		var latest = TagSelector.SelectLatest(tags);
		var table = new Table()
			.Border(TableBorder.Rounded)
			.AddColumn("Tag")
			.AddColumn("Commit")
			.AddColumn("Notes");

		foreach (var tag in tags.OrderByDescending(item => item.Name, StringComparer.OrdinalIgnoreCase))
		{
			var notes = new List<string>();
			if (latest is not null && tag.Name.Equals(latest.Name, StringComparison.Ordinal))
			{
				notes.Add("[green]latest[/]");
			}

			if (!string.IsNullOrEmpty(currentCommit) &&
				(tag.Commit.StartsWith(currentCommit, StringComparison.OrdinalIgnoreCase)
					|| currentCommit.StartsWith(tag.Commit, StringComparison.OrdinalIgnoreCase)))
			{
				notes.Add("[teal]current[/]");
			}

			table.AddRow(
				Markup.Escape(tag.Name),
				Markup.Escape(ShortSha(tag.Commit)),
				string.Join(" ", notes));
		}

		console.Write(table);
	}

	/// <summary>
	/// Writes a user-facing error.
	/// </summary>
	public static void WriteError(IAnsiConsole console, Exception exception)
	{
		if (exception is BuildCliException)
		{
			console.MarkupLine($"[red]Error:[/] {Markup.Escape(exception.Message)}");
			return;
		}

		console.WriteException(exception, ExceptionFormats.ShortenPaths | ExceptionFormats.ShortenTypes);
	}

	/// <summary>
	/// Returns a shortened SHA for display.
	/// </summary>
	public static string ShortSha(string sha)
	{
		return sha.Length <= 12 ? sha : sha[..12];
	}

	private static string FormatUpToDate(BuildSubmoduleStatus status)
	{
		if (!status.IsRegistered)
		{
			return "[yellow]not added[/]";
		}

		if (!status.IsInitialized)
		{
			return "[yellow]not initialized[/]";
		}

		if (status.LatestTag is null)
		{
			return "[grey]unknown[/]";
		}

		return status.IsLatest ? "[green]yes[/]" : "[yellow]no[/]";
	}
}
