variable "project_name" {
  type = string
}

variable "environment" {
  type = string
}

variable "stack_name" {
  type = string
}

variable "location" {
  type = string
}

variable "container_app_environment_id" {
  type = string
}

variable "container_app_environment_name" {
  type = string
}

variable "container_app_environment_default_domain" {
  type = string
}

variable "tags" {
  type = map(string)
}

variable "ghcr_server" {
  type = string
}

variable "ghcr_username" {
  type      = string
  sensitive = true
}

variable "ghcr_password" {
  type      = string
  sensitive = true
}

variable "sql_admin_login" {
  type      = string
  sensitive = true
}

variable "sql_admin_password" {
  type      = string
  sensitive = true
}

variable "google_maps_api_key" {
  type      = string
  sensitive = true
}

variable "pexels_api_key" {
  type      = string
  sensitive = true
}

variable "keycloak_admin_username" {
  type      = string
  sensitive = true
}

variable "keycloak_admin_password" {
  type      = string
  sensitive = true
}

variable "frontend_keycloak_realm" {
  type     = string
  default  = null
  nullable = true
}

variable "frontend_keycloak_client_id" {
  type = string
}

variable "currency_converter_soap_username" {
  type      = string
  sensitive = true
}

variable "currency_converter_soap_password" {
  type      = string
  sensitive = true
}

variable "rabbitmq_username" {
  type = string
}

variable "rabbitmq_password" {
  type      = string
  sensitive = true
}

variable "rabbitmq_car_exchange" {
  type = string
}

variable "rabbitmq_booking_exchange" {
  type = string
}

variable "redis_password" {
  type      = string
  sensitive = true
}

variable "keycloak_import_override" {
  type = string
}

variable "stack_definition" {
  type = object({
    backend_mode    = string
    enable_rabbitmq = bool
    enable_redis    = bool
    realm_name      = string
    image_tags = object({
      frontend                   = string
      nginx_gateway              = string
      request_proxy_service      = string
      currency_converter_service = string
      keycloak                   = string
      car_rental_service         = optional(string)
      car_service                = optional(string)
      booking_service            = optional(string)
    })
  })
}
