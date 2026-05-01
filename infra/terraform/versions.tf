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
