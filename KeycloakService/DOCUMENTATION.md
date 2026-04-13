# KeycloakService Documentation

## Overview

`KeycloakService` provides authentication and authorization for the platform.
It is used by frontend browser flows and backend JWT policy enforcement.

## Technologies

- Keycloak 26.1
- SQL Server backend (`KC_DB=mssql`)
- Realm templates (`car-rental-dev-realm.json`, `car-rental-prod-realm.json`)
- Custom theme (`themes/leiwand-cars`)
- Custom container entrypoint for deterministic realm import

## Runtime Behavior

Container startup script:

1. Selects import mode (`dev` or `prod`)
2. Selects and renders realm template
3. Replaces realm/frontend URL/SSL placeholders
4. Imports realm via `kc.sh import`
5. Starts Keycloak (`start-dev` for dev, `start --optimized` for prod)

## Identity Model

- Realms: `car-rental-dev`, `car-rental-prod`
- Frontend client: `car-rental-frontend`
- Application roles:
- `app-user`
- `app-admin`

Realm templates include bootstrap users and role assignments for setup convenience.

## Integration Points

- Frontend: Keycloak JS (`check-sso`, PKCE)
- Backend: bearer-token validation + role policies (`User`, `Admin`)

## Quality Notes

- Deterministic realm import and environment-driven configuration
- Build validated in CI (`build-only` service classification)
- Behavior-level automated realm/login tests are not currently in CI
