// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using Ingenium.BuildCli.Submodule;

namespace Ingenium.BuildCli.Tests;

public sealed class BuildSubmoduleGuardTests
{
	[Fact]
	public void EnsureInitialized_ThrowsWhenNotRegistered()
	{
		var error = Assert.Throws<BuildCliException>(() => BuildSubmoduleGuard.EnsureInitialized(new BuildSubmoduleStatus
		{
			RepositoryRoot = "/tmp/repo",
			IsRegistered = false,
			IsInitialized = false
		}));

		Assert.Equal(ExitCodes.SubmoduleNotFound, error.ExitCode);
		Assert.Contains("bld init", error.Message);
	}

	[Fact]
	public void EnsureInitialized_ThrowsWhenNotCheckedOut()
	{
		var error = Assert.Throws<BuildCliException>(() => BuildSubmoduleGuard.EnsureInitialized(new BuildSubmoduleStatus
		{
			RepositoryRoot = "/tmp/repo",
			IsRegistered = true,
			IsInitialized = false
		}));

		Assert.Contains("not initialized", error.Message);
	}

	[Fact]
	public void EnsureInitialized_AllowsReadySubmodule()
	{
		BuildSubmoduleGuard.EnsureInitialized(new BuildSubmoduleStatus
		{
			RepositoryRoot = "/tmp/repo",
			IsRegistered = true,
			IsInitialized = true,
			RelativePath = "build"
		});
	}
}
