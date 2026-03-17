# Production Setup

## Stack

- Azure Container Apps
- GitHub Container Registry (`ghcr.io`)
- Custom NGINX gateway container
- Azure SQL Database
- Azure Container Apps secrets
- Terraform
- GitHub Actions

## Apps

- `frontend`: public
- `nginx-gateway`: public
- `car-rental-service`: internal only
- `request-proxy-service`: internal only
- `currency-converter-service`: internal only
- `keycloak`: public on its own hostname

## Routing

- `<frontend-azure-fqdn>` -> `frontend`
- `<gateway-azure-fqdn>` -> `nginx-gateway`
- `<keycloak-azure-fqdn>` -> `keycloak`
- `nginx-gateway` -> `car-rental-service`
- `nginx-gateway` -> `request-proxy-service`
- `car-rental-service` -> `currency-converter-service`

## Gateway

- Use a dedicated NGINX container as the public backend entry point
- Keep the custom `nginx.conf`
- Route `/api/` to `car-rental-service`
- Route `/api/proxy/` to `request-proxy-service`
- Keep CORS and proxy headers in NGINX
- Do not expose backend services directly

## Images

- Images stay in `ghcr.io`
- CI/CD deploys containers to Azure Container Apps
- Prefer immutable tags even if `latest` is also published

## Secrets

- GitHub Secrets: CI/CD only
- Azure Container Apps secrets: runtime secrets
- Add Azure Key Vault later only if needed

## Database

- Use Azure SQL Database
- App DB: `CarRentalDB`
- Keycloak DB: `KeycloakDB`
- `car-rental-service` uses `ConnectionStrings__DefaultConnection`

## Keycloak

- Public because browser login redirects to it
- Keep admin access for the master realm
- Use a strong bootstrap admin user
- Persist Keycloak data in Azure SQL Database
- Bake custom theme into the Keycloak image
- Import predefined realm, roles, and clients on first startup
- Keep Keycloak on its own hostname, not behind the NGINX gateway

## Deploy Flow

1. Build image
2. Push image to `ghcr.io`
3. Deploy new Container App revision
4. Run DB migration job

## Notes

- Do not use Docker Compose in production
- Do not expose `currency-converter-service` publicly
- Keep one backend entry point through `nginx-gateway`
- Keep Keycloak public, but isolated on its own hostname
- Keep backend routing logic in the custom NGINX config
- Use Azure-provided hostnames until a custom domain is added
- Keep internal services reachable only inside the Container Apps environment
