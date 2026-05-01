variable "project_name" {
  type        = string
  description = "Short project name used in resource naming."
  default     = "car-rental"
}

variable "environment" {
  type        = string
  description = "Deployment environment."
  default     = "prod"
}

variable "location" {
  type        = string
  description = "Azure region."
  default     = "westeurope"
}

variable "tags" {
  type        = map(string)
  description = "Common tags."
  default = {
    app         = "car-rental"
    environment = "prod"
    managed_by  = "terraform"
  }
}

variable "ghcr_server" {
  type        = string
  description = "Container registry server."
  default     = "ghcr.io"
}

variable "ghcr_username" {
  type        = string
  description = "GHCR username."
  sensitive   = true
}

variable "ghcr_password" {
  type        = string
  description = "GHCR token/password."
  sensitive   = true
}

variable "sql_admin_login" {
  type        = string
  description = "Azure SQL admin login."
  sensitive   = true
}

variable "sql_admin_password" {
  type        = string
  description = "Azure SQL admin password."
  sensitive   = true
}

variable "google_maps_api_key" {
  type        = string
  sensitive   = true
  description = "Google Maps API key for request-proxy-service."
}

variable "pexels_api_key" {
  type        = string
  sensitive   = true
  description = "Pexels API key for request-proxy-service."
}

variable "keycloak_admin_username" {
  type        = string
  sensitive   = true
  description = "Keycloak bootstrap admin username."
}

variable "keycloak_admin_password" {
  type        = string
  sensitive   = true
  description = "Keycloak bootstrap admin password."
}

variable "frontend_keycloak_realm" {
  type        = string
  description = "Runtime VITE_KEYCLOAK_REALM and Keycloak realm name used by the frontend."
  default     = "car-rental-prod"
}

variable "frontend_keycloak_client_id" {
  type        = string
  description = "Runtime VITE_KEYCLOAK_CLIENT_ID used by the frontend."
  default     = "car-rental-frontend"
}

variable "currency_converter_soap_username" {
  type        = string
  sensitive   = true
  description = "Basic auth username for currency-converter SOAP endpoint."
}

variable "currency_converter_soap_password" {
  type        = string
  sensitive   = true
  description = "Basic auth password for currency-converter SOAP endpoint."
}

variable "rabbitmq_username" {
  type        = string
  description = "Username for the shared RabbitMQ broker."
  default     = "emre"
}

variable "rabbitmq_password" {
  type        = string
  sensitive   = true
  description = "Password for the shared RabbitMQ broker."
}

variable "rabbitmq_car_exchange" {
  type        = string
  description = "RabbitMQ exchange used for car events."
  default     = "car_exchange"
}

variable "rabbitmq_booking_exchange" {
  type        = string
  description = "RabbitMQ exchange used for booking events."
  default     = "booking_exchange"
}

variable "redis_password" {
  type        = string
  sensitive   = true
  description = "Password for the Redis cache."
}

variable "keycloak_import_override" {
  type        = string
  description = "Whether Keycloak import should overwrite an existing realm ('true' or 'false')."
  default     = "false"
}

variable "image_tags" {
  type = object({
    frontend                   = string
    nginx_gateway              = string
    car_service                = string
    booking_service            = string
    request_proxy_service      = string
    currency_converter_service = string
    keycloak                   = string
  })

  description = "Immutable image tags to deploy."
}
