// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using Ingenium.BuildCli.SelfUpdate;

namespace Ingenium.BuildCli.Tests;

public sealed class SelfUpdatePathsTests
{
	[Fact]
	public void GetInstallDirectory_UsesOverride()
	{
		var expected = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "custom-bld"));
		Assert.Equal(expected, SelfUpdatePaths.GetInstallDirectory(expected));
	}

	[Fact]
	public void GetBinDirectory_UsesOverride()
	{
		var expected = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "custom-bin"));
		Assert.Equal(expected, SelfUpdatePaths.GetBinDirectory(expected));
	}

	[Fact]
	public void GetRepositoryUrl_UsesOverride()
	{
		Assert.Equal(
			"https://example.test/BuildCLI.git",
			SelfUpdatePaths.GetRepositoryUrl("https://example.test/BuildCLI.git"));
	}

	[Fact]
	public void GetRuntimeIdentifier_IsWellFormed()
	{
		var rid = SelfUpdatePaths.GetRuntimeIdentifier();
		Assert.Contains('-', rid);
		Assert.True(
			rid.StartsWith("linux-", StringComparison.Ordinal) ||
			rid.StartsWith("osx-", StringComparison.Ordinal) ||
			rid.StartsWith("win-", StringComparison.Ordinal),
			rid);
	}

	[Fact]
	public void GetProjectPath_UsesIngeniumLayout()
	{
		var root = Path.Combine(Path.GetTempPath(), "buildcli");
		Assert.Equal(
			Path.Combine(root, "apps", "Ingenium.BuildCli", "Ingenium.BuildCli.csproj"),
			SelfUpdatePaths.GetProjectPath(root));
	}

	[Fact]
	public void GetExecutableFileName_MatchesCurrentOs()
	{
		var name = SelfUpdatePaths.GetExecutableFileName();
		Assert.Equal(OperatingSystem.IsWindows() ? "bld.exe" : "bld", name);
	}
}
