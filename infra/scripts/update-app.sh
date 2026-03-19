#!/usr/bin/env bash

set -euo pipefail

if [[ $# -lt 1 || $# -gt 2 ]]; then
  echo "Usage: $0 <app-name> [resource-group]" >&2
  exit 1
fi

app_name="$1"
resource_group="${2:-car-rental-prod-rg}"
rollout_value="$(date +%s)"

echo "Forcing new revision for '${app_name}' in resource group '${resource_group}'..."

az containerapp update \
  --name "${app_name}" \
  --resource-group "${resource_group}" \
  --set-env-vars "REDEPLOYED_AT=${rollout_value}"
