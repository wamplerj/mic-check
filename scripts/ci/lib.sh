#!/usr/bin/env bash
# Shared helpers for scripts/ci/*.sh.
# Every script in this directory is meant to run identically in CI and on a
# developer's machine - no Gitea/GitHub-specific built-in actions, just bash.
set -euo pipefail

log() {
  echo "[$(date -u +'%Y-%m-%dT%H:%M:%SZ')] $*"
}

fail() {
  echo "[$(date -u +'%Y-%m-%dT%H:%M:%SZ')] ERROR: $*" >&2
  exit 1
}

# Repo root, regardless of caller's cwd.
CI_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

# Registry configuration. All values come from the environment (CI secrets or
# a developer's shell) - nothing is hardcoded, per project convention.
REGISTRY="${REGISTRY:-}"
REGISTRY_OWNER="${REGISTRY_OWNER:-}"
REGISTRY_USER="${REGISTRY_USER:-}"
REGISTRY_TOKEN="${REGISTRY_TOKEN:-}"

GIT_SHA="$(git -C "$CI_ROOT" rev-parse --short HEAD)"

require_registry_vars() {
  [[ -n "$REGISTRY" ]] || fail "REGISTRY env var is required (e.g. gitea.example.com)"
  [[ -n "$REGISTRY_OWNER" ]] || fail "REGISTRY_OWNER env var is required (e.g. your gitea org/user)"
}

# Populates API_IMAGE / ADMIN_IMAGE, e.g. gitea.example.com/james/miccheck-api
image_names() {
  require_registry_vars
  API_IMAGE="$REGISTRY/$REGISTRY_OWNER/miccheck-api"
  ADMIN_IMAGE="$REGISTRY/$REGISTRY_OWNER/miccheck-admin"
}

# Installs .NET into $CI_ROOT/.dotnet via the vendored dotnet-install.sh if
# `dotnet` isn't already on PATH, then prepends it to PATH for this process.
# Keeps bare runners (no SDK preinstalled) working the same as a dev machine.
ensure_dotnet() {
  if command -v dotnet > /dev/null 2>&1; then
    return 0
  fi

  local install_dir="$CI_ROOT/.dotnet"
  if [[ ! -x "$install_dir/dotnet" ]]; then
    log "dotnet not found on PATH; installing .NET SDK via dotnet-install.sh"
    bash "$CI_ROOT/dotnet-install.sh" --channel LTS --install-dir "$install_dir"
  fi
  export PATH="$install_dir:$PATH"
  export DOTNET_ROOT="$install_dir"

  ensure_dotnet_native_deps
}

# The .NET runtime is a native ELF binary that dynamically links libstdc++/libgcc.
# Bare/minimal images (e.g. the act hostexecutor container) may lack them entirely,
# which fails as an obscure symbol-relocation error rather than "command not found".
ensure_dotnet_native_deps() {
  if ldconfig -p 2>/dev/null | grep -q 'libstdc++\.so\.6'; then
    return 0
  fi

  log "libstdc++.so.6 missing; installing native runtime deps for dotnet"
  local sudo_cmd=""
  if [[ "$(id -u)" -ne 0 ]] && command -v sudo > /dev/null 2>&1; then
    sudo_cmd="sudo"
  fi

  if command -v apt-get > /dev/null 2>&1; then
    $sudo_cmd apt-get update -y
    $sudo_cmd apt-get install -y --no-install-recommends libstdc++6 libgcc-s1 libicu-dev ca-certificates
  elif command -v apk > /dev/null 2>&1; then
    # Alpine/musl runner - dotnet-install.sh falls back to the linux-musl-x64 SDK
    # here, which still wants a real libstdc++/libgcc (not just gcompat).
    $sudo_cmd apk add --no-cache libstdc++ libgcc icu-libs ca-certificates
  else
    fail "libstdc++.so.6 missing and neither apt-get nor apk is available; install a C++ runtime manually on this runner"
  fi
}

# Installs Node.js into $CI_ROOT/.node from the official prebuilt tarball if
# `npm` isn't already on PATH, then prepends it to PATH for this process.
# Mirrors ensure_dotnet() above - keeps bare runners (no Node preinstalled,
# e.g. the self-hosted qa runner) working the same as a dev machine.
ensure_node() {
  if command -v npm > /dev/null 2>&1; then
    return 0
  fi

  # nodejs.org only ships glibc binaries; on a musl/Alpine runner (same one
  # dotnet-install.sh detects and picks the linux-musl-x64 SDK for) that
  # tarball fails to exec at all ("env: can't execute 'node'"). Prefer the
  # distro's own package on musl instead of a broken glibc download.
  if [[ ! -x "$CI_ROOT/.node/bin/node" ]] && command -v apk > /dev/null 2>&1; then
    log "node not found on PATH; installing via apk (musl runner)"
    local sudo_cmd=""
    if [[ "$(id -u)" -ne 0 ]] && command -v sudo > /dev/null 2>&1; then
      sudo_cmd="sudo"
    fi
    $sudo_cmd apk add --no-cache nodejs npm
    return 0
  fi

  local node_version="22.14.0"
  local install_dir="$CI_ROOT/.node"
  if [[ ! -x "$install_dir/bin/node" ]]; then
    log "node not found on PATH; installing Node.js v$node_version"
    local tarball="node-v${node_version}-linux-x64"
    local url="https://nodejs.org/dist/v${node_version}/${tarball}.tar.xz"
    if command -v curl > /dev/null 2>&1; then
      curl -fsSL "$url" -o "/tmp/${tarball}.tar.xz"
    elif command -v wget > /dev/null 2>&1; then
      wget -q "$url" -O "/tmp/${tarball}.tar.xz"
    else
      fail "neither curl nor wget found on PATH; cannot download Node.js"
    fi
    mkdir -p "$install_dir"
    tar -xJf "/tmp/${tarball}.tar.xz" -C "$install_dir" --strip-components=1
    rm -f "/tmp/${tarball}.tar.xz"
  fi
  export PATH="$install_dir/bin:$PATH"
}

# Installs the dotnet-reportgenerator-globaltool CLI (merges coverlet/Jest
# coverage output into badges + build-summary markdown) into $CI_ROOT/.dotnet-tools
# if it isn't already on PATH. Mirrors ensure_dotnet()/ensure_node() above.
ensure_reportgenerator() {
  if command -v reportgenerator > /dev/null 2>&1; then
    return 0
  fi

  local tool_dir="$CI_ROOT/.dotnet-tools"
  if [[ ! -x "$tool_dir/reportgenerator" ]]; then
    log "reportgenerator not found on PATH; installing dotnet-reportgenerator-globaltool"
    dotnet tool install dotnet-reportgenerator-globaltool --tool-path "$tool_dir"
  fi
  export PATH="$tool_dir:$PATH"
}

# The IP address on which a container published on 0.0.0.0/<gateway-ip> is
# reachable from a sibling container on docker's default bridge network (i.e.
# the docker host's bridge-side address, not its public interface). Used to
# bind QA's db port narrowly - reachable by the smoke-qa job container, not
# exposed off-box the way 0.0.0.0 would be.
docker_bridge_gateway() {
  docker network inspect bridge -f '{{(index .IPAM.Config 0).Gateway}}' 2>/dev/null \
    || ip route show default 2>/dev/null | awk '/default/ {print $3; exit}'
}

registry_login() {
  require_registry_vars
  [[ -n "$REGISTRY_USER" ]] || fail "REGISTRY_USER env var is required to push images"
  [[ -n "$REGISTRY_TOKEN" ]] || fail "REGISTRY_TOKEN env var is required to push images"
  log "Logging in to $REGISTRY as $REGISTRY_USER"
  echo "$REGISTRY_TOKEN" | docker login "$REGISTRY" -u "$REGISTRY_USER" --password-stdin
}
