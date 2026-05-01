variable "name" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "container_app_environment_id" {
  type = string
}

variable "revision_mode" {
  type    = string
  default = "Single"
}

variable "registry_server" {
  type = string
}

variable "registry_username" {
  type      = string
  sensitive = true
}

variable "registry_password" {
  type      = string
  sensitive = true
}

variable "image" {
  type = string
}

variable "external" {
  type = bool
}

variable "target_port" {
  type = number
}

variable "transport" {
  type    = string
  default = "auto"
}

variable "min_replicas" {
  type = number
}

variable "max_replicas" {
  type = number
}

variable "cpu" {
  type = number
}

variable "memory" {
  type = string
}

variable "env" {
  type    = map(string)
  default = {}
}

variable "secret_env" {
  type      = map(string)
  default   = {}
  sensitive = true
}

variable "tags" {
  type = map(string)
}
