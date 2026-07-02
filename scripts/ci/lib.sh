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

registry_login() {
  require_registry_vars
  [[ -n "$REGISTRY_USER" ]] || fail "REGISTRY_USER env var is required to push images"
  [[ -n "$REGISTRY_TOKEN" ]] || fail "REGISTRY_TOKEN env var is required to push images"
  log "Logging in to $REGISTRY as $REGISTRY_USER"
  echo "$REGISTRY_TOKEN" | docker login "$REGISTRY" -u "$REGISTRY_USER" --password-stdin
}
