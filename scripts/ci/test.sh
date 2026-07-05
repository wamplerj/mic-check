#!/usr/bin/env bash
# Runs the fast test suites: .NET unit tests (EF InMemory, no DB/Docker needed)
# and the admin Jest suite. Integration tests are intentionally excluded here -
# they require a live API + Postgres (see readme in the Integration test project).
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")" && source ./lib.sh
cd "$CI_ROOT"

ensure_dotnet

log "Running MicCheck.Api.Tests.Unit"
dotnet test tests/api/MicCheck.Api.Tests.Unit/MicCheck.Api.Tests.Unit.csproj -c Release --logger trx

log "Running admin Jest tests"
npm --prefix src/admin test

log "test.sh complete"
