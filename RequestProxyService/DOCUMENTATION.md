# Request Proxy Service Documentation

## Overview and Responsibility

`RequestProxyService` is a controlled outbound proxy for third-party APIs. It lets the frontend request whitelisted upstream resources without exposing API keys in the browser, and it keeps service definitions in configuration rather than in UI code.

## Tools and Technologies Used

- Go service
- Gin HTTP router
- OpenAPI for public contract
- JSON-based upstream service configuration

## API Description

Base routes:

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/health` | Liveness endpoint |
| `POST` | `/api/proxy/execute` | Execute an allowed request against a configured upstream |

Request body for `/api/proxy/execute`:

```text
ExecuteRequest
|- service
|- method
|- path
|- query?   map[string]string
|- headers? map[string]string
`- body?    any
```

Configured upstreams from `config/services.json`:

- `google-places` -> `https://maps.googleapis.com`
- `pexels` -> `https://api.pexels.com`

Both configured services only allow `GET`.

## Runtime Wiring and Dependencies

```text
Frontend -> NginxGateway -> RequestProxyService -> Google Maps API
                                        `-------> Pexels API
```

Configuration and secrets:

- `SERVICES_CONFIG_PATH` selects the JSON config file
- `GOOGLE_MAPS_API_KEY` is injected into the default query for `google-places`
- `PEXELS_API_KEY` is injected into the default headers for `pexels`

## Validation and Error Handling

The service validates requests against the loaded config before forwarding:

- unknown service -> `404`
- disallowed method -> `405`
- malformed request -> `400`
- upstream network or transport failure -> `502`
- other unexpected failures -> `500`

For successful upstream `2xx` responses, the raw upstream payload is passed through to the caller.

## Code Quality Practices

- Public OpenAPI contract in [`openapi.yaml`](./openapi.yaml)
- Unit tests exist for config loading and proxy handler behavior
- Backend CI matrix runs quality, tests, and build for this service
- Whitelisting and secret injection are centralized in configuration rather than scattered in frontend code

## Lessons Learned

### What does not work yet

- Only two upstream integrations are configured at the moment.
- The service does not implement advanced controls such as rate limiting or per-client quotas.
- Proxying raw upstream payloads keeps the service simple, but it also means response normalization is limited.

### Experience with technologies

- A config-driven proxy is a practical compromise between security and frontend flexibility.
- Keeping secrets on the server side is much simpler than trying to protect them in a browser-only flow.
- Gin is sufficient for a lightweight integration service like this.

### Experience with AI tools used

- AI helped with high-level service boundaries and documentation structure.
- The allowed methods, secret injection, and error semantics still had to be confirmed from the current code and config.
