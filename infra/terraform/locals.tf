locals {
  name_prefix = "${var.project_name}-${var.environment}"

  sql_server_name               = replace(substr("${local.name_prefix}-sql", 0, 60), "_", "-")
  log_analytics_name            = replace(substr("${local.name_prefix}-law", 0, 63), "_", "-")
  containerapps_env_name        = replace(substr("${local.name_prefix}-aca-env", 0, 32), "_", "-")
  stateful_storage_account_name = substr(replace("${var.project_name}${var.environment}stateful", "-", ""), 0, 24)

  frontend_app_name           = "car-rental-frontend"
  nginx_gateway_app_name      = "nginx-gateway"
  car_app_name                = "car-service"
  booking_app_name            = "booking-service"
  request_proxy_app_name      = "request-proxy-service"
  currency_converter_app_name = "currency-converter-service"
  keycloak_app_name           = "keycloak-service"
  rabbitmq_app_name           = "rabbitmq"
  redis_app_name              = "redis"

  car_rental_db_name = "CarRentalDB"
  keycloak_db_name   = "KeycloakDB"

  sql_fqdn = module.sql.server_fqdn

  car_rental_connection_string = "Server=tcp:${local.sql_fqdn},1433;Initial Catalog=${local.car_rental_db_name};Persist Security Info=False;User ID=${var.sql_admin_login};Password=${var.sql_admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  keycloak_jdbc_url            = "jdbc:sqlserver://${local.sql_fqdn}:1433;databaseName=${local.keycloak_db_name};encrypt=true;trustServerCertificate=false;hostNameInCertificate=*.database.windows.net;loginTimeout=30;"

  frontend_vite_api_base_url  = "https://${local.nginx_gateway_app_name}.${module.container_apps_env.default_domain}"
  frontend_vite_keycloak_url  = "https://${local.keycloak_app_name}.${module.container_apps_env.default_domain}"
  frontend_public_url         = "https://${local.frontend_app_name}.${module.container_apps_env.default_domain}"
  currency_converter_grpc_url = "http://${local.currency_converter_app_name}"
  rabbitmq_hostname           = local.rabbitmq_app_name
  redis_address               = "${local.redis_app_name}:6379"

  org_name = "timmy-2003"
  stateless_apps = {
    frontend = {
      name         = local.frontend_app_name
      image        = "${var.ghcr_server}/${local.org_name}/car-rental-frontend:${var.image_tags.frontend}"
      external     = true
      target_port  = 80
      transport    = "auto"
      min_replicas = 1
      max_replicas = 1
      cpu          = 0.25
      memory       = "0.5Gi"
      env = {
        NODE_ENV                = "production"
        VITE_API_BASE_URL       = local.frontend_vite_api_base_url
        VITE_KEYCLOAK_URL       = local.frontend_vite_keycloak_url
        VITE_KEYCLOAK_REALM     = var.frontend_keycloak_realm
        VITE_KEYCLOAK_CLIENT_ID = var.frontend_keycloak_client_id
      }
      secret_env = {}
    }

    nginx_gateway = {
      name         = local.nginx_gateway_app_name
      image        = "${var.ghcr_server}/${local.org_name}/nginx-gateway:${var.image_tags.nginx_gateway}"
      external     = true
      target_port  = 8080
      transport    = "auto"
      min_replicas = 1
      max_replicas = 1
      cpu          = 0.25
      memory       = "0.5Gi"
      env = {
        CORS_ALLOWED_ORIGIN = local.frontend_public_url
      }
      secret_env = {}
    }

    car_service = {
      name         = local.car_app_name
      image        = "${var.ghcr_server}/${local.org_name}/car-service:${var.image_tags.car_service}"
      external     = false
      target_port  = 8080
      transport    = "auto"
      min_replicas = 1
      max_replicas = 1
      cpu          = 0.5
      memory       = "1Gi"
      env = {
        ASPNETCORE_ENVIRONMENT             = "Production"
        Cors__AllowedOrigin                = local.frontend_public_url
        CurrencyConverterSettings__GrpcUrl = local.currency_converter_grpc_url
        RabbitMQ__HostName                 = local.rabbitmq_hostname
        RabbitMQ__Port                     = "5672"
        RabbitMQ__UserName                 = var.rabbitmq_username
        RabbitMQ__CarExchange              = var.rabbitmq_car_exchange
        RabbitMQ__BookingExchange          = var.rabbitmq_booking_exchange
        Keycloak__realm                    = var.frontend_keycloak_realm
        Keycloak__resource                 = var.frontend_keycloak_client_id
        Keycloak__auth-server-url          = "${local.frontend_vite_keycloak_url}/"
        Keycloak__verify-token-audience    = "false"
        KeycloakAdmin__authServerUrl       = "${local.frontend_vite_keycloak_url}/"
        KeycloakAdmin__realm               = var.frontend_keycloak_realm
      }
      secret_env = {
        ConnectionStrings__DefaultConnection = local.car_rental_connection_string
        CurrencyConverterSettings__Username  = var.currency_converter_soap_username
        CurrencyConverterSettings__Password  = var.currency_converter_soap_password
        RabbitMQ__Password                   = var.rabbitmq_password
        KeycloakBootstrapAdmin__Username     = var.keycloak_admin_username
        KeycloakBootstrapAdmin__Password     = var.keycloak_admin_password
      }
    }

    booking_service = {
      name         = local.booking_app_name
      image        = "${var.ghcr_server}/${local.org_name}/booking-service:${var.image_tags.booking_service}"
      external     = false
      target_port  = 8080
      transport    = "auto"
      min_replicas = 1
      max_replicas = 1
      cpu          = 0.5
      memory       = "1Gi"
      env = {
        ASPNETCORE_ENVIRONMENT             = "Production"
        Cors__AllowedOrigin                = local.frontend_public_url
        CurrencyConverterSettings__GrpcUrl = local.currency_converter_grpc_url
        RabbitMQ__HostName                 = local.rabbitmq_hostname
        RabbitMQ__Port                     = "5672"
        RabbitMQ__UserName                 = var.rabbitmq_username
        RabbitMQ__CarExchange              = var.rabbitmq_car_exchange
        RabbitMQ__BookingExchange          = var.rabbitmq_booking_exchange
        Keycloak__realm                    = var.frontend_keycloak_realm
        Keycloak__resource                 = var.frontend_keycloak_client_id
        Keycloak__auth-server-url          = "${local.frontend_vite_keycloak_url}/"
        Keycloak__verify-token-audience    = "false"
        KeycloakAdmin__authServerUrl       = "${local.frontend_vite_keycloak_url}/"
        KeycloakAdmin__realm               = var.frontend_keycloak_realm
      }
      secret_env = {
        ConnectionStrings__DefaultConnection = local.car_rental_connection_string
        CurrencyConverterSettings__Username  = var.currency_converter_soap_username
        CurrencyConverterSettings__Password  = var.currency_converter_soap_password
        RabbitMQ__Password                   = var.rabbitmq_password
        KeycloakBootstrapAdmin__Username     = var.keycloak_admin_username
        KeycloakBootstrapAdmin__Password     = var.keycloak_admin_password
      }
    }

    request_proxy_service = {
      name         = local.request_proxy_app_name
      image        = "${var.ghcr_server}/${local.org_name}/request-proxy-service:${var.image_tags.request_proxy_service}"
      external     = false
      target_port  = 8080
      transport    = "auto"
      min_replicas = 1
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
      name         = local.currency_converter_app_name
      image        = "${var.ghcr_server}/${local.org_name}/currency-converter-service:${var.image_tags.currency_converter_service}"
      external     = false
      target_port  = 8080
      transport    = "http2"
      min_replicas = 1
      max_replicas = 1
      cpu          = 0.25
      memory       = "0.5Gi"
      env = {
        REDIS_ADDR        = local.redis_address
        REDIS_TLS_ENABLED = "false"
      }
      secret_env = {
        SOAP_USERNAME  = var.currency_converter_soap_username
        SOAP_PASSWORD  = var.currency_converter_soap_password
        REDIS_PASSWORD = var.redis_password
      }
    }

    keycloak = {
      name         = local.keycloak_app_name
      image        = "${var.ghcr_server}/${local.org_name}/keycloak-service:${var.image_tags.keycloak}"
      external     = true
      target_port  = 8080
      transport    = "auto"
      min_replicas = 1
      max_replicas = 1
      cpu          = 0.5
      memory       = "1Gi"
      env = {
        KC_DB                    = "mssql"
        KC_DB_URL                = local.keycloak_jdbc_url
        KC_DB_USERNAME           = var.sql_admin_login
        KC_HOSTNAME_STRICT       = "false"
        KC_HTTP_ENABLED          = "true"
        KC_PROXY_HEADERS         = "xforwarded"
        KEYCLOAK_IMPORT_MODE     = "prod"
        KEYCLOAK_IMPORT_OVERRIDE = var.keycloak_import_override
        KEYCLOAK_REALM_TEMPLATE  = "car-rental-prod-realm.json"
        KEYCLOAK_FRONTEND_URL    = local.frontend_public_url
        KEYCLOAK_REALM_NAME      = var.frontend_keycloak_realm
        KEYCLOAK_SSL_REQUIRED    = "external"
      }
      secret_env = {
        KC_DB_PASSWORD              = var.sql_admin_password
        KC_BOOTSTRAP_ADMIN_USERNAME = var.keycloak_admin_username
        KC_BOOTSTRAP_ADMIN_PASSWORD = var.keycloak_admin_password
      }
    }
  }
}
