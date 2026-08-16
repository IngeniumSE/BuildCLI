// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using Ingenium.BuildCli.SelfUpdate;

namespace Ingenium.BuildCli.Tests;

public sealed class SelfUpdatePublishArgumentsTests
{
	[Fact]
	public void Create_MatchesSelfContainedInstallScript()
	{
		var args = SelfUpdatePublishArguments.Create("project.csproj", "linux-x64", "/tmp/out", frameworkDependent: false);

		Assert.Equal(
			[
				"publish",
				"project.csproj",
				"-c",
				"Release",
				"-r",
				"linux-x64",
				"-o",
				"/tmp/out",
				"--nologo",
				"--self-contained",
				"true",
				"-p:PublishSingleFile=true",
				"-p:IncludeNativeLibrariesForSelfExtract=true"
			],
			args);
	}

	[Fact]
	public void Create_CanPublishFrameworkDependent()
	{
		var args = SelfUpdatePublishArguments.Create("project.csproj", "win-x64", "C:\\out", frameworkDependent: true);

		Assert.Contains("--self-contained", args);
		Assert.Contains("false", args);
		Assert.DoesNotContain("-p:PublishSingleFile=true", args);
	}
}
