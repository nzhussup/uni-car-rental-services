module "resource_group" {
  source   = "./modules/resource_group"
  name     = "${local.name_prefix}-rg"
  location = var.location
  tags     = var.tags
}

module "log_analytics" {
  source              = "./modules/log_analytics"
  name                = local.log_analytics_name
  location            = var.location
  resource_group_name = module.resource_group.name
  tags                = var.tags
}

module "container_apps_env" {
  source                     = "./modules/container_apps_env"
  name                       = local.containerapps_env_name
  location                   = var.location
  resource_group_name        = module.resource_group.name
  log_analytics_workspace_id = module.log_analytics.id
  tags                       = var.tags
}

module "sql" {
  source              = "./modules/sql"
  server_name         = local.sql_server_name
  resource_group_name = module.resource_group.name
  location            = var.location
  admin_login         = var.sql_admin_login
  admin_password      = var.sql_admin_password
  database_names      = [local.car_rental_db_name, local.keycloak_db_name]
  tags                = var.tags
}

module "rabbitmq" {
  source = "./modules/rabbitmq"

  name                         = local.rabbitmq_app_name
  resource_group_name          = module.resource_group.name
  location                     = var.location
  container_app_environment_id = module.container_apps_env.id
  storage_account_name         = local.stateful_storage_account_name
  username                     = var.rabbitmq_username
  password                     = var.rabbitmq_password
  tags                         = var.tags
}

module "redis" {
  source = "./modules/redis"

  name                         = local.redis_app_name
  resource_group_name          = module.resource_group.name
  container_app_environment_id = module.container_apps_env.id
  password                     = var.redis_password
  tags                         = var.tags
}

module "apps" {
  source = "./modules/container_app"

  for_each = local.stateless_apps

  name                         = each.value.name
  resource_group_name          = module.resource_group.name
  container_app_environment_id = module.container_apps_env.id
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

  tags = var.tags
}
