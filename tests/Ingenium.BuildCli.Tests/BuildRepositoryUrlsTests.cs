// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using Ingenium.BuildCli.Submodule;

namespace Ingenium.BuildCli.Tests;

public sealed class BuildRepositoryUrlsTests
{
	[Theory]
	[InlineData("git@github.com:IngeniumSE/Build.git", true)]
	[InlineData("https://github.com/IngeniumSE/Build.git", true)]
	[InlineData("https://github.com/IngeniumSE/Build", true)]
	[InlineData("git@github.com:IngeniumSE/CLI.git", false)]
	[InlineData(null, false)]
	public void IsBuildRepository_RecognizesCanonicalUrls(string? url, bool expected)
	{
		Assert.Equal(expected, BuildRepositoryUrls.IsBuildRepository(url));
	}

	[Fact]
	public void InferFromParentRemote_UsesHttpsWhenParentIsHttps()
	{
		var url = BuildRepositoryUrls.InferFromParentRemote("https://github.com/IngeniumSE/CLI.git", useHttps: null);

		Assert.Equal(BuildRepositoryUrls.Https, url);
	}

	[Fact]
	public void InferFromParentRemote_UsesSshByDefault()
	{
		var url = BuildRepositoryUrls.InferFromParentRemote("git@github.com:IngeniumSE/CLI.git", useHttps: null);

		Assert.Equal(BuildRepositoryUrls.Ssh, url);
	}

	[Fact]
	public void InferFromParentRemote_HonoursHttpsOverride()
	{
		var url = BuildRepositoryUrls.InferFromParentRemote("git@github.com:IngeniumSE/CLI.git", useHttps: true);

		Assert.Equal(BuildRepositoryUrls.Https, url);
	}
}
