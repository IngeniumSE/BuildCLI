// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using Ingenium.BuildCli.Commands;
using Ingenium.BuildCli.Extensions;
using Ingenium.BuildCli.Git;
using Ingenium.BuildCli.Host;
using Ingenium.BuildCli.Infrastructure;
using Ingenium.BuildCli.Execution;
using Ingenium.BuildCli.Rendering;
using Ingenium.BuildCli.Submodule;

using Microsoft.Extensions.DependencyInjection;

using Spectre.Console;
using Spectre.Console.Cli;

namespace Ingenium.BuildCli;

/// <summary>
/// Configures the Spectre.Console.Cli application and its services.
/// </summary>
public static class BuildCliApplication
{
	/// <summary>
	/// Creates the configured command application.
	/// </summary>
	public static CommandApp Create(IAnsiConsole? console = null, Action<IServiceCollection>? configureServices = null)
	{
		var services = new ServiceCollection();
		services.AddSingleton(console ?? AnsiConsole.Console);
		services.AddSingleton<IGitTrace, AnsiConsoleGitTrace>();
		services.AddSingleton<IGitClient>(provider => new GitClient(trace: provider.GetRequiredService<IGitTrace>()));
		services.AddSingleton<IProcessRunner>(provider => new ProcessRunner(provider.GetRequiredService<IGitTrace>()));
		services.AddSingleton<IBuildSubmoduleService, BuildSubmoduleService>();
		services.AddSingleton<IBuildHostService, BuildHostService>();
		services.AddSingleton<IBuildExtensionService, BuildExtensionService>();
		configureServices?.Invoke(services);

		var app = new CommandApp(new TypeRegistrar(services));
		app.Configure(config =>
		{
			if (console is not null)
			{
				config.ConfigureConsole(console);
			}

			Configure(config);
		});
		return app;
	}

	/// <summary>
	/// Registers commands, examples, and the global exception handler.
	/// </summary>
	public static void Configure(IConfigurator config)
	{
		config.SetApplicationName("buildcli");
		config.SetApplicationVersion(AppVersion.Current);
		config.ValidateExamples();

		config.SetExceptionHandler((exception, resolver) =>
		{
			var console = resolver?.Resolve(typeof(IAnsiConsole)) as IAnsiConsole ?? AnsiConsole.Console;
			ConsoleWriter.WriteError(console, exception);
			return exception is BuildCliException buildCliException
				? buildCliException.ExitCode
				: ExitCodes.GeneralError;
		});

		config.AddCommand<InitCommand>("init")
			.WithDescription("Add the Ingenium Build submodule to a repository.")
			.WithExample("init")
			.WithExample("init", "--tag", "v1.2.3")
			.WithExample("init", "--path", "./src", "--https");

		config.AddCommand<UpdateCommand>("update")
			.WithDescription("Update the Build submodule to the latest tag or a specific version.")
			.WithExample("update")
			.WithExample("update", "--tag", "v1.2.3");

		config.AddCommand<StatusCommand>("status")
			.WithDescription("Show the current Build submodule state.")
			.WithExample("status");

		config.AddCommand<TagsCommand>("tags")
			.WithDescription("List tags available on the Build remote.")
			.WithExample("tags")
			.WithExample("tags", "--https");

		config.AddCommand<RepairCommand>("repair")
			.WithDescription("Repair a dirty or broken Build submodule.")
			.WithExample("repair", "--strategy", "stash")
			.WithExample("repair", "--strategy", "reset", "--yes")
			.WithExample("repair", "--strategy", "reinit", "--tag", "v1.2.3", "--yes");

		config.AddCommand<BuildCommand>("build")
			.WithDescription("Run a Cake target through the Build submodule.")
			.WithExample("build")
			.WithExample("build", "TestProjects")
			.WithExample("build", "Default", "--configuration", "Release");

		config.AddCommand<ExtensionCommand>("extension")
			.WithDescription("Create a Cake build-extension project in build-extensions/.")
			.WithExample("extension")
			.WithExample("extension", "Framework");
	}
}
