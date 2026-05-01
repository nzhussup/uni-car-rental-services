output "name" {
  value = azurerm_container_app.this.name
}

output "latest_fqdn" {
  value = azurerm_container_app.this.latest_revision_fqdn
}
