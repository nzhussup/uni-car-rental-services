# Backend Aggregate Documentation

## Cover Page

- Project: Car Rental Platform
- Scope: Backend services, local runtime, and production infrastructure
- Repository path: `car-rental-services`
- Version date: May 4, 2026

## Table of Contents

1. Tools and Technologies
2. Description of Web Service APIs and Application
3. Overall Architecture in Production
4. Local Development Process
5. Service Documentation: Booking Service
6. Service Documentation: Car Service
7. Service Documentation: Currency Converter Service
8. Service Documentation: Request Proxy Service
9. Service Documentation: Nginx Gateway
10. Service Documentation: Keycloak Service
11. Infrastructure Documentation: Terraform
12. Code Quality Assurance
13. Lessons Learned

## Tools and Technologies

- Backend APIs: ASP.NET Core and Go
- Contracts: OpenAPI and gRPC protobuf
- Authentication and authorization: Keycloak
- Messaging: RabbitMQ
- Caching: Redis
- Databases: SQL Server locally and Azure SQL in production
- Ingress: NGINX
- Infrastructure as code: Terraform
- Delivery: Docker and GitHub Actions

## Description of Web Service APIs and Application

Discovered backend/runtime components:

- `BookingService`
- `CarService`
- `CurrencyConverterService`
- `RequestProxyService`
- `NginxGateway`
- `KeycloakService`
- local compose support services: SQL Server, RabbitMQ, Redis
- Terraform deployment stack

Composite runtime flow:

```text
Frontend -> NginxGateway -> CarService
                      -> BookingService
                      -> RequestProxyService

BookingService <-> RabbitMQ <-> CarService
BookingService -------------> CurrencyConverterService
CarService -----------------> CurrencyConverterService
CurrencyConverterService ---> ECB feed
CurrencyConverterService <-> Redis
Frontend and APIs <--------> Keycloak
BookingService/CarService --> SQL Server
```

The repository also contains a root-level `openapi.yaml` inside `car-rental-services` that represents the composite API surface.

OpenAPI and frontend client generation:

- each backend service can generate its own `openapi.yaml`
- `scripts/generate-all-openapi.sh` scans configured service directories, generates service specs, and merges them into `car-rental-services/openapi.yaml`
- during the merge step, conflicting component names and path collisions are rewritten when necessary
- that aggregate backend contract is the intended source for the frontend-generated API client

## Overall Architecture in Production

Terraform deploys shared Azure resources and one or more versioned stacks. In split mode the externally reachable surface is:

- frontend container app
- NGINX gateway container app
- Keycloak container app

Internal-only production services are:

- booking-service
- car-service
- request-proxy-service
- currency-converter-service
- optional RabbitMQ
- optional Redis

Production dependency graph:

```text
Browser -> frontend
Browser -> gateway
Browser -> keycloak
gateway -> booking-service
gateway -> car-service
gateway -> request-proxy-service
booking-service -> Azure SQL
car-service -> Azure SQL
keycloak -> Azure SQL
booking-service <-> RabbitMQ <-> car-service
booking-service -> currency-converter-service
car-service -> currency-converter-service
currency-converter-service -> Redis (optional)
```

RabbitMQ messaging architecture:

- purpose: asynchronous coordination between booking lifecycle changes and car availability state
- exchange for booking-originated events: `booking_exchange`
- exchange for car-originated events: `car_exchange`
- queue declared by `CarService`: `booking_queue`
- queue declared by `BookingService`: `car_queue`
- exchange type: topic

Routing keys discovered in the code:

- `booking.info`
- `car.info`
- `car.maintenance`

Payload types discovered in the code:

- `BookingInfo`
- `CarInfo`
- `MaintainanceStartInfo`

Message flow:

1. `BookingService` publishes `BookingInfo`
2. `CarService` consumes from `booking_queue`
3. `CarService` reserves or releases unavailable dates
4. `CarService` publishes `CarInfo` or `MaintainanceStartInfo`
5. `BookingService` consumes from `car_queue` and updates bookings

## Local Development Process

The local runtime comes from `.dev/compose.yml` and runs the backend system behind `http://localhost:8080`. The frontend is expected to run separately on `http://localhost:5173`.

Important local endpoints:

- gateway: `http://localhost:8080`
- Keycloak: `http://localhost:7070`
- booking-service direct port: `http://localhost:5011`
- car-service direct port: `http://localhost:5002`
- RabbitMQ management: `http://localhost:15672`

The stack depends on health checks for SQL Server and RabbitMQ, automatic EF Core migrations in the .NET services, and a `db-seeder` job that waits for the expected tables before loading seed data.

Local support-service details:

- RabbitMQ exposes AMQP on `5672` and the management UI on `15672`
- the local broker defaults to user `emre`
- Redis exposes `6379` and is used by `CurrencyConverterService` as an optional cache
- if Redis is unavailable, the currency converter still starts and runs without caching

## Service Documentation: Booking Service

### Overview and Responsibility

`BookingService` is the booking-focused REST API in the split backend. It stores bookings in SQL Server, enforces user and admin access rules with Keycloak JWTs, converts money through the gRPC currency service client, and exchanges booking state with `CarService` over RabbitMQ.

### Tools and Technologies Used

- ASP.NET Core Web API on .NET
- Entity Framework Core with SQL Server
- Keycloak AuthServices for JWT authentication and realm-role authorization
- AutoMapper for DTO mapping
- RabbitMQ.Client for asynchronous integration
- Swagger / OpenAPI for contract generation

### API Description

Base route: `/api/booking`

| Method | Route | Purpose | Authorization |
| --- | --- | --- | --- |
| `GET` | `/api/booking` | List all bookings with pagination and optional currency conversion | `app-admin` |
| `GET` | `/api/booking/user` | List bookings for the current authenticated user | `app-user` |
| `GET` | `/api/booking/{id}` | Read one booking | owner or admin |
| `POST` | `/api/booking` | Create a booking for the current user | `app-user` |
| `PATCH` | `/api/booking/{id}/status` | Update booking status | `app-admin` |
| `PATCH` | `/api/booking/{id}/cancel` | Cancel the current user's booking | `app-user` |
| `DELETE` | `/api/booking/{id}` | Delete a booking and release reserved dates | `app-admin` |

### Data Model

```text
Booking
|- Id
|- CarId
|- UserId (Keycloak subject UUID)
|- PickupDate
|- DropoffDate
|- BookingDate
|- Status
|- CarPriceInUsd
|- TotalCostInUsd
|- Make / Model / CarYear snapshot
```

### Runtime Wiring and Dependencies

```text
Frontend -> NginxGateway -> BookingService
BookingService -> SQL Server
BookingService -> Keycloak
BookingService -> CurrencyConverterService (gRPC client)
BookingService -> RabbitMQ exchange: booking.*
BookingService <- RabbitMQ exchange: car.*
```

### Validation and Error Handling

- missing authenticated user id returns `401`
- missing booking or inaccessible booking returns `404` or `403`
- overlapping active bookings for the same car are rejected
- completed bookings cannot be changed
- JSON Problem Details are returned for handled exceptions

### RabbitMQ Details

- publishes `BookingInfo` to `booking_exchange`
- routing key used by the producer: `booking.info`
- consumes `CarInfo` and `MaintainanceStartInfo` from queue `car_queue`
- the booking side is therefore eventually consistent with the car side

## Service Documentation: Car Service

### Overview and Responsibility

`CarService` is the catalog and availability API for vehicles. It stores the car inventory, filters available cars by date range, converts prices through the currency service, and publishes car availability or maintenance events to the booking domain over RabbitMQ.

### Tools and Technologies Used

- ASP.NET Core Web API on .NET
- Entity Framework Core with SQL Server
- Keycloak AuthServices for JWT authentication and admin authorization
- AutoMapper for DTO mapping
- RabbitMQ.Client for asynchronous integration
- Swagger / OpenAPI for contract generation

### API Description

Base route: `/api/cars`

| Method | Route | Purpose | Authorization |
| --- | --- | --- | --- |
| `GET` | `/api/cars` | Search and page through cars | public |
| `GET` | `/api/cars/{id}` | Read one car | public |
| `POST` | `/api/cars` | Create a car | `app-admin` |
| `PUT` | `/api/cars/{id}` | Update a car | `app-admin` |
| `PATCH` | `/api/cars/{id}/status` | Change availability status | `app-admin` |
| `DELETE` | `/api/cars/{id}` | Delete a car and trigger maintenance-style cancellation flow | `app-admin` |

### Data Model

```text
Car
|- Id
|- Make
|- Model
|- Year
|- Price
|- Status
`- UnavailableDates[*]
```

### Runtime Wiring and Dependencies

```text
Frontend -> NginxGateway -> CarService
CarService -> SQL Server
CarService -> Keycloak
CarService -> CurrencyConverterService (gRPC client)
CarService -> RabbitMQ exchange: car.*
CarService <- RabbitMQ exchange: booking.*
```

### Validation and Error Handling

- missing cars return `404`
- admin-only mutations return `403` for unauthorized callers
- date range search excludes overlapping unavailable periods
- JSON Problem Details are returned for handled exceptions

### RabbitMQ Details

- consumes `BookingInfo` from queue `booking_queue`
- publishes `CarInfo` with routing key `car.info`
- publishes `MaintainanceStartInfo` with routing key `car.maintenance`
- persists availability by adding or removing unavailable date ranges

## Service Documentation: Currency Converter Service

### Overview and Responsibility

`CurrencyConverterService` is the dedicated currency backend used by the car and booking APIs. It exposes gRPC operations, protects them with basic-auth interception, reads rates from the European Central Bank XML feed, and optionally caches fetched rates in Redis.

### Tools and Technologies Used

- Go
- gRPC
- Redis
- ECB XML feed

### API Description

Defined RPCs:

- `GetExchangeRate`
- `ConvertAmount`
- `GetSupportedCurrencies`

### Runtime Wiring and Dependencies

```text
BookingService -> CurrencyConverterService -> ECB feed
CarService ----> CurrencyConverterService -> ECB feed
CurrencyConverterService <-> Redis
```

### Validation and Error Handling

- startup exits if credentials are missing
- Redis failure degrades to uncached operation

## Service Documentation: Request Proxy Service

### Overview and Responsibility

`RequestProxyService` is a controlled outbound proxy for third-party APIs. It lets the frontend request whitelisted upstream resources without exposing API keys in the browser.

### Tools and Technologies Used

- Go
- Gin
- OpenAPI
- JSON-based service config

### API Description

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/health` | Liveness endpoint |
| `POST` | `/api/proxy/execute` | Execute an allowed request against a configured upstream |

Configured upstream ids:

- `google-places`
- `pexels`

### Runtime Wiring and Dependencies

```text
Frontend -> NginxGateway -> RequestProxyService -> Google Maps API / Pexels API
```

### Validation and Error Handling

- unknown service -> `404`
- disallowed method -> `405`
- upstream transport failure -> `502`

## Service Documentation: Nginx Gateway

### Overview and Responsibility

`NginxGateway` is the single HTTP ingress for backend API calls. It applies CORS and security headers and routes traffic to booking, car, proxy, or monolith backends based on path prefixes.

### Routing

- `/api/proxy` -> `REQUEST_PROXY_UPSTREAM`
- `/api/booking` -> `BOOKING_UPSTREAM`
- `/api/cars` -> `CARS_UPSTREAM`
- `/api/` -> `API_UPSTREAM`
- `/` -> `404`

## Service Documentation: Keycloak Service

### Overview and Responsibility

`KeycloakService` is the identity provider for login, token issuance, realm roles, and user profile storage.

### Identity Model

- dev realm template: `car-rental-dev-realm.json`
- prod realm template: `car-rental-prod-realm.json`
- main roles: `app-user`, `app-admin`

### Runtime Wiring and Dependencies

The custom entrypoint renders a realm import file, imports it, and starts Keycloak in dev or optimized production mode.

## Infrastructure Documentation: Terraform

### Overview and Responsibility

`infra/terraform` defines the Azure production platform. It provisions shared observability resources, container apps, SQL resources, and optional RabbitMQ and Redis instances.

### Infrastructure Model

```text
shared resources
`- stacks[v1, v2]
   |- frontend
   |- gateway
   |- keycloak
   |- request proxy
   |- currency converter
   |- split backend: booking + car
   `- optional rabbitmq + redis
```

Additional Terraform details:

- the root module provisions shared observability resources and then instantiates stack modules for each deployed version
- input variables cover registry access, SQL credentials, Keycloak bootstrap values, request-proxy API keys, RabbitMQ exchange names, and Redis password
- outputs expose environment names, SQL endpoints, and app FQDNs for downstream use
- service-to-service runtime wiring is expressed through Terraform-managed environment variables rather than manual portal configuration

## CI/CD Documentation

The backend CI/CD pipeline is defined in `car-rental-services/.github/workflows/ci.yml` and implemented with reusable composite actions under `.github/templates`.

Backend pipeline stages:

1. `prepare`
   - runs `generate_ci_matrix.py`
   - reads `.github/ci/services.json`
   - detects whether each service is Go, .NET, or `build-only`
2. `quality`
   - Go: installs `golangci-lint` and runs `golangci-lint run ./...`
   - .NET: runs `dotnet format --verify-no-changes`
3. `test`
   - Go: runs `go test ./... -v`
   - .NET: runs `dotnet test --configuration Release`
4. `build`
   - uses Docker Buildx
   - logs into GHCR with a registry token
   - builds and pushes `linux/amd64` images tagged `latest`

Services currently included in the backend matrix:

- `booking-service`
- `car-service`
- `currency-converter-service`
- `request-proxy-service`
- `nginx-gateway` as `build-only`
- `keycloak-service` as `build-only`

Frontend CI/CD:

1. `quality` runs `npm ci` and `npm run lint`
2. `test` runs `npm ci` and `npm test`
3. `build` logs into GHCR and publishes the frontend image

### RabbitMQ and Redis in Production

- RabbitMQ supports booking/car integration when enabled by stack definition.
- Redis supports exchange-rate caching for the currency converter when enabled.

## Code Quality Assurance

Backend quality evidence discovered in the repository:

- backend CI workflow: `car-rental-services/.github/workflows/ci.yml`
- service matrix source: `car-rental-services/.github/ci/services.json`
- .NET quality gate: `dotnet format --verify-no-changes`
- .NET tests: `dotnet test`
- Go quality gate: `golangci-lint`
- Go tests: `go test ./...`
- frontend CI workflow: `car-rental-frontend/.github/workflows/ci.yml`
- frontend quality commands: `npm run lint`, `npm test`, `npm run build`

Build-only backend components in CI:

- `nginx-gateway`
- `keycloak-service`

## Lessons Learned

### What does not work yet

- The system still lacks one in-repo end-to-end suite that spans frontend, auth, gateway, messaging, and data storage.
- Build-only services have weaker automated behavior validation than the API services.
- Split and monolith deployment support increases documentation and configuration surface.

### Experience with technologies

- Service decomposition is clearer now that booking and car APIs are documented separately.
- RabbitMQ and Redis are important enough to document explicitly rather than treating them as hidden implementation details.
- Terraform gives the production deployment a much more reproducible shape than manual container configuration.

### Experience with AI tools used

- AI helped structure the codebase and brainstorm the overall infrastructure.
- Human review was still required to verify endpoints, environment variables, topology, and error behavior from source.
