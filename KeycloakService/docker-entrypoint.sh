#!/bin/bash
set -euo pipefail

frontend_url="${KEYCLOAK_FRONTEND_URL:-http://localhost:5173}"
ssl_required="${KEYCLOAK_SSL_REQUIRED:-external}"
import_mode="${KEYCLOAK_IMPORT_MODE:-prod}"
realm_template_name="${KEYCLOAK_REALM_TEMPLATE:-}"
realm_name="${KEYCLOAK_REALM_NAME:-}"
import_override="${KEYCLOAK_IMPORT_OVERRIDE:-false}"

if [[ -z "$realm_template_name" ]]; then
  if [[ "$import_mode" == "dev" ]]; then
    realm_template_name="car-rental-dev-realm.json"
  else
    realm_template_name="car-rental-prod-realm.json"
  fi
fi

if [[ -z "$realm_name" ]]; then
  if [[ "$import_mode" == "dev" ]]; then
    realm_name="car-rental-dev"
  else
    realm_name="car-rental-prod"
  fi
fi

realm_template="/opt/keycloak/realm-template/${realm_template_name}"
rendered_realm_file="/tmp/${realm_name}.json"

escape_sed_replacement() {
  printf '%s' "$1" | sed -e 's/[\\/&]/\\&/g'
}

render_realm_import() {
  local escaped_realm_name escaped_frontend_url escaped_ssl_required

  if [[ ! -f "$realm_template" ]]; then
    echo "Realm template not found: $realm_template" >&2
    exit 1
  fi

  if [[ "$import_mode" == "prod" && "$frontend_url" == "http://localhost:5173" ]]; then
    echo "KEYCLOAK_FRONTEND_URL must be set to the public frontend URL for production imports." >&2
    exit 1
  fi

  escaped_realm_name="$(escape_sed_replacement "$realm_name")"
  escaped_frontend_url="$(escape_sed_replacement "$frontend_url")"
  escaped_ssl_required="$(escape_sed_replacement "$ssl_required")"

  sed \
    -e "s/car-rental-dev/${escaped_realm_name}/g" \
    -e "s|http://localhost:5173|${escaped_frontend_url}|g" \
    -e "s|https://replace-with-your-frontend-domain|${escaped_frontend_url}|g" \
    -e "s/\"sslRequired\": \"none\"/\"sslRequired\": \"${escaped_ssl_required}\"/" \
    "$realm_template" > "$rendered_realm_file"
}

import_realm() {
  local args
  args=(import "--file=${rendered_realm_file}" "--override=${import_override}")

  /opt/keycloak/bin/kc.sh "${args[@]}"
}

start_dev() {
  exec /opt/keycloak/bin/kc.sh start-dev \
    --http-enabled=true \
    --hostname-strict=false
}

start_prod() {
  local args
  args=(start --optimized)

  if [[ -n "${KC_PROXY_HEADERS:-}" ]]; then
    args+=("--proxy-headers=${KC_PROXY_HEADERS}")
  fi

  if [[ -n "${KC_HOSTNAME:-}" ]]; then
    args+=("--hostname=${KC_HOSTNAME}")
  fi

  if [[ -n "${KC_HOSTNAME_ADMIN:-}" ]]; then
    args+=("--hostname-admin=${KC_HOSTNAME_ADMIN}")
  fi

  if [[ -n "${KC_HOSTNAME_STRICT:-}" ]]; then
    args+=("--hostname-strict=${KC_HOSTNAME_STRICT}")
  fi

  if [[ "${KC_HTTP_ENABLED:-true}" == "true" ]]; then
    args+=("--http-enabled=true")
  fi

  exec /opt/keycloak/bin/kc.sh "${args[@]}"
}

render_realm_import
import_realm

if [[ "$import_mode" == "dev" ]]; then
  start_dev
fi

start_prod
