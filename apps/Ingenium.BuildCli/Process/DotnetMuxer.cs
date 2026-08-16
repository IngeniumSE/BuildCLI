// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

namespace Ingenium.BuildCli.Execution;

/// <summary>
/// Locates a .NET SDK muxer, including the user-local install used by the install scripts.
/// </summary>
public static class DotnetMuxer
{
	/// <summary>
	/// The muxer file name for the current OS.
	/// </summary>
	public static string FileName => OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";

	/// <summary>
	/// Returns the user-local muxer path used by <c>scripts/install.sh</c>.
	/// </summary>
	public static string UserInstallPath
	{
		get
		{
			var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			return Path.Combine(home, ".dotnet", FileName);
		}
	}

	/// <summary>
	/// Returns an SDK muxer path, or <c>null</c> when none can be found.
	/// </summary>
	public static string? Resolve()
	{
		return Resolve(Candidates());
	}

	/// <summary>
	/// Returns the first candidate that looks like an SDK install.
	/// </summary>
	public static string? Resolve(IEnumerable<string> candidates)
	{
		ArgumentNullException.ThrowIfNull(candidates);

		foreach (var candidate in candidates)
		{
			if (string.IsNullOrWhiteSpace(candidate))
			{
				continue;
			}

			var resolved = ResolveExistingPath(candidate);
			if (resolved is not null && IsSdkInstall(resolved))
			{
				return resolved;
			}
		}

		return null;
	}

	/// <summary>
	/// Returns candidate muxer locations, preferring PATH and then the installer directory.
	/// </summary>
	public static IEnumerable<string> Candidates()
	{
		foreach (var path in FindOnPath())
		{
			yield return path;
		}

		yield return UserInstallPath;

		var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
		if (!string.IsNullOrWhiteSpace(dotnetRoot))
		{
			yield return Path.Combine(dotnetRoot, FileName);
		}

		if (OperatingSystem.IsWindows())
		{
			var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			if (!string.IsNullOrWhiteSpace(programFiles))
			{
				yield return Path.Combine(programFiles, "dotnet", FileName);
			}

			var programFilesX86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
			if (!string.IsNullOrWhiteSpace(programFilesX86))
			{
				yield return Path.Combine(programFilesX86, "dotnet", FileName);
			}

			yield break;
		}

		yield return "/usr/share/dotnet/dotnet";
		yield return "/usr/local/share/dotnet/dotnet";
		yield return "/usr/lib/dotnet/dotnet";
	}

	/// <summary>
	/// Returns <c>true</c> when <paramref name="muxerPath"/> sits next to an <c>sdk</c> directory.
	/// </summary>
	public static bool IsSdkInstall(string muxerPath)
	{
		if (string.IsNullOrWhiteSpace(muxerPath) || !File.Exists(muxerPath))
		{
			return false;
		}

		var directory = Path.GetDirectoryName(ResolveExistingPath(muxerPath) ?? muxerPath);
		if (string.IsNullOrEmpty(directory))
		{
			return false;
		}

		var sdk = Path.Combine(directory, "sdk");
		return Directory.Exists(sdk) && Directory.EnumerateDirectories(sdk).Any();
	}

	/// <summary>
	/// Returns <c>true</c> when <paramref name="fileName"/> refers to the dotnet muxer.
	/// </summary>
	public static bool IsMuxerName(string fileName)
	{
		if (string.IsNullOrWhiteSpace(fileName))
		{
			return false;
		}

		var name = Path.GetFileName(fileName);
		return name.Equals("dotnet", StringComparison.OrdinalIgnoreCase)
			|| name.Equals("dotnet.exe", StringComparison.OrdinalIgnoreCase);
	}

	private static IEnumerable<string> FindOnPath()
	{
		var path = Environment.GetEnvironmentVariable("PATH");
		if (string.IsNullOrWhiteSpace(path))
		{
			yield break;
		}

		foreach (var part in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
		{
			string candidate;
			try
			{
				candidate = Path.Combine(part, FileName);
			}
			catch (ArgumentException)
			{
				continue;
			}

			if (File.Exists(candidate))
			{
				yield return candidate;
			}
		}
	}

	private static string? ResolveExistingPath(string path)
	{
		try
		{
			var full = Path.GetFullPath(path);
			if (!File.Exists(full))
			{
				return null;
			}

			var target = File.ResolveLinkTarget(full, returnFinalTarget: true);
			return target?.FullName ?? full;
		}
		catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException or NotSupportedException)
		{
			return File.Exists(path) ? path : null;
		}
	}
}
