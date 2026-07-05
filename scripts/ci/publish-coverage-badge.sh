#!/usr/bin/env bash
# Commits the coverage badge refreshed by coverage.sh straight back to the
# branch that triggered this run, so readme.md's relative badges/coverage.svg
# link stays current. GITHUB_SERVER_URL/GITHUB_REPOSITORY/GITHUB_REF_NAME are
# default context env vars on both GitHub Actions and Gitea Actions (Gitea's
# engine is GitHub-Actions-compatible); GITHUB_TOKEN must be passed in
# explicitly from the workflow (${{ github.token }}) on both platforms.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")" && source ./lib.sh
cd "$CI_ROOT"

[[ -n "${GITHUB_TOKEN:-}" ]] || fail "GITHUB_TOKEN env var is required to push the badge commit"
[[ -n "${GITHUB_SERVER_URL:-}" ]] || fail "GITHUB_SERVER_URL env var is required to push the badge commit"
[[ -n "${GITHUB_REPOSITORY:-}" ]] || fail "GITHUB_REPOSITORY env var is required to push the badge commit"
[[ -n "${GITHUB_REF_NAME:-}" ]] || fail "GITHUB_REF_NAME env var is required to push the badge commit"

if git diff --quiet -- badges/coverage.svg; then
  log "badges/coverage.svg unchanged; nothing to publish"
  exit 0
fi

git config user.name "miccheck-ci"
git config user.email "ci@miccheck.local"
git add badges/coverage.svg
git commit -m "chore: refresh coverage badge [skip ci]"

remote_url="${GITHUB_SERVER_URL}/${GITHUB_REPOSITORY}.git"
git -c http.extraheader="AUTHORIZATION: bearer ${GITHUB_TOKEN}" push "$remote_url" "HEAD:${GITHUB_REF_NAME}"

log "publish-coverage-badge.sh complete"
