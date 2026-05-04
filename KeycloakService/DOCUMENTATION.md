# Keycloak Service Documentation

## Overview and Responsibility

`KeycloakService` is the identity provider for the platform. It provides login, token issuance, realm roles, and user profile storage for the Vue frontend and the protected .NET APIs.

## Tools and Technologies Used

- Keycloak 26.1
- Custom container image with multi-stage build
- Realm template import at container startup
- SQL Server / Azure SQL as the configured database backend

## Identity Model and API Role

Key runtime facts:

- development realm template: `car-rental-dev-realm.json`
- production realm template: `car-rental-prod-realm.json`
- frontend client id is supplied through environment configuration
- main realm roles used by backend policies are `app-user` and `app-admin`

The frontend uses Keycloak JavaScript for:

- silent single sign-on check
- login and logout redirects
- profile loading
- bearer token refresh

The backend services use Keycloak JWT bearer authentication and realm-role authorization policies.

## Runtime Wiring and Dependencies

```text
Browser <-> KeycloakService
BookingService -> KeycloakService
CarService ----> KeycloakService
KeycloakService -> SQL database
```

The custom entrypoint:

1. chooses dev or prod realm template
2. replaces realm name, frontend URL, and SSL requirement placeholders
3. imports the rendered realm JSON
4. starts `kc.sh start-dev` or optimized production mode

Important environment variables:

- `KEYCLOAK_IMPORT_MODE`
- `KEYCLOAK_REALM_TEMPLATE`
- `KEYCLOAK_FRONTEND_URL`
- `KEYCLOAK_REALM_NAME`
- `KEYCLOAK_SSL_REQUIRED`
- `KC_BOOTSTRAP_ADMIN_USERNAME`
- `KC_BOOTSTRAP_ADMIN_PASSWORD`

## Validation and Error Handling

- startup fails if the selected realm template file is missing
- production import refuses to continue if the frontend URL is left at `http://localhost:5173`
- backend services can fall back to bootstrap-admin usage when full admin-client credentials are incomplete

## Code Quality Practices

- Build-only service in backend CI
- Realm configuration is versioned in JSON templates
- Docker image build bakes the theme and entrypoint into the final container

## Lessons Learned

### What does not work yet

- There are no automated tests in this repository that validate full realm import behavior.
- Identity behavior still depends on correct environment injection in each deployment target.

### Experience with technologies

- Centralized authentication through Keycloak keeps browser and API authorization aligned.
- Templated realm imports are more reproducible than manual console setup.

### Experience with AI tools used

- AI helped articulate the auth architecture and document the environment contract.
- The exact bootstrap and import behavior still required reading the Dockerfile and entrypoint script directly.
