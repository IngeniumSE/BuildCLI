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

	[Fact]
	public void Apply_ForwardsUnknownCommandAsCakeTarget()
	{
		Assert.Equal(["build", "Test"], CommandLineDefaults.Apply(["Test"]));
		Assert.Equal(["build", "Publish"], CommandLineDefaults.Apply(["Publish"]));
	}

	[Fact]
	public void Apply_KeepsCliOptionsAndForwardsCakeArguments()
	{
		Assert.Equal(
			["build", "Publish", "--path", "./src", "--", "--publish", "--nuget", "--token", "abc"],
			CommandLineDefaults.Apply(["Publish", "--path", "./src", "--publish", "--nuget", "--token", "abc"]));
	}

	[Fact]
	public void Apply_NormalizesExplicitBuildCommandCakeArguments()
	{
		Assert.Equal(
			["build", "Test", "--configuration", "Release", "--", "--verbosity", "Diagnostic"],
			CommandLineDefaults.Apply(["build", "Test", "--configuration", "Release", "--verbosity", "Diagnostic"]));
	}

	[Fact]
	public void IsKnownCommand_RecognizesFirstClassCommands()
	{
		Assert.True(CommandLineDefaults.IsKnownCommand("init"));
		Assert.False(CommandLineDefaults.IsKnownCommand("Test"));
	}
}
