# BuildCLI

A cross-platform CLI for adding and updating the [Ingenium Build](https://github.com/IngeniumSE/Build) git submodule in a parent repository.

The tool is written in C# and uses [Spectre.Console](https://spectreconsole.net/) for command parsing and terminal layout.

## Commands

Run `bld` from any git repository that should host the Build submodule.

A bare `bld` (no command) runs `build` after verifying that the Build submodule is initialized. If it is missing, the CLI stops and tells you to run `bld init`.

Any unknown command is forwarded to the Build host as a Cake target, so `bld Test` is the same as `bld build Test`. Extra Cake arguments are passed through; `bld`'s own options (`--path`, `--configuration`, and so on) are still parsed by the CLI.

```text
bld                           Run the Default Cake target (same as `bld build`)
bld Test                      Run the Cake Test target (unknown commands are forwarded)
bld Publish --publish --nuget Run a Cake target and pass extra host arguments
bld init                      Add the Build submodule (defaults to the latest tag)
bld init --tag v1.2.3         Add the Build submodule pinned to a specific tag
bld update                    Move an existing submodule to the latest tag
bld update --tag v1.2.3       Move an existing submodule to a specific tag
bld self-update               Reinstall the bld CLI from the latest main
bld self-update --ref v0.2.0  Install a specific BuildCLI branch or tag
bld upgrade                   Alias for self-update
bld status                    Show the current submodule path, commit, and tags
bld tags                      List tags advertised by the Build remote
bld repair --strategy stash   Stash local submodule changes, then restore the parent HEAD
bld repair --strategy reset --yes
                              Discard local changes and restore the parent-recorded HEAD
bld repair --strategy reinit --tag v1.2.3 --yes
                              Delete and clone the submodule again at a tagged version
bld build                     Run the Build host Default target
bld build TestProjects        Run a specific Cake target in the Build submodule
bld extension                 Create build-extensions/{Repo}BuildExtensions
bld extension Framework       Create build-extensions/FrameworkBuildExtensions
```

Common options:

| Option | Description |
| --- | --- |
| `-p`, `--path <PATH>` | Parent repository path. Defaults to the current directory. |
| `--submodule-path <PATH>` | Relative submodule path. Detects an existing Build entry, otherwise `build`. |
| `--url <URL>` | Override the submodule URL. Defaults to `git@github.com:IngeniumSE/Build.git`. |
| `--https` | Use `https://github.com/IngeniumSE/Build.git` instead of SSH. |
| `-t`, `--tag <REF>` | Tag, branch, or commit to check out. |
| `-f`, `--force` | Re-initialize or overwrite an existing submodule during `init`. |
| `--verbose` | Write the git commands that are executed. |

`init` and `update` stage `.gitmodules` and the submodule gitlink. They do not create a commit, so you can review the change in the parent repository first.

`self-update` (alias `upgrade`) republishes this CLI from [IngeniumSE/BuildCLI](https://github.com/IngeniumSE/BuildCLI) and replaces the installed `bld` binary. It does not change the Build submodule; use `update` for that. By default it clones `main` over HTTPS. Pass `--source` to publish an existing checkout, or `--ref` for a branch or tag. The install location matches `scripts/install.sh` / `scripts/install.ps1` (`~/.local/share/ingenium/bld` on Unix, `%LOCALAPPDATA%\Ingenium\bld` on Windows) and can be overridden with `BLD_INSTALL_DIR` or `--install-dir`.

Existing Ingenium repositories that already use `build` or `Build` as the submodule path are detected automatically.

`repair` can prompt for a strategy when run interactively. `reset` and `reinit` are destructive and require `--yes` in non-interactive use.

`build` first checks that the Build submodule is registered and checked out. It then restores .NET local tools when `.config/dotnet-tools.json` is present, and runs `apps/Build` the same way `./build.sh` does. Unknown commands such as `Test`, `Pack`, or a build-extension task name are forwarded to that host as `--target` values.

`extension` writes the layout the Build host already imports:

```text
build-extensions/Directory.Build.props
build-extensions/Directory.Build.targets
build-extensions/{Name}BuildExtensions/{Name}BuildExtensions.csproj
build-extensions/{Name}BuildExtensions/SampleTask.cs
```

## Installation

The installer publishes a self-contained `bld` binary and places it on your PATH. Git is required. The .NET 8 SDK is installed automatically when it is missing.

### macOS and Linux

From a clone:

```bash
./scripts/install.sh
```

Or later, once this repository is available remotely:

```bash
curl -sSL https://raw.githubusercontent.com/IngeniumSE/BuildCLI/main/scripts/install.sh | bash
```

The default install location is `~/.local/share/ingenium/bld`, with a symlink at `~/.local/bin/bld`. Add `~/.local/bin` to `PATH` if the installer reports that the command is not visible yet.

After `bld` is on PATH, later versions can be installed with:

```bash
bld self-update
```

### Windows

From a clone:

```powershell
./scripts/install.ps1
```

Or later:

```powershell
irm https://raw.githubusercontent.com/IngeniumSE/BuildCLI/main/scripts/install.ps1 | iex
```

The default install location is `%LOCALAPPDATA%\Ingenium\bld`. That directory is added to the user `PATH`. Open a new terminal before running `bld`. After `bld` is on PATH, later versions can be installed with `bld self-update`.

### .NET tool

The project is also packable as a .NET global tool:

```bash
dotnet pack apps/Ingenium.BuildCli -c Release
dotnet tool install --global --add-source apps/Ingenium.BuildCli/nupkg Ingenium.BuildCli
```

## Development

```bash
dotnet test
```

or:

```bash
./build.sh
```

The solution targets .NET 8 and follows the Ingenium repository layout (`apps/`, `tests/`, central package management).
