#!/usr/bin/env bash
# Deploys the freshly-pushed :qa images to the dedicated, network-isolated QA
# stack and recreates the miccheck database in an empty state. Safe/idempotent
# to re-run: `down -v` removes the Postgres data volume, and the API's own
# startup logic (EF Core migrations + idempotent dev seeding) rebuilds it.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")" && source ./lib.sh
cd "$CI_ROOT"
image_names
registry_login

export API_IMAGE ADMIN_IMAGE
export JWT_SECRET_KEY="${JWT_SECRET_KEY:?JWT_SECRET_KEY env var is required}"
export QA_ADMIN_PORT="${QA_ADMIN_PORT:-3001}"

COMPOSE="docker compose -p miccheck-qa -f deploy/qa/docker-compose.qa.yml"

log "Pulling latest :qa images"
$COMPOSE pull

log "Tearing down existing QA stack and wiping the database volume"
$COMPOSE down -v

log "Starting QA stack"
$COMPOSE up -d

log "Waiting for API health check via admin proxy on port $QA_ADMIN_PORT"
attempts=30
until curl -fsS "http://localhost:${QA_ADMIN_PORT}/api/v1/health" >/dev/null 2>&1; do
  attempts=$((attempts - 1))
  if [[ "$attempts" -le 0 ]]; then
    fail "QA stack did not become healthy in time"
  fi
  sleep 2
done

log "QA environment deployed and healthy at http://localhost:${QA_ADMIN_PORT}"
