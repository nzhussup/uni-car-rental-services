#!/usr/bin/env bash

set -euo pipefail

DEFINED_SERVICES_PATH="$(dirname "$0")/../../.github/ci/services.json"

services="$(jq -r '.services[].name' "${DEFINED_SERVICES_PATH}")"$'\n'"car-rental-frontend"

echo "Updating all services:"
printf ' - %s\n' ${services}

for service in ${services}; do
  echo "Updating '${service}'..."
  "$(dirname "$0")/update-app.sh" "${service}"
done