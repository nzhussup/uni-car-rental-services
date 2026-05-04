# car-rental-services

[![Backend CI](https://github.com/timmy-2003/car-rental-services/actions/workflows/ci.yml/badge.svg)](https://github.com/timmy-2003/car-rental-services/actions/workflows/ci.yml)
![.NET](https://img.shields.io/badge/.NET-10-512bd4?logo=dotnet&logoColor=white)
![Go](https://img.shields.io/badge/Go-1.24-00add8?logo=go&logoColor=white)
![OpenAPI](https://img.shields.io/badge/API-OpenAPI-6ba539?logo=openapiinitiative&logoColor=white)
![gRPC](https://img.shields.io/badge/Internal-gRPC-244c5a?logo=grpc&logoColor=white)
![RabbitMQ](https://img.shields.io/badge/Messaging-RabbitMQ-ff6600?logo=rabbitmq&logoColor=white)
![Redis](https://img.shields.io/badge/Cache-Redis-d82c20?logo=redis&logoColor=white)
![Keycloak](https://img.shields.io/badge/Auth-Keycloak-4d4d4d?logo=keycloak&logoColor=white)
![Terraform](https://img.shields.io/badge/IaC-Terraform-844fba?logo=terraform&logoColor=white)
![Docker](https://img.shields.io/badge/Deploy-Docker-2496ed?logo=docker&logoColor=white)

Backend and infrastructure repository for the car rental platform.

Main components:

- `BookingService/` - booking REST API
- `CarService/` - car catalog and availability REST API
- `CurrencyConverterService/` - gRPC currency conversion service with optional Redis cache
- `RequestProxyService/` - controlled proxy for external APIs
- `NginxGateway/` - ingress routing and CORS layer
- `KeycloakService/` - identity provider container setup
- `.dev/` - local Docker Compose stack
- `infra/terraform/` - Azure production infrastructure

How the backend is structured:

- browser traffic goes through `NginxGateway`
- `BookingService` and `CarService` are separate .NET APIs
- booking/car synchronization is asynchronous through RabbitMQ
- `CurrencyConverterService` is called by the .NET APIs for money conversion
- `RequestProxyService` keeps external API keys out of the frontend
- `KeycloakService` provides authentication and realm roles

## Local development

Start the backend stack from `car-rental-services/.dev`:

```bash
docker compose -f .dev/compose.yml up --build
```

The compose stack starts:

- NGINX gateway
- SQL Server
- RabbitMQ
- BookingService
- CarService
- CurrencyConverterService
- Redis
- RequestProxyService
- Keycloak
- database seeder job

Important local endpoints:

- gateway: `http://localhost:8080`
- booking service: `http://localhost:5011`
- car service: `http://localhost:5002`
- SQL Server: `localhost:1433`
- Keycloak: `http://localhost:7070`
- Redis: `localhost:6379`
- RabbitMQ management: `http://localhost:15672`

Important local defaults from compose:

- SQL Server database: `CarRentalDB`
- SQL Server user: `sa`
- RabbitMQ user: `emre`
- Keycloak bootstrap admin: `root`
- Keycloak dev realm: `car-rental-dev`

Typical local workflow:

1. start the backend compose stack
2. wait for SQL Server and RabbitMQ health checks to pass
3. run the frontend separately on `http://localhost:5173`
4. use the gateway on `http://localhost:8080` as the browser-facing API

## CI/CD

Backend CI is defined in:

- `.github/workflows/ci.yml`
- `.github/ci/services.json`

The pipeline:

1. generates a service matrix
2. runs quality checks
3. runs tests
4. builds and pushes Docker images to GHCR

Build-only services in the matrix:

- `nginx-gateway`
- `keycloak-service`

Quality/test behavior:

- Go services run `golangci-lint` and `go test ./... -v`
- .NET services run `dotnet format --verify-no-changes` and `dotnet test`
- images are built with Docker Buildx and pushed to GHCR

## OpenAPI generation

The repository supports per-service and aggregate OpenAPI generation.

Generate one service spec:

```bash
./scripts/generate-openapi.sh <project-dir> <output-path>
```

Examples:

```bash
./scripts/generate-openapi.sh ./BookingService ./BookingService/openapi.yaml
./scripts/generate-openapi.sh ./CarService ./CarService/openapi.yaml
./scripts/generate-openapi.sh ./RequestProxyService ./RequestProxyService/openapi.yaml
```

Generate all configured service specs plus the aggregate backend contract:

```bash
./scripts/generate-all-openapi.sh
```

This generates:

- per-service `openapi.yaml` files
- aggregate backend contract at `./openapi.yaml`

The frontend uses that aggregate backend contract to generate its typed API client.

Current API contract sources:

- `BookingService/openapi.yaml`
- `CarService/openapi.yaml`
- `RequestProxyService/openapi.yaml`
- aggregate backend contract: `openapi.yaml`

## Notes

- Booking and car availability synchronization is event-driven through RabbitMQ.
- Currency conversion depends on `CurrencyConverterService`.
- Production deployment is defined in `infra/terraform`.
- detailed architecture and service behavior are documented in the `DOCUMENTATION.md` files.
