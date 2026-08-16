// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using Ingenium.BuildCli.Git;
using Ingenium.BuildCli.Submodule;
using Ingenium.BuildCli.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

using Spectre.Console.Cli;
using Spectre.Console.Testing;

namespace Ingenium.BuildCli.Tests;

public sealed class CommandAppTests
{
	[Fact]
	public async Task Help_ListsPrimaryCommands()
	{
		var console = new TestConsole();
		var app = CreateApp(console);
		var exitCode = await app.RunAsync(["--help"]);

		Assert.Equal(0, exitCode);
		var output = console.Output;
		Assert.Contains("init", output, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("update", output, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("status", output, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("tags", output, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task InitHelp_DescribesTagOption()
	{
		var console = new TestConsole();
		var app = CreateApp(console);
		var exitCode = await app.RunAsync(["init", "--help"]);

		Assert.Equal(0, exitCode);
		Assert.Contains("--tag", console.Output, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task Status_RendersUninitializedRepository()
	{
		using var workspace = GitTestWorkspace.Create();
		var console = new TestConsole();
		var app = CreateApp(console);
		var exitCode = await app.RunAsync(["status", "--path", workspace.ParentRepo, "--url", workspace.BuildRepo]);

		Assert.Equal(0, exitCode);
		Assert.Contains("not added", console.Output, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task InitThenStatus_ShowsCurrentTag()
	{
		using var workspace = GitTestWorkspace.Create();
		var console = new TestConsole();
		var app = CreateApp(console);

		var initExit = await app.RunAsync([
			"init",
			"--path", workspace.ParentRepo,
			"--url", workspace.BuildRepo,
			"--tag", "v1.0.0"
		]);
		Assert.Equal(0, initExit);

		console = new TestConsole();
		app = CreateApp(console);
		var statusExit = await app.RunAsync([
			"status",
			"--path", workspace.ParentRepo,
			"--url", workspace.BuildRepo
		]);

		Assert.Equal(0, statusExit);
		Assert.Contains("v1.0.0", console.Output);
	}

	[Fact]
	public void SelectLatest_UsedByStatusModel()
	{
		var status = new BuildSubmoduleStatus
		{
			RepositoryRoot = "/tmp/repo",
			Commit = "abc123",
			LatestCommit = "abc123def",
			LatestTag = "v1.0.0"
		};

		Assert.True(status.IsLatest);
	}

	private static CommandApp CreateApp(TestConsole console)
	{
		return BuildCliApplication.Create(console, services =>
		{
			services.AddSingleton<IGitClient>(_ => GitTestWorkspace.CreateClient());
		});
	}
}
