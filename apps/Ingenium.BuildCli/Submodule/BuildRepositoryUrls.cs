// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

namespace Ingenium.BuildCli.Submodule;

/// <summary>
/// Canonical locations of the Ingenium Build repository.
/// </summary>
public static class BuildRepositoryUrls
{
	/// <summary>
	/// The default SSH clone URL for the Build submodule.
	/// </summary>
	public const string Ssh = "git@github.com:IngeniumSE/Build.git";

	/// <summary>
	/// The HTTPS clone URL for the Build submodule.
	/// </summary>
	public const string Https = "https://github.com/IngeniumSE/Build.git";

	/// <summary>
	/// The default relative path used when adding the submodule.
	/// </summary>
	public const string DefaultPath = "build";

	/// <summary>
	/// Returns the default Build URL for the requested transport.
	/// </summary>
	public static string GetDefault(bool useHttps)
	{
		return useHttps ? Https : Ssh;
	}

	/// <summary>
	/// Returns <c>true</c> when <paramref name="url"/> points at the Ingenium Build repository.
	/// </summary>
	public static bool IsBuildRepository(string? url)
	{
		if (string.IsNullOrWhiteSpace(url))
		{
			return false;
		}

		var normalized = Normalize(url);
		return normalized.Contains("ingeniumse/build", StringComparison.Ordinal);
	}

	/// <summary>
	/// Chooses SSH or HTTPS based on an existing parent-repo remote, unless an override is supplied.
	/// </summary>
	public static string InferFromParentRemote(string? parentRemoteUrl, bool? useHttps)
	{
		if (useHttps == true)
		{
			return Https;
		}

		if (useHttps == false)
		{
			return Ssh;
		}

		if (!string.IsNullOrWhiteSpace(parentRemoteUrl) &&
			parentRemoteUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
		{
			return Https;
		}

		return Ssh;
	}

	/// <summary>
	/// Normalizes a git URL for comparison.
	/// </summary>
	public static string Normalize(string url)
	{
		var value = url.Trim();
		if (value.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
		{
			value = value[..^4];
		}

		value = value.Replace('\\', '/');
		if (value.StartsWith("git@", StringComparison.OrdinalIgnoreCase))
		{
			var colon = value.IndexOf(':');
			if (colon > 0)
			{
				value = value[(colon + 1)..];
			}
		}

		const string httpsGithub = "https://github.com/";
		if (value.StartsWith(httpsGithub, StringComparison.OrdinalIgnoreCase))
		{
			value = value[httpsGithub.Length..];
		}

		return value.Trim('/').ToLowerInvariant();
	}
}
