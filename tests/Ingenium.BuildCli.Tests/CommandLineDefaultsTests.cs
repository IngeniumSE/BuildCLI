// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

namespace Ingenium.BuildCli.Tests;

public sealed class CommandLineDefaultsTests
{
	[Fact]
	public void Apply_DefaultsEmptyArgsToBuild()
	{
		Assert.Equal(["build"], CommandLineDefaults.Apply([]));
	}

	[Fact]
	public void Apply_LeavesHelpAndVersionAlone()
	{
		Assert.Equal(["--help"], CommandLineDefaults.Apply(["--help"]));
		Assert.Equal(["-v"], CommandLineDefaults.Apply(["-v"]));
	}

	[Fact]
	public void Apply_LeavesKnownCommandsAlone()
	{
		Assert.Equal(["init", "--tag", "v1.0.0"], CommandLineDefaults.Apply(["init", "--tag", "v1.0.0"]));
		Assert.Equal(["status"], CommandLineDefaults.Apply(["status"]));
	}

	[Fact]
	public void Apply_TreatsLeadingOptionsAsBuildOptions()
	{
		Assert.Equal(
			["build", "--path", "./src", "--configuration", "Release"],
			CommandLineDefaults.Apply(["--path", "./src", "--configuration", "Release"]));
	}
}
