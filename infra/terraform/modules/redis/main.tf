resource "azurerm_container_app" "this" {
  name                         = var.name
  resource_group_name          = var.resource_group_name
  container_app_environment_id = var.container_app_environment_id
  revision_mode                = "Single"
  tags                         = var.tags

  secret {
    name  = "redis-password"
    value = var.password
  }

  template {
    min_replicas = 1
    max_replicas = 1

    container {
      name   = var.name
      image  = "docker.io/bitnami/redis:latest"
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "ALLOW_EMPTY_PASSWORD"
        value = "no"
      }

      env {
        name  = "REDIS_AOF_ENABLED"
        value = "yes"
      }

      env {
        name        = "REDIS_PASSWORD"
        secret_name = "redis-password"
      }
    }
  }

  ingress {
    external_enabled           = false
    target_port                = 6379
    allow_insecure_connections = false
    transport                  = "tcp"

    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }
}
