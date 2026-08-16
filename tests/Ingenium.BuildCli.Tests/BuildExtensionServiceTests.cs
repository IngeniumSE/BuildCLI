// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using Ingenium.BuildCli;
using Ingenium.BuildCli.Extensions;
using Ingenium.BuildCli.Tests.Support;

namespace Ingenium.BuildCli.Tests;

public sealed class BuildExtensionServiceTests
{
	[Fact]
	public async Task Create_WritesExpectedLayout()
	{
		using var workspace = GitTestWorkspace.Create();
		var service = new BuildExtensionService(GitTestWorkspace.CreateClient());

		var scaffold = await service.CreateAsync(new BuildExtensionRequest
		{
			RepositoryPath = workspace.ParentRepo,
			Name = "Framework"
		});

		Assert.Equal("FrameworkBuildExtensions", scaffold.ProjectName);
		Assert.True(File.Exists(Path.Combine(workspace.ParentRepo, "build-extensions", "Directory.Build.props")));
		Assert.True(File.Exists(Path.Combine(workspace.ParentRepo, "build-extensions", "Directory.Build.targets")));
		Assert.True(File.Exists(Path.Combine(scaffold.ProjectDirectory, "FrameworkBuildExtensions.csproj")));
		Assert.True(File.Exists(Path.Combine(scaffold.ProjectDirectory, "SampleTask.cs")));

		var project = File.ReadAllText(Path.Combine(scaffold.ProjectDirectory, "FrameworkBuildExtensions.csproj"));
		Assert.Contains("Build.Abstractions", project);
		Assert.Contains("net8.0", project);

		var props = File.ReadAllText(Path.Combine(workspace.ParentRepo, "build-extensions", "Directory.Build.props"));
		Assert.Contains("../build/apps/Directory.Build.props", props);
	}

	[Fact]
	public async Task Create_ThrowsWhenProjectExists()
	{
		using var workspace = GitTestWorkspace.Create();
		var service = new BuildExtensionService(GitTestWorkspace.CreateClient());
		var request = new BuildExtensionRequest
		{
			RepositoryPath = workspace.ParentRepo,
			Name = "Demo"
		};

		await service.CreateAsync(request);
		var error = await Assert.ThrowsAsync<BuildCliException>(() => service.CreateAsync(request));
		Assert.Equal(ExitCodes.AlreadyExists, error.ExitCode);
	}

	[Fact]
	public async Task Create_UsesRepositoryFolderName()
	{
		using var workspace = GitTestWorkspace.Create();
		var service = new BuildExtensionService(GitTestWorkspace.CreateClient());

		var scaffold = await service.CreateAsync(new BuildExtensionRequest
		{
			RepositoryPath = workspace.ParentRepo
		});

		Assert.Equal("ParentBuildExtensions", scaffold.ProjectName);
	}
}
