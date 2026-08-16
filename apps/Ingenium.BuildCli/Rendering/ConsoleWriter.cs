// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

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
