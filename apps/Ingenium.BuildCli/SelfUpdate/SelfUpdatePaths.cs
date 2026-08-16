// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System.Runtime.InteropServices;

namespace Ingenium.BuildCli.SelfUpdate;

/// <summary>
/// Resolves install locations and publish settings used by <c>bld self-update</c>.
/// </summary>
public static class SelfUpdatePaths
{
	/// <summary>
	/// The default HTTPS clone URL for this CLI repository.
	/// </summary>
	public const string DefaultRepositoryUrl = "https://github.com/IngeniumSE/BuildCLI.git";

	/// <summary>
	/// The default git ref installed by <c>self-update</c>.
	/// </summary>
	public const string DefaultRef = "main";

	/// <summary>
	/// Returns the BuildCLI clone URL, honoring <c>BUILDCLI_REPO_URL</c>.
	/// </summary>
	public static string GetRepositoryUrl(string? overrideUrl = null)
	{
		if (!string.IsNullOrWhiteSpace(overrideUrl))
		{
			return overrideUrl.Trim();
		}

		var configured = Environment.GetEnvironmentVariable("BUILDCLI_REPO_URL");
		return string.IsNullOrWhiteSpace(configured) ? DefaultRepositoryUrl : configured.Trim();
	}

	/// <summary>
	/// Returns the directory that holds the published <c>bld</c> binary.
	/// </summary>
	public static string GetInstallDirectory(string? overridePath = null)
	{
		if (!string.IsNullOrWhiteSpace(overridePath))
		{
			return Path.GetFullPath(overridePath);
		}

		var configured = Environment.GetEnvironmentVariable("BLD_INSTALL_DIR");
		if (string.IsNullOrWhiteSpace(configured))
		{
			configured = Environment.GetEnvironmentVariable("BUILDCLI_INSTALL_DIR");
		}

		if (!string.IsNullOrWhiteSpace(configured))
		{
			return Path.GetFullPath(configured);
		}

		if (OperatingSystem.IsWindows())
		{
			var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			return Path.Combine(localAppData, "Ingenium", "bld");
		}

		var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		return Path.Combine(home, ".local", "share", "ingenium", "bld");
	}

	/// <summary>
	/// Returns the directory that should contain a <c>bld</c> symlink on Unix.
	/// </summary>
	public static string GetBinDirectory(string? overridePath = null)
	{
		if (!string.IsNullOrWhiteSpace(overridePath))
		{
			return Path.GetFullPath(overridePath);
		}

		var configured = Environment.GetEnvironmentVariable("BUILDCLI_BIN_DIR");
		if (!string.IsNullOrWhiteSpace(configured))
		{
			return Path.GetFullPath(configured);
		}

		var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		return Path.Combine(home, ".local", "bin");
	}

	/// <summary>
	/// Returns the published executable file name for the current OS.
	/// </summary>
	public static string GetExecutableFileName()
	{
		return OperatingSystem.IsWindows() ? $"{CliInfo.Name}.exe" : CliInfo.Name;
	}

	/// <summary>
	/// Returns the .NET runtime identifier used to publish the current machine.
	/// </summary>
	public static string GetRuntimeIdentifier()
	{
		var rid = RuntimeInformation.RuntimeIdentifier;
		if (!string.IsNullOrWhiteSpace(rid) && rid.Contains('-', StringComparison.Ordinal))
		{
			return rid;
		}

		var os = OperatingSystem.IsWindows()
			? "win"
			: OperatingSystem.IsMacOS()
				? "osx"
				: OperatingSystem.IsLinux()
					? "linux"
					: throw new BuildCliException("Unsupported operating system for self-update.");

		var arch = RuntimeInformation.OSArchitecture switch
		{
			Architecture.X64 => "x64",
			Architecture.Arm64 => "arm64",
			_ => throw new BuildCliException($"Unsupported architecture: {RuntimeInformation.OSArchitecture}.")
		};

		return $"{os}-{arch}";
	}

	/// <summary>
	/// Returns the CLI project path inside a BuildCLI checkout.
	/// </summary>
	public static string GetProjectPath(string sourceRoot)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(sourceRoot);
		return Path.Combine(sourceRoot, "apps", "Ingenium.BuildCli", "Ingenium.BuildCli.csproj");
	}

	/// <summary>
	/// Returns <c>true</c> when <paramref name="directory"/> appears on <c>PATH</c>.
	/// </summary>
	public static bool IsDirectoryOnPath(string directory)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(directory);

		var path = Environment.GetEnvironmentVariable("PATH");
		if (string.IsNullOrWhiteSpace(path))
		{
			return false;
		}

		var full = Path.GetFullPath(directory);
		var comparison = OperatingSystem.IsWindows()
			? StringComparison.OrdinalIgnoreCase
			: StringComparison.Ordinal;

		foreach (var part in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
		{
			try
			{
				if (string.Equals(Path.GetFullPath(part), full, comparison))
				{
					return true;
				}
			}
			catch (ArgumentException)
			{
			}
		}

		return false;
	}
}
