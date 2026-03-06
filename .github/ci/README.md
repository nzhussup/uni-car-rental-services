# CI Service Configuration

The CI workflow reads service definitions from `.github/ci/services.json`.

## Add a service

Append an item to `services`:

```json
{
  "name": "my-service",
  "path": "path/to/my-service"
}
```

- `name`: logical service name used in job names and artifact names.
- `path`: path to the service folder from repository root.

## Detection rules

The workflow auto-detects service type by path contents:

- Go service: folder contains `go.mod`
- .NET service: folder contains `*.sln` or `*.csproj`

If both or none are detected for one service, the workflow fails with a clear message.

## Quality checks

- Go: `golangci-lint`, installed with `go install` in CI so it is built with the service Go version.
- .NET: `dotnet format --verify-no-changes`.

## Branch behavior

- CI orchestration is in `.github/workflows/ci.yml`.
- Stage implementation templates are in `.github/templates/`:
  - `.github/templates/prepare/action.yml`
  - `.github/templates/quality/action.yml`
  - `.github/templates/test/action.yml`
  - `.github/templates/build/action.yml`
  - `.github/templates/deploy/action.yml`
- Dependency graph:
  - `prepare` runs once and generates the service matrix.
  - `quality` and `test` both depend on `prepare` and run in parallel.
  - `build` depends on both `quality` and `test`.
  - `deploy-mock` depends on `build`.
- Trigger behavior:
  - Pull requests and non-main pushes run `prepare`, `quality`, and `test`.
  - Pushes to `main` additionally run `build` and `deploy-mock`.

## SonarQube

SonarQube is disabled in this first version so CI works without secrets.
