#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
GEN_SCRIPT="$ROOT_DIR/scripts/generate-openapi.sh"
COMPOSITE_OUTPUT="${1:-$ROOT_DIR/openapi.yaml}"
SERVICES_CONFIG="${OPENAPI_SERVICES_CONFIG:-$ROOT_DIR/scripts/openapi-services.json}"

if [[ ! -x "$GEN_SCRIPT" ]]; then
  echo "OpenAPI generator script not executable: $GEN_SCRIPT" >&2
  echo "Run: chmod +x scripts/generate-openapi.sh" >&2
  exit 1
fi

if ! command -v python3 >/dev/null 2>&1; then
  echo "python3 is required to compose multiple OpenAPI specs." >&2
  exit 1
fi

tmp_dir="$(mktemp -d)"
trap 'rm -rf "$tmp_dir"' EXIT

declare -a generated_json_specs=()
declare -a candidate_dirs=()

if [[ -f "$SERVICES_CONFIG" ]]; then
    echo "Using OpenAPI services config: $SERVICES_CONFIG"
    while IFS= read -r service_name; do
        [[ -n "$service_name" ]] || continue
        candidate_dirs+=("$ROOT_DIR/$service_name")
    done < <(python3 - "$SERVICES_CONFIG" <<'PY'
import json
import sys

path = sys.argv[1]
with open(path, 'r', encoding='utf-8') as f:
        config = json.load(f)

services = config.get('services', [])
for entry in services:
        if not isinstance(entry, dict):
                continue
        if entry.get('enabled', True) is False:
                continue
        name = entry.get('name')
        if isinstance(name, str) and name:
                print(name)
PY
)
else
    for dir in "$ROOT_DIR"/*; do
        [[ -d "$dir" ]] || continue
        name="$(basename "$dir")"
        case "$name" in
            .git|.github|.devtools|scripts)
                continue
                ;;
        esac
        candidate_dirs+=("$dir")
    done
fi

echo "Scanning projects under: $ROOT_DIR"
for dir in "${candidate_dirs[@]}"; do
  [[ -d "$dir" ]] || continue
  name="$(basename "$dir")"

  project_yaml="$dir/openapi.yaml"
  project_json="$tmp_dir/${name}.openapi.json"

  if "$GEN_SCRIPT" "$dir" "$project_yaml" >/dev/null 2>&1; then
    echo "Generated: $project_yaml"

    if "$GEN_SCRIPT" "$dir" "$project_json" >/dev/null 2>&1; then
            if [[ -s "$project_json" ]]; then
                generated_json_specs+=("$name|$project_json")
            else
                echo "Skipped JSON merge for $name (generated file missing or empty)."
            fi
    else
      echo "Skipped JSON generation for $name (needed for composite merge)."
    fi
  else
    echo "Skipped: $name (no supported OpenAPI generator detected)"
  fi
done

if [[ ${#generated_json_specs[@]} -eq 0 ]]; then
  echo "No OpenAPI specs were generated. Composite output not created." >&2
  exit 1
fi

mkdir -p "$(dirname "$COMPOSITE_OUTPUT")"

python3 - "$COMPOSITE_OUTPUT" "${generated_json_specs[@]}" <<'PY'
import copy
import json
import re
import sys

out_path = sys.argv[1]
spec_entries = sys.argv[2:]

composite = {
    "openapi": "3.0.3",
    "info": {
        "title": "Composite Services API",
        "version": "1.0.0"
    },
    "paths": {},
    "components": {
        "schemas": {},
        "responses": {},
        "parameters": {},
        "examples": {},
        "requestBodies": {},
        "headers": {},
        "securitySchemes": {},
        "links": {},
        "callbacks": {}
    },
    "tags": []
}

tag_names = set()


def rewrite_refs(node, ref_map):
    if isinstance(node, dict):
        updated = {}
        for k, v in node.items():
            if k == "$ref" and isinstance(v, str) and v in ref_map:
                updated[k] = ref_map[v]
            else:
                updated[k] = rewrite_refs(v, ref_map)
        return updated
    if isinstance(node, list):
        return [rewrite_refs(v, ref_map) for v in node]
    return node


for entry in spec_entries:
    service_name, spec_path = entry.split("|", 1)
    with open(spec_path, "r", encoding="utf-8") as f:
        spec = json.load(f)

    service_ref_map = {}
    service_components = spec.get("components") or {}
    if not isinstance(service_components, dict):
        service_components = {}

    for section, section_items in service_components.items():
        if not isinstance(section_items, dict):
            continue
        if section not in composite["components"]:
            composite["components"][section] = {}

        for item_name, item_value in section_items.items():
            target_name = item_name
            if target_name in composite["components"][section]:
                if composite["components"][section][target_name] == item_value:
                    continue
                target_name = f"{service_name}_{item_name}"
            composite["components"][section][target_name] = copy.deepcopy(item_value)
            if target_name != item_name:
                old_ref = f"#/components/{section}/{item_name}"
                new_ref = f"#/components/{section}/{target_name}"
                service_ref_map[old_ref] = new_ref

    service_paths = spec.get("paths") or {}
    if not isinstance(service_paths, dict):
        service_paths = {}
    service_paths = rewrite_refs(service_paths, service_ref_map)
    for path, path_item in service_paths.items():
        target_path = path
        if target_path in composite["paths"]:
            if composite["paths"][target_path] == path_item:
                continue
            target_path = f"/{service_name}{path}" if path.startswith("/") else f"/{service_name}/{path}"
        composite["paths"][target_path] = path_item

    for tag in (spec.get("tags") or []):
        if not isinstance(tag, dict):
            continue
        name = tag.get("name")
        if not name:
            continue
        if name in tag_names:
            continue
        tag_names.add(name)
        composite["tags"].append(tag)

# Remove empty component sections for cleaner output.
composite["components"] = {
    key: value for key, value in composite["components"].items() if value
}
if not composite["tags"]:
    composite.pop("tags", None)

safe_key_re = re.compile(r"^[A-Za-z_][A-Za-z0-9_.-]*$")


def yaml_key(key):
    if safe_key_re.match(key):
        return key
    return json.dumps(key)


def yaml_scalar(value):
    if value is None:
        return "null"
    if isinstance(value, bool):
        return "true" if value else "false"
    if isinstance(value, (int, float)):
        return str(value)
    return json.dumps(value)


def to_yaml(node, indent=0):
    sp = "  " * indent
    if isinstance(node, dict):
        if not node:
            return sp + "{}"
        lines = []
        for k, v in node.items():
            key = yaml_key(str(k))
            if isinstance(v, (dict, list)):
                if isinstance(v, dict) and not v:
                    lines.append(f"{sp}{key}: {{}}")
                elif isinstance(v, list) and not v:
                    lines.append(f"{sp}{key}: []")
                else:
                    lines.append(f"{sp}{key}:")
                    lines.append(to_yaml(v, indent + 1))
            else:
                lines.append(f"{sp}{key}: {yaml_scalar(v)}")
        return "\n".join(lines)

    if isinstance(node, list):
        if not node:
            return sp + "[]"
        lines = []
        for item in node:
            if isinstance(item, (dict, list)):
                if isinstance(item, dict) and not item:
                    lines.append(f"{sp}- {{}}")
                elif isinstance(item, list) and not item:
                    lines.append(f"{sp}- []")
                else:
                    lines.append(f"{sp}-")
                    lines.append(to_yaml(item, indent + 1))
            else:
                lines.append(f"{sp}- {yaml_scalar(item)}")
        return "\n".join(lines)

    return sp + yaml_scalar(node)


yaml_text = to_yaml(composite) + "\n"
with open(out_path, "w", encoding="utf-8") as f:
    f.write(yaml_text)
PY

echo "Composite OpenAPI generated at: $COMPOSITE_OUTPUT"
