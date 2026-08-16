// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using Ingenium.BuildCli;
using Ingenium.BuildCli.Git;
using Ingenium.BuildCli.Submodule;
using Ingenium.BuildCli.Tests.Support;

namespace Ingenium.BuildCli.Tests;

public sealed class BuildSubmoduleServiceTests
{
	[Fact]
	public async Task Init_AddsSubmoduleAtLatestTag()
	{
		using var workspace = GitTestWorkspace.Create();
		var service = CreateService();

		var change = await service.InitAsync(new BuildSubmoduleRequest
		{
			RepositoryPath = workspace.ParentRepo,
			Url = workspace.BuildRepo
		});

		Assert.True(change.Added);
		Assert.Equal("build", change.RelativePath);
		Assert.Equal("v1.1.0", change.CheckedOutRef);
		Assert.True(Directory.Exists(Path.Combine(workspace.ParentRepo, "build")));
		Assert.Contains("v1.1.0", GitTestWorkspace.Git(Path.Combine(workspace.ParentRepo, "build"), "tag", "--points-at", "HEAD"));
	}

	[Fact]
	public async Task Init_ChecksOutSpecificTag()
	{
		using var workspace = GitTestWorkspace.Create();
		var service = CreateService();

		var change = await service.InitAsync(new BuildSubmoduleRequest
		{
			RepositoryPath = workspace.ParentRepo,
			Url = workspace.BuildRepo,
			Tag = "v1.0.0"
		});

		Assert.Equal("v1.0.0", change.CheckedOutRef);
		Assert.Contains("v1.0.0", GitTestWorkspace.Git(Path.Combine(workspace.ParentRepo, "build"), "tag", "--points-at", "HEAD"));
	}

	[Fact]
	public async Task Init_ThrowsWhenAlreadyInitialized()
	{
		using var workspace = GitTestWorkspace.Create();
		var service = CreateService();
		var request = new BuildSubmoduleRequest
		{
			RepositoryPath = workspace.ParentRepo,
			Url = workspace.BuildRepo
		};

		await service.InitAsync(request);

		var error = await Assert.ThrowsAsync<BuildCliException>(() => service.InitAsync(request));
		Assert.Equal(ExitCodes.AlreadyInitialized, error.ExitCode);
	}

	[Fact]
	public async Task Update_MovesFromOlderTagToLatest()
	{
		using var workspace = GitTestWorkspace.Create();
		var service = CreateService();

		await service.InitAsync(new BuildSubmoduleRequest
		{
			RepositoryPath = workspace.ParentRepo,
			Url = workspace.BuildRepo,
			Tag = "v1.0.0"
		});

		var change = await service.UpdateAsync(new BuildSubmoduleRequest
		{
			RepositoryPath = workspace.ParentRepo,
			Url = workspace.BuildRepo
		});

		Assert.False(change.Added);
		Assert.Equal("v1.1.0", change.CheckedOutRef);
		Assert.NotEqual(change.PreviousCommit, change.Commit);
	}

	[Fact]
	public async Task Update_CanPinSpecificTag()
	{
		using var workspace = GitTestWorkspace.Create();
		var service = CreateService();

		await service.InitAsync(new BuildSubmoduleRequest
		{
			RepositoryPath = workspace.ParentRepo,
			Url = workspace.BuildRepo
		});

		var change = await service.UpdateAsync(new BuildSubmoduleRequest
		{
			RepositoryPath = workspace.ParentRepo,
			Url = workspace.BuildRepo,
			Tag = "v1.0.0"
		});

		Assert.Equal("v1.0.0", change.CheckedOutRef);
	}

	[Fact]
	public async Task Status_ReportsRegisteredSubmodule()
	{
		using var workspace = GitTestWorkspace.Create();
		var service = CreateService();

		await service.InitAsync(new BuildSubmoduleRequest
		{
			RepositoryPath = workspace.ParentRepo,
			Url = workspace.BuildRepo,
			Tag = "v1.0.0"
		});

		var status = await service.GetStatusAsync(new BuildSubmoduleRequest
		{
			RepositoryPath = workspace.ParentRepo
		});

		Assert.True(status.IsRegistered);
		Assert.True(status.IsInitialized);
		Assert.Equal("build", status.RelativePath);
		Assert.Contains("v1.0.0", status.CurrentTags);
		Assert.Equal("v1.1.0", status.LatestTag);
		Assert.False(status.IsLatest);
	}

	[Fact]
	public async Task ListTags_ReturnsRemoteTags()
	{
		using var workspace = GitTestWorkspace.Create();
		var service = CreateService();

		var tags = await service.ListTagsAsync(new BuildSubmoduleRequest
		{
			RepositoryPath = workspace.ParentRepo,
			Url = workspace.BuildRepo
		});

		Assert.Contains(tags, tag => tag.Name == "v1.0.0");
		Assert.Contains(tags, tag => tag.Name == "v1.1.0");
	}

	[Fact]
	public async Task Update_ThrowsWhenSubmoduleIsMissing()
	{
		using var workspace = GitTestWorkspace.Create();
		var service = CreateService();

		var error = await Assert.ThrowsAsync<BuildCliException>(() => service.UpdateAsync(new BuildSubmoduleRequest
		{
			RepositoryPath = workspace.ParentRepo
		}));

		Assert.Equal(ExitCodes.SubmoduleNotFound, error.ExitCode);
	}

	[Fact]
	public async Task Init_UsesCustomSubmodulePath()
	{
		using var workspace = GitTestWorkspace.Create();
		var service = CreateService();

		var change = await service.InitAsync(new BuildSubmoduleRequest
		{
			RepositoryPath = workspace.ParentRepo,
			Url = workspace.BuildRepo,
			SubmodulePath = "Build"
		});

		Assert.Equal("Build", change.RelativePath);
		Assert.True(Directory.Exists(Path.Combine(workspace.ParentRepo, "Build")));
	}

	[Fact]
	public async Task Repair_Reset_DiscardsLocalChanges()
	{
		using var workspace = GitTestWorkspace.Create();
		var service = CreateService();
		var request = new BuildSubmoduleRequest
		{
			RepositoryPath = workspace.ParentRepo,
			Url = workspace.BuildRepo
		};

		await service.InitAsync(request);
		var readme = Path.Combine(workspace.ParentRepo, "build", "README.md");
		var original = File.ReadAllText(readme);
		File.WriteAllText(readme, "broken locally");

		var result = await service.RepairAsync(request, RepairStrategy.Reset);

		Assert.Equal(RepairStrategy.Reset, result.Strategy);
		Assert.Equal(original, File.ReadAllText(readme));
		Assert.Equal(string.Empty, GitTestWorkspace.Git(Path.Combine(workspace.ParentRepo, "build"), "status", "--porcelain"));
	}

	[Fact]
	public async Task Repair_Stash_PreservesLocalChanges()
	{
		using var workspace = GitTestWorkspace.Create();
		var service = CreateService();
		var request = new BuildSubmoduleRequest
		{
			RepositoryPath = workspace.ParentRepo,
			Url = workspace.BuildRepo
		};

		await service.InitAsync(request);
		var readme = Path.Combine(workspace.ParentRepo, "build", "README.md");
		var original = File.ReadAllText(readme);
		File.WriteAllText(readme, "keep me");

		var result = await service.RepairAsync(request, RepairStrategy.Stash);

		Assert.Equal(original, File.ReadAllText(readme));
		Assert.False(string.IsNullOrWhiteSpace(result.StashRef));
		var stashShow = GitTestWorkspace.Git(Path.Combine(workspace.ParentRepo, "build"), "stash", "show", "-p", result.StashRef!);
		Assert.Contains("keep me", stashShow);
	}

	[Fact]
	public async Task Repair_Reinit_ChecksOutRequestedTag()
	{
		using var workspace = GitTestWorkspace.Create();
		var service = CreateService();

		await service.InitAsync(new BuildSubmoduleRequest
		{
			RepositoryPath = workspace.ParentRepo,
			Url = workspace.BuildRepo
		});

		var result = await service.RepairAsync(
			new BuildSubmoduleRequest
			{
				RepositoryPath = workspace.ParentRepo,
				Url = workspace.BuildRepo,
				Tag = "v1.0.0"
			},
			RepairStrategy.Reinit);

		Assert.Equal(RepairStrategy.Reinit, result.Strategy);
		Assert.Equal("v1.0.0", result.Change.CheckedOutRef);
		Assert.Contains("v1.0.0", GitTestWorkspace.Git(Path.Combine(workspace.ParentRepo, "build"), "tag", "--points-at", "HEAD"));
	}

	private static BuildSubmoduleService CreateService()
	{
		return new BuildSubmoduleService(GitTestWorkspace.CreateClient());
	}
}
