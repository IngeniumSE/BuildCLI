// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using Ingenium.BuildCli.Execution;

namespace Ingenium.BuildCli.Tests;

public sealed class DotnetMuxerTests
{
	[Fact]
	public void UserInstallPath_IsHomeDotnet()
	{
		var expected = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
			".dotnet",
			DotnetMuxer.FileName);

		Assert.Equal(expected, DotnetMuxer.UserInstallPath);
	}

	[Fact]
	public void Candidates_IncludeUserInstall()
	{
		Assert.Contains(DotnetMuxer.UserInstallPath, DotnetMuxer.Candidates());
	}

	[Fact]
	public void IsMuxerName_RecognizesDotnet()
	{
		Assert.True(DotnetMuxer.IsMuxerName("dotnet"));
		Assert.True(DotnetMuxer.IsMuxerName("dotnet.exe"));
		Assert.True(DotnetMuxer.IsMuxerName(Path.Combine("opt", "dotnet")));
		Assert.False(DotnetMuxer.IsMuxerName("git"));
	}

	[Fact]
	public void IsSdkInstall_RequiresSdkDirectory()
	{
		var root = Path.Combine(Path.GetTempPath(), "buildcli-dotnet-muxer", Guid.NewGuid().ToString("N"));
		var muxer = Path.Combine(root, DotnetMuxer.FileName);
		Directory.CreateDirectory(root);
		File.WriteAllText(muxer, "dotnet");

		try
		{
			Assert.False(DotnetMuxer.IsSdkInstall(muxer));
			Directory.CreateDirectory(Path.Combine(root, "sdk", "8.0.100"));
			Assert.True(DotnetMuxer.IsSdkInstall(muxer));
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[Fact]
	public void Resolve_PrefersFirstSdkCandidate()
	{
		var root = Path.Combine(Path.GetTempPath(), "buildcli-dotnet-muxer", Guid.NewGuid().ToString("N"));
		var runtimeOnly = Path.Combine(root, "runtime", DotnetMuxer.FileName);
		var sdk = Path.Combine(root, "sdk-install", DotnetMuxer.FileName);
		Directory.CreateDirectory(Path.GetDirectoryName(runtimeOnly)!);
		Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(sdk)!, "sdk", "8.0.100"));
		File.WriteAllText(runtimeOnly, "dotnet");
		File.WriteAllText(sdk, "dotnet");

		try
		{
			var resolved = DotnetMuxer.Resolve([runtimeOnly, sdk]);
			Assert.Equal(Path.GetFullPath(sdk), resolved);
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[Fact]
	public void Resolve_ReturnsNullWhenNoSdkExists()
	{
		Assert.Null(DotnetMuxer.Resolve(["/tmp/does-not-exist/dotnet"]));
	}
}
