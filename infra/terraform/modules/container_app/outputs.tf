output "id" {
  value = azurerm_container_app.this.id
}

output "name" {
  value = azurerm_container_app.this.name
}

output "latest_fqdn" {
  value = azurerm_container_app.this.latest_revision_fqdn
}

output "latest_name" {
  value = azurerm_container_app.this.latest_revision_name
}
