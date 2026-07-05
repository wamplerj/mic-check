#!/usr/bin/env bash
# Runs the fast test suites: .NET unit tests (EF InMemory, no DB/Docker needed)
# and the admin Jest suite. Integration tests are intentionally excluded here -
# they require a live API + Postgres (see readme in the Integration test project).
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")" && source ./lib.sh
cd "$CI_ROOT"

ensure_dotnet

# --results-directory doesn't clear prior runs - it adds a new GUID folder
# alongside old ones every time. On a runner that reuses its workspace
# (self-hosted, unlike GitHub's ephemeral ones), stale coverage from past
# runs would otherwise get merged in by coverage.sh and silently skew the
# combined percentage.
rm -rf "$CI_ROOT/coverage/dotnet"

log "Running MicCheck.Api.Tests.Unit"
dotnet test tests/api/MicCheck.Api.Tests.Unit/MicCheck.Api.Tests.Unit.csproj -c Release --logger trx \
  --collect:"XPlat Code Coverage" --results-directory "$CI_ROOT/coverage/dotnet" \
  --settings tests/api/MicCheck.Api.Tests.Unit/coverlet.runsettings

log "Running admin Jest tests"
npm --prefix src/admin test -- --coverage --coverageReporters=lcov --coverageReporters=text-summary

log "test.sh complete"
