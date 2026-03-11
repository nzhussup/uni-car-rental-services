#!/usr/bin/env bash
set -euo pipefail

PROJECT_DIR="${1:-$PWD}"
PROJECT_DIR="$(cd -- "$PROJECT_DIR" && pwd)"
DEFAULT_OUTPUT="$PROJECT_DIR/openapi.yaml"
OUTPUT_PATH="${2:-$DEFAULT_OUTPUT}"
OPENAPI_DOC_NAME="${OPENAPI_DOC_NAME:-v1}"
SKIP_DB_MIGRATION_VALUE="${SKIP_DB_MIGRATION:-true}"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet SDK is required but not found on PATH." >&2
  exit 1
fi

mapfile -t web_projects < <(find "$PROJECT_DIR" -maxdepth 3 -type f -name "*.csproj" \
  -not -path "*/bin/*" -not -path "*/obj/*" -print0 | xargs -0 grep -l "Microsoft.NET.Sdk.Web" || true)

if [[ ${#web_projects[@]} -eq 0 ]]; then
  echo "No ASP.NET Core web project (.csproj with Microsoft.NET.Sdk.Web) found under: $PROJECT_DIR" >&2
  echo "OpenAPI generation is supported only for web projects." >&2
  exit 1
fi

if [[ ${#web_projects[@]} -gt 1 ]]; then
  echo "Multiple web projects found. Pass the target project directory as the first argument:" >&2
  printf '  - %s\n' "${web_projects[@]}" >&2
  exit 1
fi

CSPROJ_PATH="${web_projects[0]}"
CSPROJ_DIR="$(dirname -- "$CSPROJ_PATH")"
ASSEMBLY_NAME="$(basename -- "$CSPROJ_PATH" .csproj)"

if dotnet tool restore >/dev/null 2>&1; then
  SWAGGER_CMD=(dotnet tool run swagger)
elif command -v swagger >/dev/null 2>&1; then
  SWAGGER_CMD=(swagger)
else
  echo "Swagger CLI not found. Install one of these:" >&2
  echo "  dotnet new tool-manifest" >&2
  echo "  dotnet tool install Swashbuckle.AspNetCore.Cli --version 10.1.4" >&2
  echo "or install globally:" >&2
  echo "  dotnet tool install --global Swashbuckle.AspNetCore.Cli" >&2
  exit 1
fi

TARGET_FRAMEWORK="$(sed -n 's:.*<TargetFramework>\(.*\)</TargetFramework>.*:\1:p' "$CSPROJ_PATH" | head -n1)"
if [[ -z "$TARGET_FRAMEWORK" ]]; then
  TARGET_FRAMEWORKS="$(sed -n 's:.*<TargetFrameworks>\(.*\)</TargetFrameworks>.*:\1:p' "$CSPROJ_PATH" | head -n1)"
  TARGET_FRAMEWORK="${TARGET_FRAMEWORKS%%;*}"
fi

dotnet build "$CSPROJ_PATH"

if [[ -n "$TARGET_FRAMEWORK" ]]; then
  DLL_PATH="$CSPROJ_DIR/bin/Debug/$TARGET_FRAMEWORK/$ASSEMBLY_NAME.dll"
else
  DLL_PATH="$(find "$CSPROJ_DIR/bin/Debug" -type f -name "$ASSEMBLY_NAME.dll" | head -n1)"
fi

if [[ -z "${DLL_PATH:-}" || ! -f "$DLL_PATH" ]]; then
  echo "Could not locate compiled assembly for $ASSEMBLY_NAME after build." >&2
  exit 1
fi

mkdir -p "$(dirname "$OUTPUT_PATH")"

SKIP_DB_MIGRATION="$SKIP_DB_MIGRATION_VALUE" "${SWAGGER_CMD[@]}" tofile \
  --yaml \
  --output "$OUTPUT_PATH" \
  "$DLL_PATH" \
  "$OPENAPI_DOC_NAME"

echo "OpenAPI generated at: $OUTPUT_PATH"
