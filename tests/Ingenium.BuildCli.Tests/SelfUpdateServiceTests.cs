// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using Ingenium.BuildCli.SelfUpdate;
using Ingenium.BuildCli.Tests.Support;

using Ingenium.BuildCli;

namespace Ingenium.BuildCli.Tests;

public sealed class SelfUpdateServiceTests
{
	[Fact]
	public async Task Update_PublishesSourceAndInstallsBinary()
	{
		using var workspace = new TempWorkspace();
		FakeGitClient.WriteProject(workspace.Source);
		var git = new FakeGitClient();
		var processes = new FakeProcessRunner { PublishedVersion = "0.2.0" };
		var service = new SelfUpdateService(git, processes);

		var result = await service.UpdateAsync(new SelfUpdateRequest
		{
			Source = workspace.Source,
			InstallDirectory = workspace.Install,
			BinDirectory = workspace.Bin
		});

		Assert.Empty(git.Commands);
		Assert.Equal("0.2.0", result.Version);
		Assert.Equal(Path.Combine(workspace.Install, SelfUpdatePaths.GetExecutableFileName()), result.ExecutablePath);
		Assert.True(File.Exists(result.ExecutablePath));
		Assert.Contains(processes.Calls, call => call.FileName == "dotnet" && call.Arguments[0] == "publish");
		Assert.Contains(processes.Calls, call => call.Arguments.Contains("--version"));

		if (!OperatingSystem.IsWindows())
		{
			Assert.Equal(Path.Combine(workspace.Bin, "bld"), result.BinLink);
			Assert.True(File.Exists(result.BinLink));
		}
	}

	[Fact]
	public async Task Update_ClonesWhenSourceIsOmitted()
	{
		using var workspace = new TempWorkspace();
		var git = new FakeGitClient();
		var processes = new FakeProcessRunner();
		var service = new SelfUpdateService(git, processes);

		var result = await service.UpdateAsync(new SelfUpdateRequest
		{
			Ref = "release/1.0",
			Url = "https://example.test/BuildCLI.git",
			InstallDirectory = workspace.Install,
			BinDirectory = workspace.Bin
		});

		Assert.Single(git.Commands);
		Assert.Equal("clone", git.Commands[0][0]);
		Assert.Contains("--depth", git.Commands[0]);
		Assert.Contains("1", git.Commands[0]);
		Assert.Contains("--branch", git.Commands[0]);
		Assert.Contains("release/1.0", git.Commands[0]);
		Assert.Contains("https://example.test/BuildCLI.git", git.Commands[0]);
		Assert.Equal("release/1.0", result.Ref);
		Assert.True(File.Exists(result.ExecutablePath));
		Assert.False(Directory.Exists(result.Source));
	}

	[Fact]
	public async Task Update_ThrowsWhenDotnetIsMissing()
	{
		var service = new SelfUpdateService(new FakeGitClient(), new FakeProcessRunner { DotnetAvailable = false });

		var error = await Assert.ThrowsAsync<BuildCliException>(() => service.UpdateAsync(new SelfUpdateRequest()));
		Assert.Equal(ExitCodes.BuildFailed, error.ExitCode);
		Assert.Contains("dotnet", error.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task Update_ThrowsWhenGitIsMissingAndCloneIsRequired()
	{
		var service = new SelfUpdateService(new FakeGitClient { Available = false }, new FakeProcessRunner());

		var error = await Assert.ThrowsAsync<BuildCliException>(() => service.UpdateAsync(new SelfUpdateRequest()));
		Assert.Equal(ExitCodes.GitNotFound, error.ExitCode);
	}

	[Fact]
	public async Task Update_ThrowsWhenSourceProjectIsMissing()
	{
		using var workspace = new TempWorkspace();
		Directory.CreateDirectory(workspace.Source);
		var service = new SelfUpdateService(new FakeGitClient(), new FakeProcessRunner());

		var error = await Assert.ThrowsAsync<BuildCliException>(() => service.UpdateAsync(new SelfUpdateRequest
		{
			Source = workspace.Source,
			InstallDirectory = workspace.Install
		}));

		Assert.Contains("Could not find", error.Message, StringComparison.Ordinal);
	}

	[Fact]
	public async Task Update_ThrowsWhenPublishFails()
	{
		using var workspace = new TempWorkspace();
		FakeGitClient.WriteProject(workspace.Source);
		var processes = new FakeProcessRunner
		{
			PublishExitCode = 1,
			PublishError = "MSB1009"
		};
		var service = new SelfUpdateService(new FakeGitClient(), processes);

		var error = await Assert.ThrowsAsync<BuildCliException>(() => service.UpdateAsync(new SelfUpdateRequest
		{
			Source = workspace.Source,
			InstallDirectory = workspace.Install
		}));

		Assert.Equal(ExitCodes.BuildFailed, error.ExitCode);
		Assert.Contains("MSB1009", error.Message);
	}

	[Fact]
	public async Task Update_PassesFrameworkDependentToPublish()
	{
		using var workspace = new TempWorkspace();
		FakeGitClient.WriteProject(workspace.Source);
		var processes = new FakeProcessRunner();
		var service = new SelfUpdateService(new FakeGitClient(), processes);

		await service.UpdateAsync(new SelfUpdateRequest
		{
			Source = workspace.Source,
			InstallDirectory = workspace.Install,
			BinDirectory = workspace.Bin,
			FrameworkDependent = true
		});

		var publish = Assert.Single(processes.Calls, call => call.FileName == "dotnet" && call.Arguments[0] == "publish");
		Assert.Contains("--self-contained", publish.Arguments);
		Assert.Contains("false", publish.Arguments);
	}

	private sealed class TempWorkspace : IDisposable
	{
		public TempWorkspace()
		{
			Root = Path.Combine(Path.GetTempPath(), "buildcli-self-update", Guid.NewGuid().ToString("N"));
			Source = Path.Combine(Root, "source");
			Install = Path.Combine(Root, "install");
			Bin = Path.Combine(Root, "bin");
			Directory.CreateDirectory(Root);
		}

		public string Root { get; }

		public string Source { get; }

		public string Install { get; }

		public string Bin { get; }

		public void Dispose()
		{
			try
			{
				if (Directory.Exists(Root))
				{
					Directory.Delete(Root, recursive: true);
				}
			}
			catch (IOException)
			{
			}
		}
	}
}
