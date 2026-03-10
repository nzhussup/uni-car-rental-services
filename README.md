# car-rental-services

## Generate OpenAPI YAML

The script is generic. It uses the current directory as the target project directory.

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
