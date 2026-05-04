# Local Development Stack Documentation

## Overview and Responsibility

`car-rental-services/.dev/compose.yml` defines the local development runtime. It starts the backend services plus the support infrastructure needed to run them together on a developer machine.

## Components Discovered in the Compose Stack

- `nginx` on host port `8080`
- `mssql` on host port `1433`
- `rabbitmq` on host ports `5672` and `15672`
- `booking-service` on host port `5011`
- `car-service` on host port `5002`
- `db-seeder`
- `currency-converter-service`
- `redis` on host port `6379`
- `request-proxy-service`
- `keycloak` on host port `7070`

## Local Runtime Flow

```text
Frontend (Vite on 5173)
  -> nginx on 8080
     -> booking-service
     -> car-service
     -> request-proxy-service

booking-service <-> mssql
car-service     <-> mssql
booking-service <-> rabbitmq <-> car-service
booking-service -> currency-converter-service
car-service     -> currency-converter-service
currency-converter-service <-> redis
frontend and APIs <-> keycloak
```

## Local Development Process

Recommended startup order implied by the compose file:

1. start the compose stack from `car-rental-services/.dev`
2. wait for SQL Server and RabbitMQ health checks
3. let the .NET services apply migrations on startup
4. let `db-seeder` load seed data after the expected tables exist
5. run the frontend separately with Vite on `http://localhost:5173`
6. use `http://localhost:8080` as the API base URL and `http://localhost:7070` for Keycloak

Important local behavior:

- NGINX sets CORS for `http://localhost:5173`
- Redis is password-protected locally
- RabbitMQ management UI is exposed on `15672`
- Keycloak dev mode imports the `car-rental-dev` realm template automatically

## RabbitMQ, Redis, and SQL Server

RabbitMQ:

- topic exchanges connect booking and car domains
- `booking-service` publishes `booking.*`
- `car-service` publishes `car.*`

Redis:

- optional cache for exchange-rate lookups
- if unavailable, `CurrencyConverterService` still starts and falls back to uncached operation

SQL Server:

- shared local database for booking and car service data
- seeded by `db-seeder` after migrations expose the expected tables

## Code Quality Practices

- local runtime is fully containerized except for the frontend dev server
- the stack makes it practical to test cross-service flows before production deployment
- OpenAPI generation scripts exist under `car-rental-services/scripts`

## Lessons Learned

### What does not work yet

- Local startup still depends on timing between migrations and seed execution.
- Developers need external API keys for the proxy-backed frontend features to work fully.
- The frontend and backend still run from separate repos/subtrees, so bootstrapping is multi-step.

### Experience with technologies

- Docker Compose remains the fastest way to make the multi-service system reproducible for development.
- RabbitMQ and Redis are easiest to reason about when documented as support services, not hidden implementation details.

### Experience with AI tools used

- AI helped describe the local-dev topology and the role of the support containers.
- Exact port mappings, health checks, and seed ordering had to come from the compose and shell scripts.
