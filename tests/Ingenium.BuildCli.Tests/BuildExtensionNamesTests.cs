// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using Ingenium.BuildCli.Extensions;

namespace Ingenium.BuildCli.Tests;

public sealed class BuildExtensionNamesTests
{
	[Theory]
	[InlineData("Framework", "FrameworkBuildExtensions")]
	[InlineData("FrameworkBuildExtensions", "FrameworkBuildExtensions")]
	[InlineData("open-f1", "OpenF1BuildExtensions")]
	[InlineData("1repo", "RepoBuildExtensions")]
	public void ToProjectName_AddsSuffixOnce(string name, string expected)
	{
		Assert.Equal(expected, BuildExtensionNames.ToProjectName(name));
	}
}
