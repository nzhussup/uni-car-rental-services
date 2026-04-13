# Backend and Infrastructure Documentation

## Cover Page

- Project: Car Rental Platform
- Scope: Backend services and production infrastructure
- Repository Path: `car-rental-services`
- Version date: April 11, 2026

## Table of Contents

1. [Cover Page](#cover-page)
2. [Tools and Technologies Used](#tools-and-technologies-used)
3. [Description of Web Service APIs and Application](#description-of-web-service-apis-and-application)
4. [Service API Descriptions](#service-api-descriptions)
5. [Production Infrastructure (Terraform)](#production-infrastructure-terraform)
6. [How Code Quality Was Ensured](#how-code-quality-was-ensured)
7. [Lessons Learned](#lessons-learned)

## Tools and Technologies Used

- API services: ASP.NET Core (.NET 10), Go 1.24
- Protocols: REST/JSON and SOAP 1.1
- Identity and access control: Keycloak 26.1
- API gateway: NGINX 1.27
- Databases: SQL Server (dev), Azure SQL (prod)
- Infrastructure as code: Terraform + AzureRM provider
- Containers and delivery: Docker, GHCR, GitHub Actions

## Description of Web Service APIs and Application

The backend system consists of specialized services:

- `CarRentalService`: core car and booking business API
- `CurrencyConverterService`: SOAP conversion service used by car rental pricing endpoints
- `RequestProxyService`: controlled external API proxy (Google Places, Pexels)
- `NginxGateway`: single ingress for `/api` and `/api/proxy`
- `KeycloakService`: authentication and role management

### Runtime Flow (Backend-Centric)

```text
Frontend -> NginxGateway -> CarRentalService -> SQL DB
                             CarRentalService -> CurrencyConverterService
Frontend -> NginxGateway -> RequestProxyService -> External APIs
Frontend <-> KeycloakService
```

## Service API Descriptions

### CarRentalService

Overview:

- Main REST API for cars and bookings
- Enforces role-based authorization (`app-user`, `app-admin`)

Endpoints:

- Cars: `GET/POST/PUT/DELETE /api/cars`, `PATCH /api/cars/{id}/status`
- Bookings: `GET/POST /api/booking`, `GET /api/booking/user`, `GET/DELETE /api/booking/{id}`, `PATCH /api/booking/{id}/status`, `PATCH /api/booking/{id}/cancel`

Data model:

```text
Car (1) -------- (N) Booking
Booking.UserId = Keycloak subject UUID
```

Business logic highlights:

- Pagination with `Skip`/`Take` (max 50)
- Car availability filtering by date overlap
- Booking overlap prevention for active bookings
- Currency conversion delegated to SOAP service

### CurrencyConverterService

Overview:

- SOAP 1.1 API for rate lookup and amount conversion
- Consumes ECB daily exchange feed

Endpoints:

- `POST /soap`
- `GET /wsdl`
- `GET /health`

Operations:

- `GetExchangeRate`
- `ConvertAmount`
- `GetSupportedCurrencies`

Validation:

- Missing/unsupported currency rejected
- Non-positive amounts rejected

### RequestProxyService

Overview:

- Config-driven outbound HTTP proxy with method allow-listing
- Keeps third-party API keys off the frontend

Endpoints:

- `GET /health`
- `POST /api/proxy/execute`

Request shape:

- `service`, `method`, `path`, optional `query`, `headers`, `body`

Behavior:

- 2xx upstream payload passthrough
- undefined service -> `404`
- disallowed method -> `405`
- upstream failure -> `502`

### NginxGateway

Overview:

- Ingress proxy and CORS policy point

Routing:

- `/api/` -> car-rental-service
- `/api/proxy/` -> request-proxy-service
- fallback `/` -> `404`

### KeycloakService

Overview:

- Central IdP for browser login and backend bearer token authorization

Identity model:

- Realms: `car-rental-dev`, `car-rental-prod`
- Client: `car-rental-frontend`
- Roles: `app-user`, `app-admin`

Runtime:

- Custom entrypoint renders realm template and imports it at startup

## Production Infrastructure (Terraform)

Terraform stack (`infra/terraform`) provisions:

- Resource group
- Log Analytics workspace
- Container Apps environment
- SQL server + `CarRentalDB` + `KeycloakDB`
- Container apps for frontend, gateway, services, and keycloak

Exposure model:

- Public: frontend, nginx gateway, keycloak
- Internal: car-rental-service, request-proxy-service, currency-converter-service

Runtime wiring:

```text
Browser -> frontend
Browser/API -> nginx-gateway
nginx-gateway -> car-rental-service/request-proxy-service
car-rental-service -> currency-converter-service
car-rental-service -> CarRentalDB
keycloak-service -> KeycloakDB
```

## How Code Quality Was Ensured

### Backend API Services

- .NET quality gate: `dotnet format --verify-no-changes`
- .NET tests: `dotnet test`
- Go quality gate: `golangci-lint run ./...`
- Go tests: `go test ./... -v`

### CI Pipeline

`car-rental-services/.github/workflows/ci.yml` runs matrix jobs:

- `quality`
- `test`
- `build`

Build-only services (`NginxGateway`, `KeycloakService`) are built/published but do not run service-level tests.

### Testing

- Use of Unit tests
- Use of Integration tests

## Lessons Learned

### What Does Not Work Yet

- Full end-to-end automated tests across all backend services are limited.
- Build-only services need stronger automated behavior validation.
- Some resilience controls (rate limiting, outage drills, cache strategy) remain improvement areas.

### Experience with Technologies

- Service decomposition worked well for ownership and scalability.
- Mixed REST + SOAP integration is practical when contracts are explicit.
- Keycloak centralized authentication and authorization effectively across UI and APIs.
- Terraform provided reproducible production infrastructure.

### Experience with AI Tools Used

- AI helped structure the codebase and brainstorm the overall infrastructure.
- Human verification was required for security-sensitive and environment-specific details.
