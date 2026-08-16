#!/usr/bin/env bash
# Installs bld onto PATH for macOS and Linux.
# Usage:
#   ./scripts/install.sh
#   curl -sSL https://raw.githubusercontent.com/IngeniumSE/BuildCLI/main/scripts/install.sh | bash
set -euo pipefail

REPO_URL="${BUILDCLI_REPO_URL:-https://github.com/IngeniumSE/BuildCLI.git}"
INSTALL_DIR="${BLD_INSTALL_DIR:-${BUILDCLI_INSTALL_DIR:-${HOME}/.local/share/ingenium/bld}}"
BIN_DIR="${BUILDCLI_BIN_DIR:-${HOME}/.local/bin}"
SELF_CONTAINED="${BUILDCLI_SELF_CONTAINED:-true}"

log() {
	printf '==> %s\n' "$*"
}

fail() {
	printf 'error: %s\n' "$*" >&2
	exit 1
}

require() {
	command -v "$1" >/dev/null 2>&1 || fail "'$1' is required to install bld."
}

detect_rid() {
	local os arch
	os="$(uname -s | tr '[:upper:]' '[:lower:]')"
	arch="$(uname -m)"

	case "$os" in
		linux) os="linux" ;;
		darwin) os="osx" ;;
		mingw*|msys*|cygwin*) os="win" ;;
		*) fail "Unsupported operating system: $(uname -s)" ;;
	esac

	case "$arch" in
		x86_64|amd64) arch="x64" ;;
		arm64|aarch64) arch="arm64" ;;
		*) fail "Unsupported architecture: $arch" ;;
	esac

	printf '%s-%s\n' "$os" "$arch"
}

ensure_dotnet() {
	if command -v dotnet >/dev/null 2>&1; then
		return
	fi

	log "dotnet was not found; installing the .NET 8 SDK into ~/.dotnet"
	require curl
	curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0 --install-dir "${HOME}/.dotnet"
	export PATH="${HOME}/.dotnet:${HOME}/.dotnet/tools:${PATH}"
	command -v dotnet >/dev/null 2>&1 || fail "dotnet installation completed but the SDK is still not on PATH."
}

resolve_source() {
	local self="${BASH_SOURCE[0]:-}"
	if [[ -n "${self}" && -f "${self}" ]]; then
		local script_dir
		script_dir="$(cd "$(dirname "${self}")" && pwd)"
		if [[ -f "${script_dir}/../apps/Ingenium.BuildCli/Ingenium.BuildCli.csproj" ]]; then
			printf '%s\n' "$(cd "${script_dir}/.." && pwd)"
			return
		fi
	fi

	require git
	local checkout
	checkout="$(mktemp -d "${TMPDIR:-/tmp}/buildcli-src.XXXXXX")"
	log "Cloning ${REPO_URL}"
	git clone --depth 1 "${REPO_URL}" "${checkout}"
	printf '%s\n' "${checkout}"
}

main() {
	require git
	ensure_dotnet

	local source_dir rid configuration
	source_dir="$(resolve_source)"
	rid="$(detect_rid)"
	configuration="Release"

	log "Publishing bld for ${rid}"
	mkdir -p "${INSTALL_DIR}" "${BIN_DIR}"

	local publish_args=(
		dotnet publish "${source_dir}/apps/Ingenium.BuildCli/Ingenium.BuildCli.csproj"
		-c "${configuration}"
		-r "${rid}"
		-o "${INSTALL_DIR}"
		--nologo
	)

	if [[ "${SELF_CONTAINED}" == "true" ]]; then
		publish_args+=(--self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true)
	else
		publish_args+=(--self-contained false)
	fi

	"${publish_args[@]}"

	local executable="${INSTALL_DIR}/bld"
	[[ -f "${executable}" ]] || fail "Publish succeeded but ${executable} was not produced."
	chmod +x "${executable}"

	ln -sfn "${executable}" "${BIN_DIR}/bld"
	log "Installed ${executable}"
	log "Linked ${BIN_DIR}/bld"

	if ! command -v bld >/dev/null 2>&1; then
		cat <<EOF

bld is installed, but ${BIN_DIR} is not on your PATH.
Add the following to your shell profile:

  export PATH="${BIN_DIR}:\$PATH"

Then restart the shell and run: bld --help
EOF
	else
		log "bld is on PATH"
		bld --version || true
	fi
}

main "$@"
