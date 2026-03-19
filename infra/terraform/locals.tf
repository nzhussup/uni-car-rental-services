locals {
  name_prefix = "${var.project_name}-${var.environment}"

  sql_server_name        = replace(substr("${local.name_prefix}-sql", 0, 60), "_", "-")
  log_analytics_name     = replace(substr("${local.name_prefix}-law", 0, 63), "_", "-")
  containerapps_env_name = replace(substr("${local.name_prefix}-aca-env", 0, 32), "_", "-")

  car_rental_db_name = "CarRentalDB"
  keycloak_db_name   = "KeycloakDB"

  sql_fqdn = module.sql.server_fqdn

  car_rental_connection_string = "Server=tcp:${local.sql_fqdn},1433;Initial Catalog=${local.car_rental_db_name};Persist Security Info=False;User ID=${var.sql_admin_login};Password=${var.sql_admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  keycloak_jdbc_url            = "jdbc:sqlserver://${local.sql_fqdn}:1433;databaseName=${local.keycloak_db_name};encrypt=true;trustServerCertificate=false;hostNameInCertificate=*.database.windows.net;loginTimeout=30;"

  org_name = "timmy-2003"
  apps = {
    frontend = {
      name         = "car-rental-frontend"
      image        = "${var.ghcr_server}/${local.org_name}/car-rental-frontend:${var.image_tags.frontend}"
      external     = true
      target_port  = 80
      min_replicas = 0
      max_replicas = 1
      cpu          = 0.25
      memory       = "0.5Gi"
      env = {
        NODE_ENV = "production"
      }
      secret_env = {}
    }

    nginx_gateway = {
      name         = "nginx-gateway"
      image        = "${var.ghcr_server}/${local.org_name}/nginx-gateway:${var.image_tags.nginx_gateway}"
      external     = true
      target_port  = 8080
      min_replicas = 0
      max_replicas = 1
      cpu          = 0.25
      memory       = "0.5Gi"
      env = {
        CORS_ALLOWED_ORIGIN = "https://REPLACE_ME"
      }
      secret_env = {}
    }

    car_rental_service = {
      name         = "car-rental-service"
      image        = "${var.ghcr_server}/${local.org_name}/car-rental-service:${var.image_tags.car_rental_service}"
      external     = false
      target_port  = 8080
      min_replicas = 0
      max_replicas = 1
      cpu          = 0.5
      memory       = "1Gi"
      env = {
        ASPNETCORE_ENVIRONMENT      = "Production"
        CURRENCY_CONVERTER_BASE_URL = "http://currency-converter-service"
      }
      secret_env = {
        ConnectionStrings__DefaultConnection = local.car_rental_connection_string
      }
    }

    request_proxy_service = {
      name         = "request-proxy-service"
      image        = "${var.ghcr_server}/${local.org_name}/request-proxy-service:${var.image_tags.request_proxy_service}"
      external     = false
      target_port  = 8080
      min_replicas = 0
      max_replicas = 1
      cpu          = 0.25
      memory       = "0.5Gi"
      env          = {}
      secret_env = {
        GOOGLE_MAPS_API_KEY = var.google_maps_api_key
        PEXELS_API_KEY      = var.pexels_api_key
      }
    }

    currency_converter_service = {
      name         = "currency-converter-service"
      image        = "${var.ghcr_server}/${local.org_name}/currency-converter-service:${var.image_tags.currency_converter_service}"
      external     = false
      target_port  = 8080
      min_replicas = 0
      max_replicas = 1
      cpu          = 0.25
      memory       = "0.5Gi"
      env          = {}
      secret_env   = {}
    }

    keycloak = {
      name         = "keycloak-service"
      image        = "${var.ghcr_server}/${local.org_name}/keycloak-service:${var.image_tags.keycloak}"
      external     = true
      target_port  = 8080
      min_replicas = 0
      max_replicas = 1
      cpu          = 0.5
      memory       = "1Gi"
      env = {
        KC_DB                   = "mssql"
        KC_DB_URL               = local.keycloak_jdbc_url
        KC_DB_USERNAME          = var.sql_admin_login
        KC_HOSTNAME_STRICT      = "false"
        KC_HTTP_ENABLED         = "true"
        KC_PROXY_HEADERS        = "xforwarded"
        KEYCLOAK_IMPORT_MODE    = "prod"
        KEYCLOAK_REALM_TEMPLATE = "car-rental-prod-realm.json"
        KEYCLOAK_FRONTEND_URL   = var.frontend_public_url
        KEYCLOAK_REALM_NAME     = "car-rental-prod"
        KEYCLOAK_SSL_REQUIRED   = "external"
      }
      secret_env = {
        KC_DB_PASSWORD              = var.sql_admin_password
        KC_BOOTSTRAP_ADMIN_USERNAME = var.keycloak_admin_username
        KC_BOOTSTRAP_ADMIN_PASSWORD = var.keycloak_admin_password
      }
    }
  }
}
