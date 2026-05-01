# Terraform Infrastructure Documentation

## Overview

This Terraform stack defines production infrastructure for the car rental platform on Azure.

## Technologies

- Terraform `>= 1.6.0`
- `hashicorp/azurerm ~> 4.64`
- Azure Container Apps
- Azure SQL
- Azure Log Analytics
- GHCR image registry

## Provisioned Infrastructure

Core resources:

- Resource Group
- Log Analytics Workspace
- Container Apps Environment
- SQL Server
- SQL Databases: `CarRentalDB`, `KeycloakDB`
- Container Apps:
- `car-rental-frontend`
- `nginx-gateway`
- `booking-service`
- `car-rental-service`
- `request-proxy-service`
- `currency-converter-service`
- `rabbitmq`
- `redis`
- `keycloak-service`

## Network and Exposure Model

Public apps:

- frontend
- nginx gateway
- keycloak

Internal apps:

- booking-service
- car-rental-service
- request-proxy-service
- currency-converter-service
- rabbitmq
- redis

## Runtime Wiring

```text
Browser -> frontend
Browser/API clients -> nginx-gateway
Browser auth flow -> keycloak
nginx-gateway -> booking-service
nginx-gateway -> car-rental-service
nginx-gateway -> request-proxy-service
booking-service <-> rabbitmq
car-rental-service -> currency-converter-service
car-rental-service <-> rabbitmq
currency-converter-service -> redis
car-rental-service -> CarRentalDB
booking-service -> CarRentalDB
keycloak-service -> KeycloakDB
```

## Configuration and Secret Injection

Non-secret runtime values include service URLs and frontend runtime config (`VITE_*`).

Secret values include:

- SQL credentials and connection strings
- proxy API keys
- Keycloak admin/DB secrets
- SOAP credentials
- RabbitMQ credentials
- GHCR credentials

## Quality Notes

- Modular Terraform layout (`modules/*`)
- Typed and sensitive variables
- Immutable image tag deployment model
- Scripted redeploy workflow with forced new revisions
