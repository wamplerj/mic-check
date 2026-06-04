#!/usr/bin/env bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

build_api() {
  echo "Building API..."
  dotnet publish "$SCRIPT_DIR/src/api/MicCheck.Api/MicCheck.Api.csproj" -c Release -o "$SCRIPT_DIR/src/api/MicCheck.Api/publish"
  docker compose -f "$SCRIPT_DIR/docker-compose.yml" restart api
  echo "API done."
}

build_admin() {
  echo "Building admin..."
  npm --prefix "$SCRIPT_DIR/src/admin" ci --silent
  npm --prefix "$SCRIPT_DIR/src/admin" run build
  docker compose -f "$SCRIPT_DIR/docker-compose.yml" restart admin
  echo "Admin done."
}

case "${1:-all}" in
  api)   build_api ;;
  admin) build_admin ;;
  all)   build_api && build_admin ;;
  *)
    echo "Usage: $0 [api|admin|all]"
    exit 1
    ;;
esac
