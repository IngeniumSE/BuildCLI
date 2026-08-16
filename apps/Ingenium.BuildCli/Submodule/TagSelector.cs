// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System.Text.RegularExpressions;

namespace Ingenium.BuildCli.Submodule;

/// <summary>
/// Chooses the latest tag from a remote tag list.
/// </summary>
public static class TagSelector
{
	private static readonly Regex SemVerRegex = new(
		@"^v?(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?<pre>[-+][0-9A-Za-z.-]+)?$",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	/// <summary>
	/// Returns the highest semantic-version tag, or the first tag when none are semver.
	/// </summary>
	public static RemoteTag? SelectLatest(IReadOnlyList<RemoteTag> tags)
	{
		if (tags.Count == 0)
		{
			return null;
		}

		var ranked = tags
			.Select(tag => (Tag: tag, Version: TryParse(tag.Name)))
			.ToList();

		var semver = ranked
			.Where(item => item.Version is not null)
			.OrderByDescending(item => item.Version)
			.ToList();

		if (semver.Count > 0)
		{
			return semver[0].Tag;
		}

		return tags[0];
	}

	/// <summary>
	/// Finds a tag by name, accepting an optional leading <c>v</c>.
	/// </summary>
	public static RemoteTag? Find(IReadOnlyList<RemoteTag> tags, string name)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name);

		var exact = tags.FirstOrDefault(tag => tag.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
		if (exact is not null)
		{
			return exact;
		}

		var trimmed = name.StartsWith('v') || name.StartsWith('V') ? name[1..] : name;
		return tags.FirstOrDefault(tag =>
		{
			var candidate = tag.Name.StartsWith('v') || tag.Name.StartsWith('V') ? tag.Name[1..] : tag.Name;
			return candidate.Equals(trimmed, StringComparison.OrdinalIgnoreCase);
		});
	}

	/// <summary>
	/// Parses git <c>ls-remote --tags</c> output into unique tag names.
	/// </summary>
	public static IReadOnlyList<RemoteTag> ParseLsRemote(string output)
	{
		ArgumentNullException.ThrowIfNull(output);

		var tags = new Dictionary<string, RemoteTag>(StringComparer.Ordinal);
		using var reader = new StringReader(output);
		while (reader.ReadLine() is { } line)
		{
			if (string.IsNullOrWhiteSpace(line))
			{
				continue;
			}

			var parts = line.Split('\t', 2, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length != 2)
			{
				parts = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			}

			if (parts.Length != 2)
			{
				continue;
			}

			var sha = parts[0].Trim();
			var refName = parts[1].Trim();
			var peeled = refName.EndsWith("^{}", StringComparison.Ordinal);
			if (peeled)
			{
				refName = refName[..^3];
			}

			const string prefix = "refs/tags/";
			if (!refName.StartsWith(prefix, StringComparison.Ordinal))
			{
				continue;
			}

			var name = refName[prefix.Length..];
			if (peeled || !tags.ContainsKey(name))
			{
				tags[name] = new RemoteTag(name, sha);
			}
		}

		return tags.Values.ToList();
	}

	private static Version? TryParse(string name)
	{
		var match = SemVerRegex.Match(name);
		if (!match.Success)
		{
			return null;
		}

		var major = int.Parse(match.Groups["major"].Value);
		var minor = int.Parse(match.Groups["minor"].Value);
		var patch = int.Parse(match.Groups["patch"].Value);

		// Prefer stable releases over pre-releases of the same version.
		var revision = match.Groups["pre"].Success ? 0 : 1;
		return new Version(major, minor, patch, revision);
	}
}
