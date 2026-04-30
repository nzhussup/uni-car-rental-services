# car-rental-services

## Generate OpenAPI YAML

The script is generic. It uses the current directory as the target project directory.

It supports:

- .NET ASP.NET Core web projects (Swagger CLI)
- Go services with Swagger annotations (via `swag` + automatic Swagger2 -> OpenAPI3 conversion)

1. `cd` into your .NET project folder (or solution folder containing one web project).
2. Run:

```bash
../scripts/generate-openapi.sh
```

Default output is `openapi.yaml` in that same folder.

Optional: pass explicit project dir and output path:

```bash
./scripts/generate-openapi.sh <project-dir> <output-path>
```

Examples:

```bash
# from repository root
./scripts/generate-openapi.sh ./CarRentalService ./CarRentalService/openapi.yaml

# from CarRentalService folder
../scripts/generate-openapi.sh

# from repository root for Go proxy service
./scripts/generate-openapi.sh ./RequestProxyService ./RequestProxyService/openapi.yaml
```

## Generate OpenAPI For All Services + Composite Root Spec

Run from repo root:

```bash
./scripts/generate-all-openapi.sh
```

This will:

1. generate per-service `openapi.yaml` for configured services
2. generate a composite root spec at `./openapi.yaml`

Service selection is controlled by:

- `scripts/openapi-services.json`

Example config:

```json
{
  "services": [
    { "name": "BookingService", "enabled": true },
    { "name": "CarService", "enabled": true },
    { "name": "RequestProxyService", "enabled": true },
    { "name": "CurrencyConverterService", "enabled": false }
  ]
}
```

You can override config path with:

```bash
OPENAPI_SERVICES_CONFIG=/path/to/config.json ./scripts/generate-all-openapi.sh
```

## .NET Requirements For OpenAPI Generation

To make a .NET API support OpenAPI YAML generation with Swagger CLI:

1. The project must be an ASP.NET Core web project (`Microsoft.NET.Sdk.Web` in `.csproj`).
2. Register endpoint metadata:

```csharp
builder.Services.AddEndpointsApiExplorer();
```

3. Register Swagger generation and define a document name (for example `v1`):

```csharp
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });
});
```

4. Install Swagger CLI tool:

```bash
dotnet new tool-manifest
dotnet tool install Swashbuckle.AspNetCore.Cli --version 10.1.4
```

5. Ensure startup does not require unavailable infrastructure when generating docs.
   If your app runs DB migrations on startup, guard them with an environment flag (like `SKIP_DB_MIGRATION`) so CLI generation can run without a live DB.
