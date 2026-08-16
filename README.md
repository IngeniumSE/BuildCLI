# BuildCLI

A cross-platform CLI for adding and updating the [Ingenium Build](https://github.com/IngeniumSE/Build) git submodule in a parent repository.

The tool is written in C# and uses [Spectre.Console](https://spectreconsole.net/) for command parsing and terminal layout.

## Commands

Run `buildcli` from any git repository that should host the Build submodule.

```text
buildcli init                 Add the Build submodule (defaults to the latest tag)
buildcli init --tag v1.2.3    Add the Build submodule pinned to a specific tag
buildcli update               Move an existing submodule to the latest tag
buildcli update --tag v1.2.3  Move an existing submodule to a specific tag
buildcli status               Show the current submodule path, commit, and tags
buildcli tags                 List tags advertised by the Build remote
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

Existing Ingenium repositories that already use `build` or `Build` as the submodule path are detected automatically.

## Installation

The installer publishes a self-contained `buildcli` binary and places it on your PATH. Git is required. The .NET 8 SDK is installed automatically when it is missing.

### macOS and Linux

From a clone:

```bash
./scripts/install.sh
```

Or later, once this repository is available remotely:

```bash
curl -sSL https://raw.githubusercontent.com/IngeniumSE/BuildCLI/main/scripts/install.sh | bash
```

The default install location is `~/.local/share/ingenium/buildcli`, with a symlink at `~/.local/bin/buildcli`. Add `~/.local/bin` to `PATH` if the installer reports that the command is not visible yet.

### Windows

From a clone:

```powershell
./scripts/install.ps1
```

Or later:

```powershell
irm https://raw.githubusercontent.com/IngeniumSE/BuildCLI/main/scripts/install.ps1 | iex
```

The default install location is `%LOCALAPPDATA%\Ingenium\BuildCli`. That directory is added to the user `PATH`. Open a new terminal before running `buildcli`.

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
