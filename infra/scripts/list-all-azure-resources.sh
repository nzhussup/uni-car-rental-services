#!/usr/bin/env bash

set -euo pipefail

if ! command -v az >/dev/null 2>&1; then
  echo "Azure CLI is not installed or not on PATH." >&2
  exit 1
fi

if ! az account show >/dev/null 2>&1; then
  echo "You are not logged in to Azure CLI. Run 'az login' first." >&2
  exit 1
fi

echo "Subscriptions:"
az account list -o table
echo

mapfile -t subscriptions < <(az account list --query "[].id" -o tsv)

if [[ ${#subscriptions[@]} -eq 0 ]]; then
  echo "No Azure subscriptions found for the current account." >&2
  exit 1
fi

for subscription_id in "${subscriptions[@]}"; do
  subscription_name="$(
    az account show \
      --subscription "${subscription_id}" \
      --query "name" \
      -o tsv
  )"

  echo "Resources in subscription: ${subscription_name} (${subscription_id})"
  az resource list --subscription "${subscription_id}" -o table
  echo
done
