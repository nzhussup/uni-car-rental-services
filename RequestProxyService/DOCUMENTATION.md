# RequestProxyService API Documentation

## Overview

`RequestProxyService` is a config-driven outbound proxy.
It allows frontend features to consume third-party APIs without exposing secrets in browser code.

## Technologies

- Go 1.24
- Gin HTTP framework
- OpenAPI contract (`openapi.yaml`)
- JSON service registry (`config/services.json`)

## Endpoints

- `GET /health`
- `POST /api/proxy/execute`

## Execute API Contract

Request fields:

- `service` (required)
- `method` (required)
- `path` (required)
- `query` (optional)
- `headers` (optional)
- `body` (optional)

Response behavior:

- 2xx upstream -> raw payload passthrough
- undefined service -> `404`
- disallowed method -> `405`
- upstream failure/non-2xx -> `502`

## Service Configuration Model

Configured providers (current):

- `google-places`
- `pexels`

Service definition fields:

- `id`
- `baseUrl`
- `allowedMethods`
- `defaultHeaders`
- `defaultQuery`

## Secret Injection

- Config values support `${ENV_VAR}` placeholders
- Placeholders are resolved from runtime environment
- Secrets remain server-side

## Processing Flow

```text
client request
-> JSON bind/validate
-> resolve service config
-> enforce allowed method
-> build target URL + headers + query
-> execute upstream call
-> return passthrough data or structured error
```

## Quality Notes

- Test coverage for config loading and proxy handler behavior
- CI includes Go linting + unit tests + image build
