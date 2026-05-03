output "resource_group_name" {
  value = {
    for stack_name, stack in module.stacks : stack_name => stack.resource_group_name
  }
}

output "container_apps_environment_name" {
  value = {
    for stack_name, stack in module.stacks : stack_name => stack.container_apps_environment_name
  }
}

output "sql_server_fqdn" {
  value = {
    for stack_name, stack in module.stacks : stack_name => stack.sql_server_fqdn
  }
}

output "app_fqdns" {
  value = {
    for stack_name, stack in module.stacks : stack_name => stack.app_fqdns
  }
}
