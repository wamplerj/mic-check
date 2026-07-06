#!/usr/bin/env bash
# Post-deploy validation for the QA environment: runs the API integration
# suite (real HTTP + real Postgres, seeding/cleaning up its own data) and the
# Playwright admin e2e suite against the just-deployed QA stack. Intended to
# run immediately after deploy-qa.sh, on the same self-hosted qa runner. This
# job runs in its own job container, a sibling of the QA stack's containers
# on docker's default bridge network - not "localhost" from the host's point
# of view - so it reaches published ports via the bridge gateway IP, same as
# deploy-qa.sh binds the db port to.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")" && source ./lib.sh
cd "$CI_ROOT"

ensure_dotnet
ensure_node

QA_ADMIN_PORT="${QA_ADMIN_PORT:-3001}"
CI_HOST="$(docker_bridge_gateway)"
[[ -n "$CI_HOST" ]] || fail "could not determine docker bridge gateway IP to reach the QA stack"
BASE_URL="http://${CI_HOST}:${QA_ADMIN_PORT}"

log "Resolved docker host as $CI_HOST for reaching the QA stack's published ports"

export MICCHECK_API_BASE_URL="$BASE_URL"
export MICCHECK_DB_CONNECTION_STRING="${MICCHECK_DB_CONNECTION_STRING:-Host=$CI_HOST;Port=55432;Database=miccheck;Username=miccheck;Password=${POSTGRES_PASSWORD:?POSTGRES_PASSWORD env var is required}}"

log "Running API integration suite against $BASE_URL"
dotnet test tests/api/MicCheck.Api.Tests.Integration/MicCheck.Api.Tests.Integration.csproj -c Release --logger trx

log "Installing admin e2e dependencies"
npm --prefix src/admin ci

# Playwright's bundled Chromium is a glibc binary and `--with-deps` only knows
# apt - neither works on this musl/Alpine runner. Run the e2e leg inside
# Microsoft's official Playwright image instead (glibc, browsers preinstalled
# at /ms-playwright), as a sibling container reachable at the same bridge
# gateway IP used above. Pin the image tag to the exact resolved
# @playwright/test version so the test runner and browser build match.
#
# This script itself runs inside the runner's own job container, talking to
# the host's docker daemon over a mounted socket (docker-outside-of-docker) -
# `docker run -v "$CI_ROOT:/work"` would ask the *host* daemon to bind-mount a
# path that only exists inside this job container, which fails. `docker cp`
# instead copies the files by content, sidestepping the path mismatch.
PW_VERSION="$(node -p "require('./src/admin/node_modules/@playwright/test/package.json').version")"
log "Running Playwright admin e2e suite against $BASE_URL (playwright:v$PW_VERSION-noble)"

PW_CONTAINER="$(docker create -e "E2E_BASE_URL=$BASE_URL" -w /work/src/admin "mcr.microsoft.com/playwright:v${PW_VERSION}-noble" npm run e2e)"
docker cp "$CI_ROOT/." "$PW_CONTAINER:/work"
pw_status=0
docker start -a "$PW_CONTAINER" || pw_status=$?
docker rm -f "$PW_CONTAINER" > /dev/null
[[ "$pw_status" -eq 0 ]] || fail "Playwright e2e suite failed (exit $pw_status)"

log "smoke-qa.sh complete - QA environment validated end to end"
