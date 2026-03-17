# RequestProxyService

A config-driven Go/Gin proxy service for calling whitelisted external APIs without exposing secrets in the frontend.

## Features

- Central service registry in `config/services.json`
- Single generic endpoint: `POST /api/proxy/execute`
- Method allow-list enforcement per service
- Env-var placeholder support in config values (`${ENV_VAR}`)
- Raw upstream payload passthrough on 2xx responses
- Structured error responses for invalid config/service/method/upstream failures
- Swagger annotations on the endpoint and API metadata

## Run

```bash
cd car-rental-services/RequestProxyService
go mod tidy
go run ./cmd/server
```

By default the service starts on `:8082`.

Environment variables:

- `PORT` (default: `8082`)
- `SERVICES_CONFIG_PATH` (default: `config/services.json`)
- any secret keys referenced by config placeholders, e.g.:
  - `GOOGLE_MAPS_API_KEY`
  - `PEXELS_API_KEY`

## Request schema

```json
{
  "service": "google-places",
  "method": "GET",
  "path": "/maps/api/place/details/json",
  "query": {
    "place_id": "ChIJN1t_tDeuEmsRUsoyG83frY4"
  },
  "headers": {
    "X-Correlation-Id": "abc-123"
  },
  "body": null
}
```

## Example curl

```bash
curl -X POST http://localhost:8082/api/proxy/execute \
  -H 'Content-Type: application/json' \
  -d '{
    "service": "google-places",
    "method": "GET",
    "path": "/maps/api/place/details/json",
    "query": {
      "place_id": "ChIJN1t_tDeuEmsRUsoyG83frY4"
    }
  }'
```

## Notes

- If the service is missing from config, the API returns `404` with `service not defined`.
- If the upstream returns non-2xx, the API returns `502` with upstream status/details.
- Keep sensitive values in environment variables and reference them as `${ENV_VAR}` in `config/services.json`.
