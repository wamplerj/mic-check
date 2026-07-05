#!/usr/bin/env bash
# Compiles the API (and its dependents) and builds the admin SPA.
# Acts as the compile gate before tests/image builds run. TreatWarningsAsErrors
# is enabled across the solution, so this also fails on any compiler warning.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")" && source ./lib.sh
cd "$CI_ROOT"

ensure_dotnet

log "Restoring and publishing MicCheck.Api (Release)"
dotnet publish src/api/MicCheck.Api/MicCheck.Api.csproj -c Release

log "Installing admin dependencies (npm ci)"
npm --prefix src/admin ci

log "Building admin SPA (vite build)"
npm --prefix src/admin run build

log "build.sh complete"
