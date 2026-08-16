// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using Ingenium.BuildCli.Host;

namespace Ingenium.BuildCli.Tests;

public sealed class BuildHostArgumentsTests
{
	[Fact]
	public void Create_AddsDefaultTarget()
	{
		var args = BuildHostArguments.Create(null, null, []);

		Assert.Equal(["--no-launch-profile", "--", "--target", "Default"], args);
	}

	[Fact]
	public void Create_DoesNotDuplicateTarget()
	{
		var args = BuildHostArguments.Create("Default", "Release", ["--target", "PackProjects"]);

		Assert.DoesNotContain("Default", args);
		Assert.Contains("--configuration", args);
		Assert.Contains("Release", args);
		Assert.Contains("PackProjects", args);
	}

	[Fact]
	public void FindBuildProject_PrefersAppsBuild()
	{
		var root = Path.Combine(Path.GetTempPath(), "buildcli-tests", Guid.NewGuid().ToString("N"));
		var projectDir = Path.Combine(root, "apps", "Build");
		Directory.CreateDirectory(projectDir);
		var project = Path.Combine(projectDir, "Build.csproj");
		File.WriteAllText(project, "<Project />");

		try
		{
			Assert.Equal(project, BuildHostService.FindBuildProject(root));
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}
}
