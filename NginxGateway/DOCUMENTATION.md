# Nginx Gateway Documentation

## Overview and Responsibility

`NginxGateway` is the single HTTP ingress for backend API calls. It applies CORS and basic hardening headers, routes browser traffic to the correct internal backend service, and prevents the frontend from calling internal container app hostnames directly.

## Tools and Technologies Used

- NGINX 1.27 Alpine image
- Docker template-based runtime configuration

## API / Routing Description

The gateway does not implement business endpoints itself. It forwards requests according to `default.conf.template`:

| Path prefix | Upstream environment variable |
| --- | --- |
| `/api/proxy` | `REQUEST_PROXY_UPSTREAM` |
| `/api/booking` | `BOOKING_UPSTREAM` |
| `/api/cars` | `CARS_UPSTREAM` |
| `/api/` | `API_UPSTREAM` |
| `/` | returns `404` |

Behavior:

- listens on port `8080`
- answers CORS preflight with `204`
- sets `X-Content-Type-Options`, `X-Frame-Options`, and `Referrer-Policy`
- hides upstream CORS headers so the gateway remains the authority

## Runtime Wiring and Dependencies

```text
Browser -> NginxGateway -> BookingService
                      -> CarService
                      -> RequestProxyService
                      -> monolith API upstream when backend_mode = monolith
```

Local development uses the compose-specific `nginx.conf`. Production uses `default.conf.template` rendered by the official NGINX image entrypoint.

## Validation and Error Handling

- undefined frontend paths are not served here; `/` returns `404`
- the gateway depends on upstream service availability for successful responses
- long-running upstream requests are bounded by connect/send/read timeouts

## Code Quality Practices

- Build-only service in backend CI
- Configuration is template-driven and checked through the same image build pipeline as other services
- Gateway logic is intentionally declarative to keep review surface small

## Lessons Learned

### What does not work yet

- There are no dedicated automated gateway behavior tests in this repository.
- Advanced ingress controls such as rate limiting and request shaping are not implemented.

### Experience with technologies

- NGINX is a good fit when the gateway mainly needs routing, CORS, and headers.
- Keeping ingress logic outside the domain services simplifies local and production topology.

### Experience with AI tools used

- AI was useful for documenting the routing topology and environment-driven deployment picture.
- The actual paths and upstream variables still had to be taken from the NGINX template files.
