#!/usr/bin/env bash
# Pushes the images built by docker-build.sh (git-sha and qa tags) to the
# Gitea container registry.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")" && source ./lib.sh
cd "$CI_ROOT"
image_names
registry_login

for tag in "$GIT_SHA" qa; do
  log "Pushing $API_IMAGE:$tag"
  docker push "$API_IMAGE:$tag"
  log "Pushing $ADMIN_IMAGE:$tag"
  docker push "$ADMIN_IMAGE:$tag"
done

log "docker-push.sh complete"
