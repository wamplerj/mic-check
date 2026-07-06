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
export POSTGRES_PASSWORD="${POSTGRES_PASSWORD:?POSTGRES_PASSWORD env var is required}"
export QA_ADMIN_PORT="${QA_ADMIN_PORT:-3001}"

# Bind narrowly to docker's bridge gateway IP rather than 0.0.0.0: reachable
# from the smoke-qa job container (a sibling on the default bridge), but not
# exposed on the host's public interface.
export DB_BIND_HOST="$(docker_bridge_gateway)"
[[ -n "$DB_BIND_HOST" ]] || fail "could not determine docker bridge gateway IP to bind the QA db port"

COMPOSE="docker compose -p miccheck-qa -f deploy/qa/docker-compose.qa.yml"

log "Pulling latest :qa images"
$COMPOSE pull

log "Tearing down existing QA stack and wiping the database volume"
$COMPOSE down -v

log "Starting QA stack"
$COMPOSE up -d

log "Waiting for API health check via admin proxy"
# Checked with `docker compose exec` rather than curling the published host
# port: CI runs this script inside a runner container on its own bridge
# network, where "localhost:$QA_ADMIN_PORT" is the runner's own loopback, not
# the docker host's - it can never reach a host-published port. Exec'ing into
# the admin container and curling its own localhost sidesteps that entirely.
attempts=30
until $COMPOSE exec -T admin curl -fsS http://localhost/health >/dev/null 2>&1; do
  attempts=$((attempts - 1))
  if [[ "$attempts" -le 0 ]]; then
    fail "QA stack did not become healthy in time"
  fi
  sleep 2
done

log "QA environment deployed and healthy at http://localhost:${QA_ADMIN_PORT}"
