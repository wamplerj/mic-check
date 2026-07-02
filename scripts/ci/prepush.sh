#!/usr/bin/env bash
# Husky pre-push hook body. Compiles everything and runs the fast test suites
# (no Docker, no registry) so broken code/tests never leave the workstation.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")" && source ./lib.sh
cd "$CI_ROOT"

log "Building full solution (Debug)"
dotnet build MicCheck.slnx -c Debug

log "Running MicCheck.Api.Tests.Unit"
dotnet test tests/api/MicCheck.Api.Tests.Unit/MicCheck.Api.Tests.Unit.csproj -c Debug

log "Installing admin dependencies (npm ci)"
npm --prefix src/admin ci

log "Running admin Jest tests"
npm --prefix src/admin test

log "Building admin SPA (vite build)"
npm --prefix src/admin run build

log "prepush.sh complete - ok to push"
