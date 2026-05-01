locals {
  registry_secret_name = "registry-password"
}

resource "azurerm_container_app" "this" {
  name                         = var.name
  resource_group_name          = var.resource_group_name
  container_app_environment_id = var.container_app_environment_id
  revision_mode                = var.revision_mode
  tags                         = var.tags

  secret {
    name  = local.registry_secret_name
    value = var.registry_password
  }

  dynamic "secret" {
    for_each = var.secret_env
    content {
      name  = lower(replace(replace(secret.key, "_", "-"), ".", "-"))
      value = secret.value
    }
  }

  registry {
    server               = var.registry_server
    username             = var.registry_username
    password_secret_name = local.registry_secret_name
  }

  template {
    min_replicas = var.min_replicas
    max_replicas = var.max_replicas

    container {
      name   = var.name
      image  = var.image
      cpu    = var.cpu
      memory = var.memory

      dynamic "env" {
        for_each = var.env
        content {
          name  = env.key
          value = env.value
        }
      }

      dynamic "env" {
        for_each = var.secret_env
        content {
          name        = env.key
          secret_name = lower(replace(replace(env.key, "_", "-"), ".", "-"))
        }
      }

    }
  }

  ingress {
    external_enabled           = var.external
    target_port                = var.target_port
    allow_insecure_connections = false
    transport                  = var.transport

    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }
}
