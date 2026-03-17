#!/usr/bin/env bash
set -euo pipefail

PROJECT_DIR="${1:-$PWD}"
PROJECT_DIR="$(cd -- "$PROJECT_DIR" && pwd)"
DEFAULT_OUTPUT="$PROJECT_DIR/openapi.yaml"
OUTPUT_PATH="${2:-$DEFAULT_OUTPUT}"
OPENAPI_DOC_NAME="${OPENAPI_DOC_NAME:-v1}"
SKIP_DB_MIGRATION_VALUE="${SKIP_DB_MIGRATION:-true}"

ext="${OUTPUT_PATH##*.}"
if [[ "$ext" == "json" ]]; then
  OUTPUT_FORMAT="json"
else
  OUTPUT_FORMAT="yaml"
fi

dotnet_reason=""
go_reason=""

convert_swagger2_to_openapi3() {
  local input_file="$1"
  local output_file="$2"
  local output_format="$3"
  local output_abs
  output_abs="$(cd -- "$(dirname -- "$output_file")" && pwd)/$(basename -- "$output_file")"

  local converter_dir
  converter_dir="$(mktemp -d)"

  cat >"$converter_dir/go.mod" <<'EOF'
module openapi-converter

go 1.24.1

require (
  github.com/getkin/kin-openapi v0.134.0
  sigs.k8s.io/yaml v1.4.0
)
EOF

  cat >"$converter_dir/main.go" <<'EOF'
package main

import (
  "encoding/json"
  "fmt"
  "os"

  "github.com/getkin/kin-openapi/openapi2"
  "github.com/getkin/kin-openapi/openapi2conv"
  "sigs.k8s.io/yaml"
)

func main() {
  if len(os.Args) != 4 {
    fmt.Fprintln(os.Stderr, "usage: converter <input-swagger2-json> <output-file> <yaml|json>")
    os.Exit(2)
  }

  inputPath := os.Args[1]
  outputPath := os.Args[2]
  outputFormat := os.Args[3]

  raw, err := os.ReadFile(inputPath)
  if err != nil {
    fmt.Fprintf(os.Stderr, "read input: %v\n", err)
    os.Exit(1)
  }

  var doc2 openapi2.T
  if err := json.Unmarshal(raw, &doc2); err != nil {
    fmt.Fprintf(os.Stderr, "parse swagger2 json: %v\n", err)
    os.Exit(1)
  }

  doc3, err := openapi2conv.ToV3(&doc2)
  if err != nil {
    fmt.Fprintf(os.Stderr, "convert swagger2->openapi3: %v\n", err)
    os.Exit(1)
  }

  outJSON, err := doc3.MarshalJSON()
  if err != nil {
    fmt.Fprintf(os.Stderr, "marshal openapi3 json: %v\n", err)
    os.Exit(1)
  }

  switch outputFormat {
  case "json":
    if err := os.WriteFile(outputPath, outJSON, 0o644); err != nil {
      fmt.Fprintf(os.Stderr, "write output json: %v\n", err)
      os.Exit(1)
    }
  case "yaml":
    outYAML, err := yaml.JSONToYAML(outJSON)
    if err != nil {
      fmt.Fprintf(os.Stderr, "convert json->yaml: %v\n", err)
      os.Exit(1)
    }
    if err := os.WriteFile(outputPath, outYAML, 0o644); err != nil {
      fmt.Fprintf(os.Stderr, "write output yaml: %v\n", err)
      os.Exit(1)
    }
  default:
    fmt.Fprintf(os.Stderr, "unknown output format: %s\n", outputFormat)
    os.Exit(2)
  }
}
EOF

  if ! (cd "$converter_dir" && go mod tidy >/dev/null 2>&1 && go run . "$input_file" "$output_abs" "$output_format"); then
    rm -rf "$converter_dir"
    return 1
  fi

  rm -rf "$converter_dir"
}

generate_dotnet() {
  if ! command -v dotnet >/dev/null 2>&1; then
    dotnet_reason="dotnet SDK not found on PATH"
    return 1
  fi

  mapfile -t web_projects < <(find "$PROJECT_DIR" -maxdepth 3 -type f -name "*.csproj" \
    -not -path "*/bin/*" -not -path "*/obj/*" -print0 | xargs -0 grep -l "Microsoft.NET.Sdk.Web" || true)

  if [[ ${#web_projects[@]} -eq 0 ]]; then
    dotnet_reason="no ASP.NET Core web project found"
    return 1
  fi

  if [[ ${#web_projects[@]} -gt 1 ]]; then
    echo "Multiple web projects found. Pass a narrower project directory as the first argument:" >&2
    printf '  - %s\n' "${web_projects[@]}" >&2
    return 1
  fi

  CSPROJ_PATH="${web_projects[0]}"
  CSPROJ_DIR="$(dirname -- "$CSPROJ_PATH")"
  ASSEMBLY_NAME="$(basename -- "$CSPROJ_PATH" .csproj)"

  local swagger_runner=""
  if (cd "$PROJECT_DIR" && dotnet tool run swagger --help >/dev/null 2>&1); then
    swagger_runner="dotnet-local"
    SWAGGER_CMD=(dotnet tool run swagger)
  elif command -v swagger >/dev/null 2>&1; then
    swagger_runner="global"
    SWAGGER_CMD=(swagger)
  else
    echo "Swagger CLI not found for .NET generation. Install one of these:" >&2
    echo "  dotnet new tool-manifest" >&2
    echo "  dotnet tool install Swashbuckle.AspNetCore.Cli --version 10.1.4" >&2
    echo "or install globally:" >&2
    echo "  dotnet tool install --global Swashbuckle.AspNetCore.Cli" >&2
    return 1
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
    return 1
  fi

  mkdir -p "$(dirname "$OUTPUT_PATH")"
  local tmp_output
  tmp_output="$(mktemp)"
  rm -f "$tmp_output"

  if [[ "$OUTPUT_FORMAT" == "yaml" ]]; then
    if [[ "$swagger_runner" == "dotnet-local" ]]; then
      (cd "$PROJECT_DIR" && SKIP_DB_MIGRATION="$SKIP_DB_MIGRATION_VALUE" "${SWAGGER_CMD[@]}" tofile \
        --yaml \
        --output "$tmp_output" \
        "$DLL_PATH" \
        "$OPENAPI_DOC_NAME")
    else
      SKIP_DB_MIGRATION="$SKIP_DB_MIGRATION_VALUE" "${SWAGGER_CMD[@]}" tofile \
        --yaml \
        --output "$tmp_output" \
        "$DLL_PATH" \
        "$OPENAPI_DOC_NAME"
    fi
  else
    if [[ "$swagger_runner" == "dotnet-local" ]]; then
      (cd "$PROJECT_DIR" && SKIP_DB_MIGRATION="$SKIP_DB_MIGRATION_VALUE" "${SWAGGER_CMD[@]}" tofile \
        --output "$tmp_output" \
        "$DLL_PATH" \
        "$OPENAPI_DOC_NAME")
    else
      SKIP_DB_MIGRATION="$SKIP_DB_MIGRATION_VALUE" "${SWAGGER_CMD[@]}" tofile \
        --output "$tmp_output" \
        "$DLL_PATH" \
        "$OPENAPI_DOC_NAME"
    fi
  fi

  if [[ ! -s "$tmp_output" ]]; then
    rm -f "$tmp_output"
    echo "Swagger CLI completed but did not produce output." >&2
    return 1
  fi

  mv "$tmp_output" "$OUTPUT_PATH"

  return 0
}

generate_go() {
  if [[ ! -f "$PROJECT_DIR/go.mod" ]]; then
    go_reason="no go.mod found"
    return 1
  fi

  local go_main="${GO_MAIN_FILE:-}"
  if [[ -z "$go_main" ]]; then
    if [[ -f "$PROJECT_DIR/cmd/server/main.go" ]]; then
      go_main="cmd/server/main.go"
    else
      local first_main
      first_main="$(find "$PROJECT_DIR" -maxdepth 4 -type f -path '*/cmd/*/main.go' | head -n1 || true)"
      if [[ -n "$first_main" ]]; then
        go_main="${first_main#"$PROJECT_DIR"/}"
      elif [[ -f "$PROJECT_DIR/main.go" ]]; then
        go_main="main.go"
      fi
    fi
  fi

  if [[ -z "$go_main" || ! -f "$PROJECT_DIR/$go_main" ]]; then
    go_reason="could not find Go main entrypoint (set GO_MAIN_FILE to override)"
    return 1
  fi

  if command -v swag >/dev/null 2>&1; then
    SWAG_CMD=(swag)
  elif command -v go >/dev/null 2>&1; then
    SWAG_CMD=(go run github.com/swaggo/swag/cmd/swag@latest)
  else
    go_reason="neither swag nor go command found"
    return 1
  fi

  local tmpdir
  tmpdir="$(mktemp -d)"

  (cd "$PROJECT_DIR" && "${SWAG_CMD[@]}" init \
    -g "$go_main" \
    --output "$tmpdir" \
    --parseInternal \
    --outputTypes "json")

  local generated_file="$tmpdir/swagger.json"
  if [[ ! -f "$generated_file" ]]; then
    echo "swag did not generate expected file: $generated_file" >&2
    rm -rf "$tmpdir"
    return 1
  fi

  mkdir -p "$(dirname "$OUTPUT_PATH")"

  if ! convert_swagger2_to_openapi3 "$generated_file" "$OUTPUT_PATH" "$OUTPUT_FORMAT"; then
    echo "failed to convert Go Swagger 2.0 output to OpenAPI 3.0" >&2
    rm -rf "$tmpdir"
    return 1
  fi

  if [[ ! -s "$OUTPUT_PATH" ]]; then
    echo "generated OpenAPI output file is missing or empty: $OUTPUT_PATH" >&2
    rm -rf "$tmpdir"
    return 1
  fi

  rm -rf "$tmpdir"

  return 0
}

if generate_dotnet; then
  if [[ ! -s "$OUTPUT_PATH" ]]; then
    echo "OpenAPI generation reported success but output file is missing or empty: $OUTPUT_PATH" >&2
    exit 1
  fi
  echo "OpenAPI generated (.NET) at: $OUTPUT_PATH"
  exit 0
fi

if generate_go; then
  echo "OpenAPI generated (Go) at: $OUTPUT_PATH"
  exit 0
fi

echo "OpenAPI generation failed for: $PROJECT_DIR" >&2
echo "  .NET probe: $dotnet_reason" >&2
echo "  Go probe: $go_reason" >&2
exit 1
