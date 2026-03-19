output "resource_group_name" {
  value = module.resource_group.name
}

output "container_apps_environment_name" {
  value = module.container_apps_env.name
}

output "sql_server_fqdn" {
  value = module.sql.server_fqdn
}

output "app_fqdns" {
  value = {
    for app_name, app in module.apps : app_name => app.latest_fqdn
  }
}
