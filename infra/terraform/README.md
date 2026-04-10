# Terraform Infrastructure

This directory contains the production infrastructure definition for the car rental platform on Azure.

## What We Use

- Terraform with the `azurerm` provider
- Azure Resource Group
- Azure Log Analytics Workspace
- Azure Container Apps Environment
- Azure Container Apps for all runtime services
- Azure SQL Database for application data and Keycloak data
- GitHub Container Registry (`ghcr.io`) for container images
- GitHub Actions for build and deployment orchestration

## Resources Provisioned

The root Terraform stack creates these Azure resource types:

- `azurerm_resource_group`
- `azurerm_log_analytics_workspace`
- `azurerm_container_app_environment`
- `azurerm_mssql_server`
- `azurerm_mssql_firewall_rule`
- `azurerm_mssql_database`
- `azurerm_container_app`

## Naming

Resource naming is based on:

- `project_name`, default: `car-rental`
- `environment`, default: `prod`

With the current defaults, Terraform derives names like:

- Resource group: `car-rental-prod-rg`
- SQL server: `car-rental-prod-sql`
- Log Analytics workspace: `car-rental-prod-law`
- Container Apps environment: `car-rental-prod-aca-env`

The default Terraform region in this repo is `germanywestcentral`. That can be overridden through input variables.

## Container Apps

Terraform provisions these Container Apps:

- `car-rental-frontend`
- `nginx-gateway`
- `car-rental-service`
- `request-proxy-service`
- `currency-converter-service`
- `keycloak-service`

Current runtime shape:

- External apps:
  - `car-rental-frontend`
  - `nginx-gateway`
  - `keycloak-service`
- Internal-only apps:
  - `car-rental-service`
  - `request-proxy-service`
  - `currency-converter-service`
- Revision mode: `Single`
- Scaling:
  - All apps currently use `min_replicas = 0`
  - All apps currently use `max_replicas = 1`

Current CPU and memory settings:

- `car-rental-frontend`: `0.25` CPU, `0.5Gi`
- `nginx-gateway`: `0.25` CPU, `0.5Gi`
- `car-rental-service`: `0.5` CPU, `1Gi`
- `request-proxy-service`: `0.25` CPU, `0.5Gi`
- `currency-converter-service`: `0.25` CPU, `0.5Gi`
- `keycloak-service`: `0.5` CPU, `1Gi`

## Traffic Topology

The deployed app topology is:

- Browser -> `car-rental-frontend`
- Browser/API client -> `nginx-gateway`
- Browser -> `keycloak-service`
- `nginx-gateway` -> `car-rental-service`
- `nginx-gateway` -> `request-proxy-service`
- `car-rental-service` -> `currency-converter-service`

The backend services are intended to stay private inside the Container Apps environment. Public access is exposed only through:

- the frontend app
- the NGINX gateway
- Keycloak

## Database Stack

Terraform provisions one Azure SQL logical server and two single databases:

- SQL server: derived from `${project_name}-${environment}-sql`
- Application database: `CarRentalDB`
- Keycloak database: `KeycloakDB`

Current database configuration:

- SKU: `GP_S_Gen5_1`
- Tier: General Purpose
- Compute model: Serverless
- Max size: `32 GB`
- Min capacity: `0.5`
- Auto-pause delay: `60` minutes
- Zone redundancy: disabled
- Public network access: enabled
- Firewall rule: `AllowAzureServices`

## Application Configuration and Secrets

Terraform injects runtime configuration directly into Container Apps.

Non-secret environment values currently include:

- `NODE_ENV` for the frontend
- `VITE_API_BASE_URL`, `VITE_KEYCLOAK_URL`, `VITE_KEYCLOAK_REALM`, `VITE_KEYCLOAK_CLIENT_ID` for frontend runtime config injection
- `VITE_API_BASE_URL` and `VITE_KEYCLOAK_URL` are derived automatically from the `nginx-gateway` and `keycloak-service` Container App hostnames
- `CORS_ALLOWED_ORIGIN` for `nginx-gateway`
- `ASPNETCORE_ENVIRONMENT` and `CURRENCY_CONVERTER_BASE_URL` for `car-rental-service`
- Keycloak runtime configuration such as `KC_DB`, `KC_DB_URL`, `KEYCLOAK_FRONTEND_URL`, and realm import settings

Secret environment values currently include:

- `ConnectionStrings__DefaultConnection` for `car-rental-service`
- `GOOGLE_MAPS_API_KEY` and `PEXELS_API_KEY` for `request-proxy-service`
- `KC_DB_PASSWORD`, `KC_BOOTSTRAP_ADMIN_USERNAME`, and `KC_BOOTSTRAP_ADMIN_PASSWORD` for `keycloak-service`
- GHCR registry credentials for image pulls

## Frontend Runtime Configuration

Frontend runtime settings are injected as Container App environment variables by Terraform, then rendered into `/app-config.js` by the frontend NGINX entrypoint on container start.

This means frontend Docker builds do not require `VITE_*` build secrets or GitHub repository variables anymore.

## Images and Registry

All application images are pulled from `ghcr.io`.

The Terraform stack expects immutable image tags to be passed in through the `image_tags` variable for:

- frontend
- nginx gateway
- car rental service
- request proxy service
- currency converter service
- keycloak

The current GHCR organization/user configured in Terraform locals is `timmy-2003`.

## Outputs

Terraform currently exposes these outputs:

- resource group name
- Container Apps environment name
- SQL server FQDN
- latest FQDN for each Container App

## Deployment Flow

The current delivery model in this repo is:

1. GitHub Actions builds and pushes service images to GHCR.
2. The deploy workflow logs into Azure.
3. The deploy step loops through service names from `.github/ci/services.json`.
4. Each service is rolled out by calling `infra/scripts/update-app.sh <service-name>`.

`update-app.sh` forces a new Container App revision by updating the `REDEPLOYED_AT` environment variable.

There is also a convenience script at `infra/scripts/update-all.sh` for local/manual rollout of all configured services.

## Operational Notes

- Keeping `min_replicas = 0` helps Container Apps scale to zero when idle.
- Azure SQL is still billable while active; continuously running apps can keep serverless SQL awake.
- Keycloak is intentionally public and separate from the NGINX gateway because browser auth flows must reach it directly.
- Internal service-to-service communication relies on the Container Apps environment network, not public ingress.
