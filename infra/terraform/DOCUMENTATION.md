# Terraform Infrastructure Documentation

## Overview and Responsibility

`car-rental-services/infra/terraform` defines the production deployment stack on Azure. It provisions shared observability resources, container app environments, SQL infrastructure, and one or more application stacks that host the frontend-facing gateway, backend services, Keycloak, and optional stateful helpers.

## Tools and Technologies Used

- Terraform
- AzureRM provider
- Azure Container Apps
- Azure SQL
- Azure Log Analytics

## Infrastructure Model

Top-level deployment structure from `main.tf`:

```text
shared resource group
|- log analytics workspace
`- container apps environment

stacks[v1, v2]
|- frontend app
|- nginx gateway
|- request proxy service
|- currency converter service
|- keycloak service
|- split backend:
|  |- car service
|  `- booking service
|- or monolith backend:
|  `- car-rental-service
|- optional rabbitmq app
`- optional redis app
```

Important module families:

- `resource_group`
- `log_analytics`
- `container_apps_env`
- `platform_stack`
- `container_app`
- `sql`
- `rabbitmq`
- `redis`

Deployment behavior:

- shared resources are created once at the root level and reused by versioned stack modules
- the current root configuration deploys two stacks, `v1` and `v2`
- each stack can run in `split` or `monolith` backend mode
- image references are injected per stack, so infrastructure and application versions can be promoted independently
- GHCR credentials are passed into Terraform so Azure Container Apps can pull the service images

## Runtime Wiring and Dependencies

Production URLs are constructed from the shared Container Apps environment domain. The current Terraform wiring sets:

- external apps: frontend, gateway, Keycloak
- internal apps: booking, car, request-proxy, currency-converter
- optional internal helpers: RabbitMQ and Redis

Selected environment wiring from `platform_stack/main.tf`:

- frontend receives `VITE_API_BASE_URL`, `VITE_KEYCLOAK_URL`, `VITE_KEYCLOAK_REALM`, `VITE_KEYCLOAK_CLIENT_ID`
- gateway receives CORS origin and upstream service names
- booking and car services receive SQL connection strings, Keycloak config, RabbitMQ settings, and currency-converter gRPC URL
- currency converter optionally receives Redis connection settings
- Keycloak receives JDBC settings and realm import parameters

Important input variables:

- registry: `ghcr_server`, `ghcr_username`, `ghcr_password`
- SQL: `sql_admin_login`, `sql_admin_password`
- request-proxy secrets: `google_maps_api_key`, `pexels_api_key`
- Keycloak bootstrap and runtime: `keycloak_admin_username`, `keycloak_admin_password`, `frontend_keycloak_realm`, `frontend_keycloak_client_id`
- currency converter auth: `currency_converter_soap_username`, `currency_converter_soap_password`
- messaging and cache: `rabbitmq_username`, `rabbitmq_password`, `rabbitmq_car_exchange`, `rabbitmq_booking_exchange`, `redis_password`

Published outputs:

- per-stack resource-group names
- per-stack Container Apps environment names
- per-stack SQL server FQDNs
- per-stack application FQDN maps

## RabbitMQ and Redis in Production

- RabbitMQ is controlled by `enable_rabbitmq` in the stack definition and provides the integration channel between booking and car domains.
- Redis is controlled by `enable_redis` and is currently used by `CurrencyConverterService` as an optional cache.
- Both are infrastructure-level dependencies rather than standalone user-facing APIs.

RabbitMQ topology:

- exchange name for car-originated events defaults to `car_exchange`
- exchange name for booking-originated events defaults to `booking_exchange`
- the application services declare their own queues at runtime:
  - `booking_queue` in `CarService`
  - `car_queue` in `BookingService`
- both exchanges use topic routing, which allows wildcard subscriptions such as `booking.*` and `car.*`

## Code Quality Practices

- Infrastructure is decomposed into reusable modules instead of one large root file
- helper scripts exist in `infra/scripts` for update and inventory tasks
- the production topology is explicit in code and environment variables, which reduces hidden manual setup

## Lessons Learned

### What does not work yet

- The repository does not include an in-repo integration test for a full Terraform apply plus application smoke test.
- Mixed support for split and monolith backend modes increases configuration surface.

### Experience with technologies

- Azure Container Apps is a reasonable match for a small multi-service platform with mostly HTTP workloads.
- Terraform modules make it easier to keep shared and per-stack resources consistent across environments.

### Experience with AI tools used

- AI was helpful for documenting the production architecture and dependency map.
- Actual app exposure, secret wiring, and stack modes still had to be verified from Terraform source.
