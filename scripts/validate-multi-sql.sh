#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
COMPOSE_FILE="$ROOT_DIR/compose.multi-sql.yaml"

cleanup() {
  docker compose -f "$COMPOSE_FILE" down -v >/dev/null 2>&1 || true
}

trap cleanup EXIT

docker compose -f "$COMPOSE_FILE" up -d --wait

export TIBIADATA_VALIDATION_MARIADB_CONNECTION="Server=127.0.0.1;Port=53306;Database=tibiadata_validation;User=validation;Password=ValidationPass!22;charset=utf8mb4;Allow User Variables=True;"
export TIBIADATA_VALIDATION_POSTGRES_CONNECTION="Host=127.0.0.1;Port=55432;Database=tibiadata_validation;Username=validation;Password=ValidationPass!22;"
export TIBIADATA_VALIDATION_SQLSERVER_CONNECTION="Server=127.0.0.1,51433;Database=TibiaDataValidation;User Id=sa;Password=ValidationPass!22;TrustServerCertificate=True;Encrypt=False;"

dotnet test "$ROOT_DIR/TibiaDataApi.Services.Tests/TibiaDataApi.Services.Tests.csproj" --filter MultiSqlCompatibilitySmokeTests
