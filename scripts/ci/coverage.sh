#!/usr/bin/env bash
# Merges the .NET (coverlet/Cobertura) and admin (Jest/lcov) coverage output
# produced by test.sh into one report via reportgenerator, prints a summary,
# appends a build-report summary when running under Actions, and refreshes
# the coverage badge committed at badges/coverage.svg. Readme embeds that
# badge via a relative path, which resolves on both GitHub and Gitea since
# the same repo content is pushed to both remotes.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")" && source ./lib.sh
cd "$CI_ROOT"

ensure_dotnet
ensure_reportgenerator

REPORT_DIR="$CI_ROOT/coverage/report"

log "Merging coverage reports with reportgenerator"
reportgenerator \
  -reports:"coverage/dotnet/**/coverage.cobertura.xml;src/admin/coverage/lcov.info" \
  -targetdir:"$REPORT_DIR" \
  -reporttypes:"Badges;MarkdownSummaryGithub;TextSummary"

cat "$REPORT_DIR/Summary.txt"

if [[ -n "${GITHUB_STEP_SUMMARY:-}" ]]; then
  cat "$REPORT_DIR/SummaryGithub.md" >> "$GITHUB_STEP_SUMMARY"
fi

mkdir -p "$CI_ROOT/badges"
cp "$REPORT_DIR/badge_linecoverage.svg" "$CI_ROOT/badges/coverage.svg"

log "coverage.sh complete"
