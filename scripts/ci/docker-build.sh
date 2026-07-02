#!/usr/bin/env bash
# Builds self-contained CI images for the API and admin apps and tags them
# with both the current git sha and "qa" (the tag the QA compose stack pulls).
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")" && source ./lib.sh
cd "$CI_ROOT"
image_names

log "Building $API_IMAGE:$GIT_SHA / :qa"
docker build \
  -f src/api/MicCheck.Api/Dockerfile.ci \
  -t "$API_IMAGE:$GIT_SHA" \
  -t "$API_IMAGE:qa" \
  .

log "Building $ADMIN_IMAGE:$GIT_SHA / :qa"
docker build \
  -f src/admin/Dockerfile.ci \
  -t "$ADMIN_IMAGE:$GIT_SHA" \
  -t "$ADMIN_IMAGE:qa" \
  src/admin

log "docker-build.sh complete"
