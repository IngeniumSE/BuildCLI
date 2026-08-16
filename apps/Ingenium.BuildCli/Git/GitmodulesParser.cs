// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System.Text.RegularExpressions;

namespace Ingenium.BuildCli.Git;

/// <summary>
/// Parses a <c>.gitmodules</c> file into submodule entries.
/// </summary>
public static class GitmodulesParser
{
	private static readonly Regex SectionRegex = new(
		@"^\[submodule\s+""(?<name>[^""]+)""\]\s*$",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private static readonly Regex PropertyRegex = new(
		@"^\s*(?<key>[A-Za-z0-9_-]+)\s*=\s*(?<value>.+?)\s*$",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	/// <summary>
	/// Parses the supplied <c>.gitmodules</c> text.
	/// </summary>
	public static IReadOnlyList<GitmoduleEntry> Parse(string contents)
	{
		ArgumentNullException.ThrowIfNull(contents);

		var entries = new List<GitmoduleEntry>();
		string? name = null;
		string? path = null;
		string? url = null;
		string? branch = null;

		void Flush()
		{
			if (name is null)
			{
				return;
			}

			entries.Add(new GitmoduleEntry(name, path ?? name, url, branch));
			name = null;
			path = null;
			url = null;
			branch = null;
		}

		using var reader = new StringReader(contents);
		while (reader.ReadLine() is { } line)
		{
			if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
			{
				continue;
			}

			var section = SectionRegex.Match(line);
			if (section.Success)
			{
				Flush();
				name = section.Groups["name"].Value;
				continue;
			}

			if (name is null)
			{
				continue;
			}

			var property = PropertyRegex.Match(line);
			if (!property.Success)
			{
				continue;
			}

			var key = property.Groups["key"].Value;
			var value = property.Groups["value"].Value;
			if (key.Equals("path", StringComparison.OrdinalIgnoreCase))
			{
				path = value;
			}
			else if (key.Equals("url", StringComparison.OrdinalIgnoreCase))
			{
				url = value;
			}
			else if (key.Equals("branch", StringComparison.OrdinalIgnoreCase))
			{
				branch = value;
			}
		}

		Flush();
		return entries;
	}

	/// <summary>
	/// Parses a <c>.gitmodules</c> file from disk. Returns an empty list when the file is missing.
	/// </summary>
	public static IReadOnlyList<GitmoduleEntry> ParseFile(string gitmodulesPath)
	{
		if (!File.Exists(gitmodulesPath))
		{
			return [];
		}

		return Parse(File.ReadAllText(gitmodulesPath));
	}
}
