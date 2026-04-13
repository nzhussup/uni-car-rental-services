# NginxGateway Documentation

## Overview

`NginxGateway` is the API ingress layer in front of backend services.
It centralizes routing and CORS behavior.

## Technologies

- NGINX 1.27 (Alpine)
- Static `nginx.conf`
- Dockerized deployment

## Listener and Routing

- Listens on `8080`

Routing rules:

- `/api/proxy/` -> `request-proxy-service`
- `/api/` -> `car-rental-service`
- `/` -> `404`

## Gateway Behavior

- Handles CORS response headers globally
- Handles preflight requests (`OPTIONS`) with `204`
- Adds forward headers (`X-Forwarded-*`, `X-Real-IP`)
- Hides upstream CORS headers to keep policy centralized
- Applies connection/send/read timeouts

## Topology View

```text
Client -> NginxGateway -> CarRentalService
                     -> RequestProxyService
```

## Quality Notes

- Small, explicit config in single file
- CI validates container build (`build-only` classification)
- No dedicated gateway contract tests in current pipeline
