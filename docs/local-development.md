# Local development

## Prerequisites

- .NET 8 SDK
- PostgreSQL (local instance or container)
- PowerShell (Windows) or compatible shell
- Node.js / npm — for the planned frontend (not required for API-only work)
- Python — for the planned FastAPI AI service (not required for API-only work)

## Restore and build

```powershell
dotnet restore Lccap.sln
dotnet build Lccap.sln
```

## Run the API

```powershell
dotnet run --project src/Lccap.Api/Lccap.Api.csproj
```

Ensure PostgreSQL is reachable and that connection settings in `src/Lccap.Api/appsettings.Development.json` (or environment variables) point at your database. Replace placeholder passwords before expecting a successful connection.

## Run tests

### Full test projects

```powershell
dotnet test tests/Lccap.Api.Tests/Lccap.Api.Tests.csproj
dotnet test tests/Lccap.Infrastructure.Tests/Lccap.Infrastructure.Tests.csproj
```

### Targeted filters (examples)

```powershell
dotnet test tests/Lccap.Api.Tests/Lccap.Api.Tests.csproj --filter PlansControllerTests
dotnet test tests/Lccap.Api.Tests/Lccap.Api.Tests.csproj --filter PlanSectionsControllerTests
dotnet test tests/Lccap.Api.Tests/Lccap.Api.Tests.csproj --filter DocumentsControllerTests
dotnet test tests/Lccap.Api.Tests/Lccap.Api.Tests.csproj --filter ActionItemsControllerTests
dotnet test tests/Lccap.Api.Tests/Lccap.Api.Tests.csproj --filter MonitoringControllerTests
dotnet test tests/Lccap.Api.Tests/Lccap.Api.Tests.csproj --filter ExportControllerTests
```

## Cursor / VS Code debug

1. Open the repository folder in VS Code or Cursor.
2. Use the **Run and Debug** view and select the **Lccap.Api** configuration.
3. Start debugging (F5). The **preLaunchTask** runs `dotnet build` on `Lccap.sln`, then launches the API with `ASPNETCORE_ENVIRONMENT=Development`.

Launch metadata lives under `.vscode/launch.json` and `.vscode/tasks.json`.

## Local PostgreSQL

- Default development settings assume a PostgreSQL server on `localhost` (see `appsettings.Development.json`).
- Create a database (e.g. `lccap_dev`) and a user with appropriate privileges; update the connection string or use `ConnectionStrings__Postgres` in the environment.
- Placeholder passwords in committed config are intentional; substitute local values yourself and do not commit real credentials.

## Local file storage

- Development file roots are configured under `FileStorage` (e.g. `data/dev-files` relative to the API content root unless overridden).
- Ensure the process can create and write under that path. Do not point `RootPath` at sensitive system directories.

## Repository / Git note

At the time this document was written, a `.git` directory was not present in the workspace. If you use Git, initialize or clone as appropriate for your team workflow; keep secrets out of version control.

## Recommended first validation flow

After the API is running against a configured database:

1. **Auth** — register/login and obtain a JWT.
2. **Plans** — create or list plans for the authenticated tenant.
3. **Sections** — read or update plan sections.
4. **Documents** — upload and associate documents (exercises file storage).
5. **Actions** — create or list action items.
6. **Monitoring** — add indicators and updates.
7. **Export** — create an export job and use the download flow when ready.

This path touches tenant scoping, storage, and export behavior in one pass.

## Related docs

- [Environment variables](environment-variables.md) — names, binding, and production cautions.
- [Security notes](security-notes.md) — MVP security assumptions and future hardening.
