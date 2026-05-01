resource "azurerm_storage_account" "this" {
  name                     = var.storage_account_name
  resource_group_name      = var.resource_group_name
  location                 = var.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  min_tls_version          = "TLS1_2"

  tags = var.tags
}

resource "azurerm_storage_share" "this" {
  name               = "rabbitmq-data"
  storage_account_id = azurerm_storage_account.this.id
  quota              = 20
}

resource "azurerm_container_app_environment_storage" "this" {
  name                         = "rabbitmq-data"
  container_app_environment_id = var.container_app_environment_id
  account_name                 = azurerm_storage_account.this.name
  access_key                   = azurerm_storage_account.this.primary_access_key
  share_name                   = azurerm_storage_share.this.name
  access_mode                  = "ReadWrite"
}

resource "azurerm_container_app" "this" {
  name                         = var.name
  resource_group_name          = var.resource_group_name
  container_app_environment_id = var.container_app_environment_id
  revision_mode                = "Single"
  tags                         = var.tags

  secret {
    name  = "rabbitmq-default-pass"
    value = var.password
  }

  template {
    min_replicas = 1
    max_replicas = 1

    container {
      name   = var.name
      image  = "docker.io/library/rabbitmq:3-management"
      cpu    = 0.5
      memory = "1Gi"

      env {
        name  = "RABBITMQ_DEFAULT_USER"
        value = var.username
      }

      env {
        name  = "RABBITMQ_MNESIA_BASE"
        value = "/mnt/rabbitmq/mnesia"
      }

      env {
        name        = "RABBITMQ_DEFAULT_PASS"
        secret_name = "rabbitmq-default-pass"
      }

      volume_mounts {
        name = "rabbitmq-data"
        path = "/mnt/rabbitmq"
      }
    }

    volume {
      name         = "rabbitmq-data"
      storage_name = azurerm_container_app_environment_storage.this.name
      storage_type = "AzureFile"
    }
  }

  ingress {
    external_enabled           = false
    target_port                = 5672
    allow_insecure_connections = false
    transport                  = "tcp"

    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }
}
