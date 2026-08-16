# Installs bld onto PATH for Windows.
# Usage:
#   ./scripts/install.ps1
#   irm https://raw.githubusercontent.com/IngeniumSE/BuildCLI/main/scripts/install.ps1 | iex
[CmdletBinding()]
param(
	[string] $RepoUrl = $(if ($env:BUILDCLI_REPO_URL) { $env:BUILDCLI_REPO_URL } else { "https://github.com/IngeniumSE/BuildCLI.git" }),
	[string] $InstallDir = $(if ($env:BLD_INSTALL_DIR) { $env:BLD_INSTALL_DIR } elseif ($env:BUILDCLI_INSTALL_DIR) { $env:BUILDCLI_INSTALL_DIR } else { Join-Path $env:LOCALAPPDATA "Ingenium\bld" }),
	[switch] $FrameworkDependent
)

$ErrorActionPreference = "Stop"

function Write-Log {
	param([string] $Message)
	Write-Host "==> $Message"
}

function Get-Rid {
	$arch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture
	switch ($arch) {
		"X64" { return "win-x64" }
		"Arm64" { return "win-arm64" }
		default { throw "Unsupported architecture: $arch" }
	}
}

function Ensure-Dotnet {
	if (Get-Command dotnet -ErrorAction SilentlyContinue) {
		return
	}

	Write-Log "dotnet was not found; installing the .NET 8 SDK"
	$installScript = Join-Path $env:TEMP "dotnet-install.ps1"
	Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile $installScript
	& $installScript -Channel 8.0
	$env:PATH = "$env:USERPROFILE\.dotnet;$env:USERPROFILE\.dotnet\tools;$env:PATH"
	if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
		throw "dotnet installation completed but the SDK is still not on PATH."
	}
}

function Get-SourceDirectory {
	$scriptDir = $PSScriptRoot
	if ($scriptDir -and (Test-Path (Join-Path $scriptDir "..\apps\Ingenium.BuildCli\Ingenium.BuildCli.csproj"))) {
		return (Resolve-Path (Join-Path $scriptDir "..")).Path
	}

	if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
		throw "git is required to install bld."
	}

	$checkout = Join-Path $env:TEMP ("buildcli-src-" + [Guid]::NewGuid().ToString("N"))
	Write-Log "Cloning $RepoUrl"
	git clone --depth 1 $RepoUrl $checkout
	if ($LASTEXITCODE -ne 0) {
		throw "Failed to clone $RepoUrl"
	}
	return $checkout
}

function Add-ToUserPath {
	param([string] $Directory)

	$current = [Environment]::GetEnvironmentVariable("Path", "User")
	if ([string]::IsNullOrEmpty($current)) {
		[Environment]::SetEnvironmentVariable("Path", $Directory, "User")
		return
	}

	$parts = $current.Split(";", [System.StringSplitOptions]::RemoveEmptyEntries)
	if ($parts -contains $Directory) {
		return
	}

	[Environment]::SetEnvironmentVariable("Path", ($current.TrimEnd(";") + ";" + $Directory), "User")
}

Ensure-Dotnet
if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
	throw "git is required to install bld."
}

$sourceDir = Get-SourceDirectory
$rid = Get-Rid
New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null

Write-Log "Publishing bld for $rid"
$publishArgs = @(
	"publish", (Join-Path $sourceDir "apps\Ingenium.BuildCli\Ingenium.BuildCli.csproj"),
	"-c", "Release",
	"-r", $rid,
	"-o", $InstallDir,
	"--nologo"
)

if ($FrameworkDependent) {
	$publishArgs += @("--self-contained", "false")
}
else {
	$publishArgs += @(
		"--self-contained", "true",
		"-p:PublishSingleFile=true",
		"-p:IncludeNativeLibrariesForSelfExtract=true"
	)
}

& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
	throw "dotnet publish failed."
}

$executable = Join-Path $InstallDir "bld.exe"
if (-not (Test-Path $executable)) {
	throw "Publish succeeded but $executable was not produced."
}

Add-ToUserPath $InstallDir
$env:PATH = "$InstallDir;$env:PATH"

Write-Log "Installed $executable"
Write-Log "Added $InstallDir to the user PATH"
Write-Host ""
Write-Host "Open a new terminal, then run: bld --help"
