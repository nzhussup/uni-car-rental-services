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

- a short environment code derived from `environment`
- `deployed_stacks` in `main.tf`, for example `["v1", "v2"]`

Current naming pattern examples:

- Shared resource group: `cr-p-shared-rg`
- Shared Log Analytics workspace: `cr-p-law`
- Shared Container Apps environment: `cr-p-acae`
- `v1` SQL server: `cr-p-v1-sql`
- `v2` SQL server: `cr-p-v2-sql`
- `v1` frontend app: `cr-p-v1-fe`
- `v2` booking app: `cr-p-v2-bkg`

The default Terraform region in this repo is `germanywestcentral`. That can be overridden through input variables.

## Container Apps

Terraform provisions one shared Container Apps environment and then deploys versioned apps into it.

Current stack topologies:

- `v1`
  - frontend
  - gateway
  - monolith API
  - request proxy
  - currency converter
  - keycloak
- `v2`
  - frontend
  - gateway
  - car service
  - booking service
  - request proxy
  - currency converter
  - keycloak
  - rabbitmq
  - redis

Current runtime shape:

- External apps:
  - `car-rental-frontend`
  - `nginx-gateway`
  - `keycloak-service`
- Internal-only apps:
  - `booking-service`
  - `car-rental-service`
  - `request-proxy-service`
  - `currency-converter-service`
  - `rabbitmq`
  - `redis`
- Revision mode: `Single`
- Scaling:
  - All apps currently use `min_replicas = 1`
  - All apps currently use `max_replicas = 1`

Current CPU and memory settings:

- `car-rental-frontend`: `0.25` CPU, `0.5Gi`
- `nginx-gateway`: `0.25` CPU, `0.5Gi`
- `booking-service`: `0.5` CPU, `1Gi`
- `car-rental-service`: `0.5` CPU, `1Gi`
- `request-proxy-service`: `0.25` CPU, `0.5Gi`
- `currency-converter-service`: `0.25` CPU, `0.5Gi`
- `rabbitmq`: `0.5` CPU, `1Gi`
- `redis`: `0.25` CPU, `0.5Gi`
- `keycloak-service`: `0.5` CPU, `1Gi`

## Traffic Topology

The deployed app topology is version-dependent:

- `v1`: gateway -> monolith API / request proxy
- `v1`: monolith API -> currency converter
- `v2`: gateway -> car service / booking service / request proxy
- `v2`: car service <-> rabbitmq
- `v2`: booking service <-> rabbitmq
- `v2`: car service -> currency converter
- `v2`: currency converter -> redis

The backend services are intended to stay private inside the Container Apps environment. Public access is exposed only through:

- the frontend app
- the NGINX gateway
- Keycloak

## Database Stack

Terraform provisions one Azure SQL logical server per deployed stack and two single databases per server:

- SQL server: derived from the short stack prefix, for example `cr-p-v1-sql`
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
- `ASPNETCORE_ENVIRONMENT`, `CurrencyConverterSettings__GrpcUrl`, and RabbitMQ connection metadata for `car-rental-service` and `booking-service`
- `REDIS_ADDR` for `currency-converter-service`
- Keycloak runtime configuration such as `KC_DB`, `KC_DB_URL`, `KEYCLOAK_FRONTEND_URL`, and realm import settings

Secret environment values currently include:

- `ConnectionStrings__DefaultConnection` for `car-rental-service` and `booking-service`
- `CurrencyConverterSettings__Username` and `CurrencyConverterSettings__Password` for `car-rental-service` and `booking-service`
- `RabbitMQ__Password` for `car-rental-service`, `booking-service`, and `rabbitmq`
- `GOOGLE_MAPS_API_KEY` and `PEXELS_API_KEY` for `request-proxy-service`
- `SOAP_USERNAME` and `SOAP_PASSWORD` for `currency-converter-service`
- `KC_DB_PASSWORD`, `KC_BOOTSTRAP_ADMIN_USERNAME`, and `KC_BOOTSTRAP_ADMIN_PASSWORD` for `keycloak-service`
- GHCR registry credentials for image pulls

## Frontend Runtime Configuration

Frontend runtime settings are injected as Container App environment variables by Terraform, then rendered into `/app-config.js` by the frontend NGINX entrypoint on container start.

This means frontend Docker builds do not require `VITE_*` build secrets or GitHub repository variables anymore.

## Images and Registry

Application service images are pulled from `ghcr.io`. Infrastructure dependencies use public images:

- `rabbitmq:3-management`
- `redis:7-alpine`

Application image versions are defined in `versions.tf` under `local.stack_versions`. `main.tf` declares which stacks should be deployed through `local.deployed_stacks`, and each selected stack provides immutable image tags for:

- frontend
- nginx gateway
- booking service
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

- RabbitMQ and Redis are deployed as single internal Container Apps with TCP ingress for service-to-service traffic.
- Azure SQL is still billable while active; continuously running apps can keep serverless SQL awake.
- Keycloak is intentionally public and separate from the NGINX gateway because browser auth flows must reach it directly.
- Internal service-to-service communication relies on the Container Apps environment network, not public ingress.
