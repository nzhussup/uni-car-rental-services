# Terraform Commands and Syntax Cheat Sheet

Azurerm docs: https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs
Azure Container App docs: https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/container_app

## Common Terraform workflow

### Initialize

```bash
terraform init
```

Downloads providers, modules, and prepares the working directory.

### Format files

```bash
terraform fmt -recursive
```

Formats all Terraform files in the current directory tree.

### Validate configuration

```bash
terraform validate
```

Checks whether the configuration is syntactically valid and internally consistent.

### Show what will change

```bash
terraform plan
```

Creates an execution plan without applying changes.

### Save the plan to a file

```bash
terraform plan -out=tfplan
```

Writes the binary plan to `tfplan` so it can be reviewed or applied later.

### Show saved plan in human-readable form

```bash
terraform show tfplan
```

Prints the saved plan in readable text.

### Save readable plan output to a file

```bash
terraform show tfplan > tfplan.txt
```

Useful when you want to inspect all changes in a text file.

### Save plan as JSON

```bash
terraform show -json tfplan > tfplan.json
```

Useful for tooling, scripts, or deeper inspection.

### Recommended plan command for review

```bash
terraform plan -out=tfplan
terraform show tfplan > tfplan.txt
```

This gives you:

- `tfplan` as the executable saved plan
- `tfplan.txt` as readable output with all changes

### Apply directly

```bash
terraform apply
```

Applies the planned infrastructure changes.

### Apply saved plan

```bash
terraform apply tfplan
```

Applies exactly the saved plan.

### Destroy infrastructure

```bash
terraform destroy
```

Destroys all resources managed by the current state.

### Refresh state during planning

```bash
terraform plan -refresh=true
```

Refreshes remote object state before planning.

### Validate with no color output

```bash
terraform validate -no-color
```

Useful in CI logs.

### Plan with variable file

```bash
terraform plan -var-file="prod.tfvars" -out=tfplan
```

### Apply with variable file

```bash
terraform apply -var-file="prod.tfvars"
```

### Destroy with variable file

```bash
terraform destroy -var-file="prod.tfvars"
```

### List state resources

```bash
terraform state list
```

### Show one state resource

```bash
terraform state show <resource_address>
```

Example:

```bash
terraform state show azurerm_resource_group.main
```

### Remove resource from state only

```bash
terraform state rm <resource_address>
```

### Import existing resource

```bash
terraform import <resource_address> <real_resource_id>
```

### Output values

```bash
terraform output
```

### Output one value

```bash
terraform output <name>
```

### Output as JSON

```bash
terraform output -json
```

### Select workspace

```bash
terraform workspace select <name>
```

### Create workspace

```bash
terraform workspace new <name>
```

### List workspaces

```bash
terraform workspace list
```

---

## Good local workflow

```bash
terraform fmt -recursive
terraform init
terraform validate
terraform plan -out=tfplan
terraform show tfplan > tfplan.txt
```

Then inspect `tfplan.txt`.

If it looks correct:

```bash
terraform apply tfplan
```

---

## Good CI workflow

```bash
terraform fmt -check -recursive
terraform init
terraform validate -no-color
terraform plan -no-color -out=tfplan
terraform show -no-color tfplan > tfplan.txt
```

---

## Terraform syntax keywords and important language elements

Below is a practical Terraform/HCL syntax reference. This is not every provider-specific argument in existence, but it covers the core Terraform language keywords and constructs you will use most.

### Top-level block types

#### `terraform`

Terraform settings, required providers, backend, and version constraints.

```hcl
terraform {
  required_version = ">= 1.6.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }

  backend "azurerm" {}
}
```

#### `provider`

Configures a provider.

```hcl
provider "azurerm" {
  features {}
}
```

#### `resource`

Declares a managed resource.

```hcl
resource "azurerm_resource_group" "main" {
  name     = "example-rg"
  location = "West Europe"
}
```

#### `data`

Reads existing external or provider-managed data.

```hcl
data "azurerm_client_config" "current" {}
```

#### `module`

Calls a child module.

```hcl
module "network" {
  source = "./modules/network"
  name   = "main"
}
```

#### `variable`

Declares an input variable.

```hcl
variable "location" {
  type        = string
  description = "Azure region"
}
```

#### `output`

Declares an output value.

```hcl
output "resource_group_name" {
  value = azurerm_resource_group.main.name
}
```

#### `locals`

Defines local values.

```hcl
locals {
  name_prefix = "myapp-prod"
}
```

#### `check`

Defines a custom validation check.

```hcl
check "example" {
  assert {
    condition     = length(local.name_prefix) > 0
    error_message = "name_prefix must not be empty"
  }
}
```

#### `moved`

Tracks resource address moves to avoid recreation.

```hcl
moved {
  from = azurerm_resource_group.old
  to   = azurerm_resource_group.main
}
```

#### `removed`

Declares that Terraform should forget something intentionally.

```hcl
removed {
  from = azurerm_resource_group.old
}
```

#### `import`

Declarative import block.

```hcl
import {
  to = azurerm_resource_group.main
  id = "/subscriptions/.../resourceGroups/example-rg"
}
```

#### `ephemeral`

Used for ephemeral resources in newer Terraform versions where supported.

---

## Common nested blocks

### `backend`

Inside `terraform` block.

### `required_providers`

Inside `terraform` block.

### `assert`

Inside `check` block.

### `lifecycle`

Resource lifecycle behavior.

```hcl
resource "azurerm_resource_group" "main" {
  name     = "example-rg"
  location = "West Europe"

  lifecycle {
    prevent_destroy = true
  }
}
```

### `provisioner`

Runs scripts on create or destroy. Usually avoided when possible.

### `connection`

Used with provisioners.

### `timeouts`

Provider/resource-specific timeout block.

### `dynamic`

Build nested blocks dynamically.

```hcl
dynamic "ip_restriction" {
  for_each = var.ip_rules
  content {
    ip_address = ip_restriction.value
  }
}
```

---

## Meta-arguments

These are special Terraform arguments that can be used on resources and sometimes modules.

### `count`

Create multiple instances by count.

```hcl
resource "azurerm_resource_group" "main" {
  count    = 2
  name     = "rg-${count.index}"
  location = "West Europe"
}
```

### `for_each`

Create multiple instances by map or set.

```hcl
resource "azurerm_resource_group" "main" {
  for_each = toset(["a", "b"])
  name     = "rg-${each.value}"
  location = "West Europe"
}
```

### `depends_on`

Explicit dependency.

```hcl
depends_on = [module.network]
```

### `provider`

Select a specific provider configuration.

### `providers`

Pass provider configurations into modules.

### `lifecycle`

Lifecycle behavior.

---

## Common lifecycle arguments

### `create_before_destroy`

### `prevent_destroy`

### `ignore_changes`

### `replace_triggered_by`

Example:

```hcl
lifecycle {
  create_before_destroy = true
  ignore_changes        = [tags]
}
```

---

## Expressions and operators

### References

```hcl
var.location
local.name_prefix
module.network.vnet_id
azurerm_resource_group.main.name
data.azurerm_client_config.current.tenant_id
```

### Strings

```hcl
name = "${var.project_name}-prod"
```

### Conditionals

```hcl
name = var.is_prod ? "prod-name" : "dev-name"
```

### Lists

```hcl
["a", "b", "c"]
```

### Maps / objects

```hcl
{
  env = "prod"
  app = "api"
}
```

### Operators

- `==`
- `!=`
- `>`
- `>=`
- `<`
- `<=`
- `&&`
- `||`
- `!`
- `? :`

---

## Special symbols and iteration objects

### `count.index`

Used with `count`.

### `each.key`

Used with `for_each` on maps.

### `each.value`

Used with `for_each` on maps or sets.

### `self`

Used in provisioners and some nested contexts.

---

## Type keywords

### Primitive types

- `string`
- `number`
- `bool`

### Collection and structural types

- `list(...)`
- `set(...)`
- `map(...)`
- `object({...})`
- `tuple([...])`

### Flexible type

- `any`

Example:

```hcl
variable "apps" {
  type = map(object({
    name   = string
    cpu    = number
    memory = string
  }))
}
```

---

## Common variable validation syntax

```hcl
variable "environment" {
  type = string

  validation {
    condition     = contains(["dev", "test", "prod"], var.environment)
    error_message = "environment must be dev, test, or prod"
  }
}
```

Keywords here:

- `validation`
- `condition`
- `error_message`

---

## Common attributes in variable/output blocks

### Variable block attributes

- `type`
- `default`
- `description`
- `nullable`
- `sensitive`
- `validation`

### Output block attributes

- `value`
- `description`
- `sensitive`
- `depends_on`

---

## Common built-in functions you will use often

### String functions

- `lower()`
- `upper()`
- `replace()`
- `substr()`
- `split()`
- `join()`
- `trim()`
- `trimprefix()`
- `trimsuffix()`

### Collection functions

- `length()`
- `contains()`
- `keys()`
- `values()`
- `lookup()`
- `merge()`
- `concat()`
- `flatten()`
- `distinct()`
- `toset()`
- `tolist()`
- `tomap()`

### Numeric and utility functions

- `min()`
- `max()`
- `floor()`
- `ceil()`
- `abs()`
- `coalesce()`
- `try()`
- `can()`

### Encoding and files

- `jsonencode()`
- `jsondecode()`
- `yamlencode()`
- `yamldecode()`
- `file()`
- `templatefile()`

### CIDR/network helpers

- `cidrsubnet()`
- `cidrhost()`
- `cidrnetmask()`
