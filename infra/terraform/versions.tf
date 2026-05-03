terraform {
  required_version = ">= 1.6.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.64"
    }
  }
}

provider "azurerm" {
  features {}

  resource_provider_registrations = "none"
}

// For azurerem to work prepregister the resource providers
# az provider register --namespace Microsoft.App --wait
# az provider register --namespace Microsoft.OperationalInsights --wait
# az provider register --namespace Microsoft.Sql --wait
# az provider register --namespace Microsoft.Resources --wait
# az provider register --namespace Microsoft.Storage --wait
// Then check if they're registered
# az provider show --namespace Microsoft.App --query registrationState -o tsv
# az provider show --namespace Microsoft.OperationalInsights --query registrationState -o tsv
# az provider show --namespace Microsoft.Sql --query registrationState -o tsv
# az provider show --namespace Microsoft.Resources --query registrationState -o tsv
# az provider show --namespace Microsoft.Storage --query registrationState -o tsv
// This should return "Registered" for all of them

locals {
  stack_versions = {
    v1 = {
      backend_mode    = "monolith"
      enable_rabbitmq = false
      enable_redis    = false
      realm_name      = "car-rental-prod"
      image_tags = {
        frontend                   = "v1.0.0"
        nginx_gateway              = "v2.0.0"
        car_rental_service         = "v1.0.0"
        request_proxy_service      = "v1.0.0"
        currency_converter_service = "v1.0.0"
        keycloak                   = "v1.0.0"
      }
    }
    v2 = {
      backend_mode    = "split"
      enable_rabbitmq = true
      enable_redis    = true
      realm_name      = "car-rental-prod"
      image_tags = {
        frontend                   = "v2.0.0"
        nginx_gateway              = "v2.0.0"
        car_service                = "v2.0.0"
        booking_service            = "v2.0.0"
        request_proxy_service      = "v2.0.0"
        currency_converter_service = "v2.0.0"
        keycloak                   = "v2.0.0"
      }
    }
  }
}
