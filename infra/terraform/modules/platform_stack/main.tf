locals {
  environment_code_by_name = { dev = "d", prod = "p" }
  environment_code         = lookup(local.environment_code_by_name, var.environment, substr(var.environment, 0, 1))
  stack_prefix             = "cr-${local.environment_code}-${var.stack_name}"
  name_prefix              = local.stack_prefix
  common_tags = merge(var.tags, {
    environment = var.environment
    stack       = var.stack_name
  })
  backend_mode    = var.stack_definition.backend_mode
  enable_rabbitmq = var.stack_definition.enable_rabbitmq
  enable_redis    = var.stack_definition.enable_redis

  sql_server_name               = "${local.stack_prefix}-sql"
  stateful_storage_account_name = substr(replace("${local.stack_prefix}state", "-", ""), 0, 24)

  frontend_keycloak_realm = coalesce(var.frontend_keycloak_realm, var.stack_definition.realm_name)

  frontend_app_name           = "${local.stack_prefix}-fe"
  nginx_gateway_app_name      = "${local.stack_prefix}-gw"
  car_rental_app_name         = "${local.stack_prefix}-api"
  car_app_name                = "${local.stack_prefix}-car"
  booking_app_name            = "${local.stack_prefix}-bkg"
  request_proxy_app_name      = "${local.stack_prefix}-prx"
  currency_converter_app_name = "${local.stack_prefix}-fx"
  keycloak_app_name           = "${local.stack_prefix}-kc"
  rabbitmq_app_name           = "${local.stack_prefix}-rmq"
  redis_app_name              = "${local.stack_prefix}-rds"

  car_rental_db_name = "CarRentalDB"
  keycloak_db_name   = "KeycloakDB"

  sql_fqdn = module.sql.server_fqdn

  car_rental_connection_string = "Server=tcp:${local.sql_fqdn},1433;Initial Catalog=${local.car_rental_db_name};Persist Security Info=False;User ID=${var.sql_admin_login};Password=${var.sql_admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  keycloak_jdbc_url            = "jdbc:sqlserver://${local.sql_fqdn}:1433;databaseName=${local.keycloak_db_name};encrypt=true;trustServerCertificate=false;hostNameInCertificate=*.database.windows.net;loginTimeout=30;"

  frontend_vite_api_base_url  = "https://${local.nginx_gateway_app_name}.${var.container_app_environment_default_domain}"
  frontend_vite_keycloak_url  = "https://${local.keycloak_app_name}.${var.container_app_environment_default_domain}"
  frontend_public_url         = "https://${local.frontend_app_name}.${var.container_app_environment_default_domain}"
  currency_converter_base_url = "https://${local.currency_converter_app_name}.internal.${var.container_app_environment_default_domain}/soap"
  currency_converter_grpc_url = "http://${local.currency_converter_app_name}"
  rabbitmq_hostname           = local.rabbitmq_app_name
  redis_address               = "${local.redis_app_name}:6379"
  gateway_api_upstream        = local.backend_mode == "monolith" ? local.car_rental_app_name : local.car_app_name
  gateway_cars_upstream       = local.backend_mode == "monolith" ? local.car_rental_app_name : local.car_app_name
  gateway_booking_upstream    = local.backend_mode == "monolith" ? local.car_rental_app_name : local.booking_app_name
  gateway_proxy_upstream      = local.request_proxy_app_name

  org_name = "timmy-2003"
  shared_apps = {
    frontend = {
      name         = local.frontend_app_name
      image        = "${var.ghcr_server}/${local.org_name}/car-rental-frontend:${var.stack_definition.image_tags.frontend}"
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
        VITE_KEYCLOAK_REALM     = local.frontend_keycloak_realm
        VITE_KEYCLOAK_CLIENT_ID = var.frontend_keycloak_client_id
      }
      secret_env = {}
    }

    nginx_gateway = {
      name         = local.nginx_gateway_app_name
      image        = "${var.ghcr_server}/${local.org_name}/nginx-gateway:${var.stack_definition.image_tags.nginx_gateway}"
      external     = true
      target_port  = 8080
      transport    = "auto"
      min_replicas = 1
      max_replicas = 1
      cpu          = 0.25
      memory       = "0.5Gi"
      env = {
        CORS_ALLOWED_ORIGIN   = local.frontend_public_url
        API_UPSTREAM          = local.gateway_api_upstream
        CARS_UPSTREAM         = local.gateway_cars_upstream
        BOOKING_UPSTREAM      = local.gateway_booking_upstream
        REQUEST_PROXY_UPSTREAM = local.gateway_proxy_upstream
      }
      secret_env = {}
    }

    request_proxy_service = {
      name         = local.request_proxy_app_name
      image        = "${var.ghcr_server}/${local.org_name}/request-proxy-service:${var.stack_definition.image_tags.request_proxy_service}"
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
      image        = "${var.ghcr_server}/${local.org_name}/currency-converter-service:${var.stack_definition.image_tags.currency_converter_service}"
      external     = false
      target_port  = 8080
      transport    = local.enable_redis ? "http2" : "auto"
      min_replicas = 1
      max_replicas = 1
      cpu          = 0.25
      memory       = "0.5Gi"
      env = local.enable_redis ? {
        REDIS_ADDR        = local.redis_address
        REDIS_TLS_ENABLED = "false"
      } : {}
      secret_env = merge(
        {
          SOAP_USERNAME = var.currency_converter_soap_username
          SOAP_PASSWORD = var.currency_converter_soap_password
        },
        local.enable_redis ? {
          REDIS_PASSWORD = var.redis_password
        } : {}
      )
    }

    keycloak = {
      name         = local.keycloak_app_name
      image        = "${var.ghcr_server}/${local.org_name}/keycloak-service:${var.stack_definition.image_tags.keycloak}"
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
        KEYCLOAK_REALM_NAME      = local.frontend_keycloak_realm
        KEYCLOAK_SSL_REQUIRED    = "external"
      }
      secret_env = {
        KC_DB_PASSWORD              = var.sql_admin_password
        KC_BOOTSTRAP_ADMIN_USERNAME = var.keycloak_admin_username
        KC_BOOTSTRAP_ADMIN_PASSWORD = var.keycloak_admin_password
      }
    }
  }

  split_backend_apps = local.backend_mode == "split" ? {
    car_service = {
      name         = local.car_app_name
      image        = "${var.ghcr_server}/${local.org_name}/car-service:${var.stack_definition.image_tags.car_service}"
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
        Keycloak__realm                    = local.frontend_keycloak_realm
        Keycloak__resource                 = var.frontend_keycloak_client_id
        Keycloak__auth-server-url          = "${local.frontend_vite_keycloak_url}/"
        Keycloak__verify-token-audience    = "false"
        KeycloakAdmin__authServerUrl       = "${local.frontend_vite_keycloak_url}/"
        KeycloakAdmin__realm               = local.frontend_keycloak_realm
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
      image        = "${var.ghcr_server}/${local.org_name}/booking-service:${var.stack_definition.image_tags.booking_service}"
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
        Keycloak__realm                    = local.frontend_keycloak_realm
        Keycloak__resource                 = var.frontend_keycloak_client_id
        Keycloak__auth-server-url          = "${local.frontend_vite_keycloak_url}/"
        Keycloak__verify-token-audience    = "false"
        KeycloakAdmin__authServerUrl       = "${local.frontend_vite_keycloak_url}/"
        KeycloakAdmin__realm               = local.frontend_keycloak_realm
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
  } : {}

  monolith_backend_apps = local.backend_mode == "monolith" ? {
    car_rental_service = {
      name         = local.car_rental_app_name
      image        = "${var.ghcr_server}/${local.org_name}/car-rental-service:${var.stack_definition.image_tags.car_rental_service}"
      external     = false
      target_port  = 8080
      transport    = "auto"
      min_replicas = 1
      max_replicas = 1
      cpu          = 0.5
      memory       = "1Gi"
      env = {
        ASPNETCORE_ENVIRONMENT             = "Production"
        CurrencyConverterSettings__BaseUrl = local.currency_converter_base_url
        Keycloak__realm                    = local.frontend_keycloak_realm
        Keycloak__resource                 = var.frontend_keycloak_client_id
        Keycloak__auth-server-url          = "${local.frontend_vite_keycloak_url}/"
        Keycloak__verify-token-audience    = "false"
        KeycloakAdmin__authServerUrl       = "${local.frontend_vite_keycloak_url}/"
        KeycloakAdmin__realm               = local.frontend_keycloak_realm
      }
      secret_env = {
        ConnectionStrings__DefaultConnection = local.car_rental_connection_string
        CurrencyConverterSettings__Username  = var.currency_converter_soap_username
        CurrencyConverterSettings__Password  = var.currency_converter_soap_password
        KeycloakBootstrapAdmin__Username     = var.keycloak_admin_username
        KeycloakBootstrapAdmin__Password     = var.keycloak_admin_password
      }
    }
  } : {}

  stateless_apps = merge(local.shared_apps, local.split_backend_apps, local.monolith_backend_apps)
  rabbitmq_modules = local.enable_rabbitmq ? { primary = true } : {}
  redis_modules    = local.enable_redis ? { primary = true } : {}
}

module "resource_group" {
  source   = "../resource_group"
  name     = "${local.name_prefix}-rg"
  location = var.location
  tags     = local.common_tags
  }

module "sql" {
  source              = "../sql"
  server_name         = local.sql_server_name
  resource_group_name = module.resource_group.name
  location            = var.location
  admin_login         = var.sql_admin_login
  admin_password      = var.sql_admin_password
  database_names      = [local.car_rental_db_name, local.keycloak_db_name]
  tags                = local.common_tags
}

module "rabbitmq" {
  for_each = local.rabbitmq_modules
  source = "../rabbitmq"

  name                         = local.rabbitmq_app_name
  resource_group_name          = module.resource_group.name
  location                     = var.location
  container_app_environment_id = var.container_app_environment_id
  storage_account_name         = local.stateful_storage_account_name
  username                     = var.rabbitmq_username
  password                     = var.rabbitmq_password
  tags                         = local.common_tags
}

module "redis" {
  for_each = local.redis_modules
  source = "../redis"

  name                         = local.redis_app_name
  resource_group_name          = module.resource_group.name
  container_app_environment_id = var.container_app_environment_id
  password                     = var.redis_password
  tags                         = local.common_tags
}

module "apps" {
  source = "../container_app"

  for_each = local.stateless_apps

  name                         = each.value.name
  resource_group_name          = module.resource_group.name
  container_app_environment_id = var.container_app_environment_id
  revision_mode                = "Single"

  registry_server   = var.ghcr_server
  registry_username = var.ghcr_username
  registry_password = var.ghcr_password

  image        = each.value.image
  external     = each.value.external
  target_port  = each.value.target_port
  transport    = each.value.transport
  min_replicas = each.value.min_replicas
  max_replicas = each.value.max_replicas
  cpu          = each.value.cpu
  memory       = each.value.memory

  env        = each.value.env
  secret_env = each.value.secret_env

  tags = local.common_tags
}
