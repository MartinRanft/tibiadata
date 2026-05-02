#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BASE_URL="${BASE_URL:-http://127.0.0.1:8097}"

run_k6() {
  local script_path="$1"

  if command -v k6 >/dev/null 2>&1; then
    BASE_URL="$BASE_URL" k6 run "$script_path"
    return
  fi

  docker run --rm --network host \
    -e BASE_URL="$BASE_URL" \
    -v "$ROOT_DIR/scripts:/scripts:ro" \
    grafana/k6:0.52.0 run "/scripts/$(basename "$script_path")"
}

run_k6 "$ROOT_DIR/scripts/load-public-api.js"
run_k6 "$ROOT_DIR/scripts/load-admin-abuse.js"
