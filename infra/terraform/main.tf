locals {
  deployed_stacks               = ["v1", "v2"]
  environment_code_by_name      = { dev = "d", prod = "p" }
  environment_code              = lookup(local.environment_code_by_name, var.environment, substr(var.environment, 0, 1))
  shared_name_prefix            = "cr-${local.environment_code}"
  shared_log_analytics_name     = "${local.shared_name_prefix}-law"
  shared_containerapps_env_name = "${local.shared_name_prefix}-acae"
}

module "shared_resource_group" {
  source   = "./modules/resource_group"
  name     = "${local.shared_name_prefix}-shared-rg"
  location = var.location
  tags     = var.tags
}

module "shared_log_analytics" {
  source              = "./modules/log_analytics"
  name                = local.shared_log_analytics_name
  location            = var.location
  resource_group_name = module.shared_resource_group.name
  tags                = var.tags
}

module "shared_container_apps_env" {
  source                     = "./modules/container_apps_env"
  name                       = local.shared_containerapps_env_name
  location                   = var.location
  resource_group_name        = module.shared_resource_group.name
  log_analytics_workspace_id = module.shared_log_analytics.id
  tags                       = var.tags
}

module "stacks" {
  source   = "./modules/platform_stack"
  for_each = { for stack_name in local.deployed_stacks : stack_name => local.stack_versions[stack_name] }

  project_name                             = var.project_name
  environment                              = var.environment
  stack_name                               = each.key
  location                                 = var.location
  container_app_environment_id             = module.shared_container_apps_env.id
  container_app_environment_name           = module.shared_container_apps_env.name
  container_app_environment_default_domain = module.shared_container_apps_env.default_domain
  tags                                     = var.tags

  ghcr_server   = var.ghcr_server
  ghcr_username = var.ghcr_username
  ghcr_password = var.ghcr_password

  sql_admin_login    = var.sql_admin_login
  sql_admin_password = var.sql_admin_password

  google_maps_api_key         = var.google_maps_api_key
  pexels_api_key              = var.pexels_api_key
  keycloak_admin_username     = var.keycloak_admin_username
  keycloak_admin_password     = var.keycloak_admin_password
  frontend_keycloak_realm     = var.frontend_keycloak_realm
  frontend_keycloak_client_id = var.frontend_keycloak_client_id

  currency_converter_soap_username = var.currency_converter_soap_username
  currency_converter_soap_password = var.currency_converter_soap_password
  rabbitmq_username                = var.rabbitmq_username
  rabbitmq_password                = var.rabbitmq_password
  rabbitmq_car_exchange            = var.rabbitmq_car_exchange
  rabbitmq_booking_exchange        = var.rabbitmq_booking_exchange
  redis_password                   = var.redis_password
  keycloak_import_override         = var.keycloak_import_override

  stack_definition = each.value
}
