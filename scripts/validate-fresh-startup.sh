#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
COMPOSE_FILE="$ROOT_DIR/compose.startup-validation.yaml"
LOG_DIR="$ROOT_DIR/.startup-validation"
APP_PID=""

cleanup() {
  if [[ -n "${APP_PID}" ]]; then
    kill "${APP_PID}" >/dev/null 2>&1 || true
    wait "${APP_PID}" >/dev/null 2>&1 || true
    APP_PID=""
  fi

  docker compose -f "$COMPOSE_FILE" down -v >/dev/null 2>&1 || true
}

wait_for_url() {
  local url="$1"
  local attempts="${2:-90}"

  for ((i=1; i<=attempts; i++)); do
    if curl -fsS "$url" >/dev/null 2>&1; then
      return 0
    fi

    sleep 2
  done

  return 1
}

run_validation() {
  local provider="$1"
  local app_url="$2"
  local connection_string="$3"
  local log_file="$LOG_DIR/${provider,,}-startup.log"

  mkdir -p "$LOG_DIR"

  ASPNETCORE_ENVIRONMENT=Development \
  ASPNETCORE_URLS="$app_url" \
  Database__Provider="$provider" \
  ConnectionStrings__DatabaseConnectionDev="$connection_string" \
  Caching__UseRedisForHybridCache=false \
  Caching__UseRedisForOutputCache=false \
  BackgroundJobs__ScheduledScraper__Enabled=false \
  BackgroundJobs__ItemImageSync__Enabled=false \
  BackgroundJobs__CreatureImageSync__Enabled=false \
  DataProtection__KeysDirectory="$LOG_DIR/dataprotection-${provider,,}" \
  dotnet run --project "$ROOT_DIR/TibiaDataApi.Api/TibiaDataApi.Api.csproj" --no-build --no-launch-profile >"$log_file" 2>&1 &

  APP_PID=$!

  if ! wait_for_url "$app_url/health/live"; then
    echo "Startup validation failed for $provider. See $log_file" >&2
    return 1
  fi

  if ! wait_for_url "$app_url/api/v1/categories/list"; then
    echo "Database-backed endpoint validation failed for $provider. See $log_file" >&2
    return 1
  fi

  kill "$APP_PID" >/dev/null 2>&1 || true
  wait "$APP_PID" >/dev/null 2>&1 || true
  APP_PID=""
}

ensure_sqlserver_database() {
  docker exec \
    tibiadataapi-tibiadataapi.startup-sqlserver-1 \
    /opt/mssql-tools18/bin/sqlcmd \
    -S 127.0.0.1 \
    -U sa \
    -P "StartupPass!22" \
    -Q "IF DB_ID(N'TibiaDataStartup') IS NULL CREATE DATABASE [TibiaDataStartup]" \
    -C >/dev/null
}

trap cleanup EXIT

docker compose -f "$COMPOSE_FILE" up -d --wait

ensure_sqlserver_database

run_validation \
  "MariaDb" \
  "http://127.0.0.1:58081" \
  "Server=127.0.0.1;Port=63306;Database=tibiadata_startup;User=startup;Password=StartupPass!22;charset=utf8mb4;Allow User Variables=True;"

run_validation \
  "SqlServer" \
  "http://127.0.0.1:58082" \
  "Server=127.0.0.1,61433;Database=TibiaDataStartup;User Id=sa;Password=StartupPass!22;TrustServerCertificate=True;Encrypt=False;"
