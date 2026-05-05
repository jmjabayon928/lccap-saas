# Local development

## Prerequisites

- .NET 8 SDK
- PostgreSQL (local instance or container)
- PowerShell (Windows) or compatible shell
- Node.js / npm — for the Next.js frontend in `frontend/` (optional if you only run the API)
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

## Development-only Demo Seed

The API includes a development-only demo seed service to populate the database with demo accounts and users for local MVP testing of authentication, tenant isolation, and LGU workspaces.

### Enabling the Seed

The seed is disabled by default. To enable it:

1. Update `src/Lccap.Api/appsettings.Development.json` or set environment variables:
   ```json
   "DemoSeed": {
     "Enabled": true,
     "Password": "YOUR_LOCAL_DEMO_PASSWORD"
   }
   ```
2. **CRITICAL**: Do not commit a real password to `appsettings.Development.json`. Use environment variables (e.g., `DemoSeed__Password`) or local user secrets.
3. If `Enabled` is true but `Password` is blank, the API will fail to start with an `InvalidOperationException`.

### Demo Accounts and Users

The seed creates the following (idempotent):

- **Platform Admin Demo**: `platform.admin@lccap.local` (Role: SystemAdmin, Scope: Platform)
- **Naga City Demo LGU**: `naga.demo@lccap.local`
  - `naga.planner@lccap.local` (Role: Planner)
  - `naga.viewer@lccap.local` (Role: Viewer)
- **Pasig City Demo LGU**: `pasig.demo@lccap.local`
  - `pasig.planner@lccap.local` (Role: Planner)
  - `pasig.viewer@lccap.local` (Role: Viewer)
- **Quezon City Demo LGU**: `quezon.demo@lccap.local`
  - `quezon.planner@lccap.local` (Role: Planner)
  - `quezon.viewer@lccap.local` (Role: Viewer)

All demo users share the password configured in `DemoSeed:Password`.

**Note**: These are development-only demo users and do not represent official government accounts or approval authorities.

## Repository / Git note

At the time this document was written, a `.git` directory was not present in the workspace. If you use Git, initialize or clone as appropriate for your team workflow; keep secrets out of version control.

## Recommended first validation flow

This path exercises the **MVP LGU workspace loop**: organize plan sections and documents, maintain actions and monitoring, and produce **export-ready draft** output via the API/UI—not an official government or donor submission channel.

After the API is running against a configured database:

1. **Auth** — register/login and obtain a JWT.
2. **Plans** — create or list plans for the authenticated tenant.
3. **Sections** — read or update plan sections.
4. **Documents** — upload and associate documents (exercises file storage).
5. **Actions** — create or list action items.
6. **Monitoring** — add indicators and updates.
7. **Export** — create an export job and use the download flow when ready.

This path touches tenant scoping, storage, and export behavior in one pass.

## Run the frontend

From the repository root:

```powershell
cd frontend
npm install
copy .env.example .env.local
# Edit .env.local if your API listens on a host/port other than http://localhost:5243
npm run dev
```

The UI expects `NEXT_PUBLIC_API_BASE_URL` (see `frontend/.env.example`) to match your running API. Use `npm run type-check`, `npm run lint`, and `npm run build` before submitting frontend changes.

### Production-like frontend run (optional)

To validate the optimized bundle (same as many hosted deployments):

```powershell
cd frontend
npm run build
npm start
```

`npm run build` must succeed before `npm start` — the production server serves the prebuilt output. Use `npm run dev` for local development with fast refresh; it does not replace a production build check.

### Manual UI check (plans and sections)

With the API running and `NEXT_PUBLIC_API_BASE_URL` pointing at it:

1. `cd frontend` → `npm run build` → `npm start` (or use `npm run dev` while iterating).
2. Open the app, **sign in**, go to **Plans**, **create a plan**.
3. Open the **plan workspace** (`/plans/{planId}`), **select a section**, edit **title** and **content**, **save**.
4. **Refresh** the workspace page and confirm the section shows the saved title and content after reload.
5. **Revision history check**:
   - Edit the same section again and **Save**.
   - Use **Revision history** (near the Save button) to open the history panel.
   - Confirm at least two entries appear (newest first).
   - Use **Restore this version** on the older entry, confirm the prompt, and provide an optional reason.
   - Confirm the editor content reverts to the older version and a success message appears.
   - **Refresh** the page and confirm the restored content persists.
   - If you have database access, confirm `audit_logs` contains `PlanSectionUpdated` and `PlanSectionRestored` entries.

### Manual UI check (existing plans list)

With the API running and `NEXT_PUBLIC_API_BASE_URL` set:

1. **Login**.
2. Go to **Plans** (`/plans`).
3. Confirm **existing tenant plans** appear in **Your workspaces** (or the empty state if none).
4. **Open an existing plan** via **Open workspace** and confirm the plan workspace loads.
5. Return to **Plans**, **create a new plan**, and confirm you are **redirected** to the new workspace after creation.
6. Open **Plans** again and confirm the **new plan** appears in the list.

### Manual UI check (documents)

With the API running and `NEXT_PUBLIC_API_BASE_URL` set:

1. Run the backend API, then `cd frontend` → `npm run build` → `npm start` (use another port if 3000 is busy, e.g. `$env:PORT='3010'; npm start` on Windows PowerShell).
2. **Login**, **create or open a plan**, open the **plan workspace**.
3. Under **Documents**, upload a valid **PDF / DOCX / XLSX / PNG / JPG** within the MVP size limit.
4. Confirm the file appears in the **Attached documents** list without refreshing (or refresh to verify persistence).
5. Try a **disallowed extension** (e.g. rename or pick `.exe` if the picker allows) — the UI should block before upload.
6. Try a file **larger than 25 MB** — the frontend validation should block before upload.
7. **Edit metadata** — use **Edit** on a row; change title, category, description, source agency, document date, and tags; **Save**, then **refresh** and confirm values persist.
8. **Archive** — use **Archive**, confirm in the prompt; the row should leave the active list immediately; **refresh** and confirm it does not reappear in **Attached documents** (the underlying file record is retained server-side for accountability).
9. Confirm **upload** still works for a new file after an archive, and other workspace flows you rely on still behave normally.

### Manual UI check (plan metadata and archive)

With the API running and `NEXT_PUBLIC_API_BASE_URL` set:

1. **Login**, go to **Plans**, and confirm the list of active plans.
2. **Edit plan metadata** — use **Edit** on a plan row; confirm the UI shows "Loading latest details…" before the form opens; change title, description, planning period, status, template mode, and version; **Save**, then **refresh** and confirm values persist.
3. **Workspace edit check** — open a plan workspace; use **Edit details** in the header; confirm the form opens immediately (as detail was already loaded); if the plan is legacy (missing rowVersion), confirm the button says **Refresh to edit** and triggers a reload.
4. **RowVersion repair check** — if a plan has an empty `rowVersion` in the database, confirm that opening the workspace or fetching detail via API repairs it automatically (the API response will contain a non-empty `rowVersion` and it will be saved to the database).
5. **Archive plan** — use **Archive** on a plan, type **ARCHIVE** to confirm; the plan should leave the active list immediately.
6. **Direct access check** — try navigating directly to `/plans/{archivedPlanId}` and confirm the UI shows a "Plan unavailable" or "Not found" error.
7. **Audit check** — if you have database access, confirm `audit_logs` contains `PlanMetadataUpdated` and `PlanArchived` entries with correct old/new value snapshots.

### Manual UI check (action items)

With the API running and `NEXT_PUBLIC_API_BASE_URL` set:

1. Run the backend API, then `cd frontend` → `npm run build` → `npm start` (use another port if 3000 is busy, e.g. `$env:PORT='3010'; npm start` on Windows PowerShell).
2. **Login**.
3. **Create or open** a plan from **Plans**.
4. On the **plan workspace** (`/plans/{planId}`), under **Actions**, add an **Adaptation** action (title, sector, budget, etc.) and save.
5. Add a **Mitigation** action and confirm both appear in the **Action items** list without a full page reload.
6. **RowVersion repair check** — if an action item has an empty `rowVersion`, confirm that fetching the list for the plan repairs it (the API response will contain non-empty tokens).
7. In the **Action items** list, use **Edit** on a row; change title, status, budget, timeline, KPI, and other fields; **Save**, then confirm the row updates in place (and the side form stays in sync if that action was selected).
8. **Refresh** the workspace page and confirm edited values reload from the API.
9. Use **Archive** on an action item; confirm the prompt; the row should leave the active list immediately; **refresh** and confirm it does not reappear in the list (the row is retained server-side as archived).
10. Under **Export draft PDF package**, run **Generate PDF draft** (and download when complete) to confirm export still works after edits and an archive.

### Manual UI check (monitoring indicators)

With the API running and `NEXT_PUBLIC_API_BASE_URL` set:

1. Run the backend API, then `cd frontend` → `npm run build` → `npm start` (use another port if 3000 is busy, e.g. `$env:PORT='3010'; npm start` on Windows PowerShell).
2. **Login**.
3. **Create or open** a plan from **Plans** and open the **plan workspace**.
4. Under **Monitoring indicators**, create an indicator (name, optional numeric fields, progress, frequency, responsible office, status). Confirm it appears in the list without a full page reload.
5. **RowVersion repair check** — if an indicator has an empty `rowVersion`, confirm that fetching the list for the plan repairs it.
6. In the **Monitoring indicators** list, use **Edit** on a row; change name, status, baseline, current, target, progress, frequency, and responsible office; **Save**, then confirm the row updates in place (and the side form stays in sync if that indicator was selected).
7. **Refresh** the workspace page and confirm edited values reload from the API.
8. Use **Archive** on an indicator; confirm the prompt; the row should leave the active list immediately; **refresh** and confirm it does not reappear in the list (the row is retained server-side as archived).
9. Under **Export draft PDF package**, run **Generate PDF draft** (and download when complete) to confirm export still works after monitoring edits and an archive.

### Manual UI check (export PDF draft package)

With the API running and `NEXT_PUBLIC_API_BASE_URL` set:

1. Run the backend API, then `cd frontend` → `npm run build` → `npm start` (use another port if 3000 is busy, e.g. `$env:PORT='3010'; npm start` on Windows PowerShell).
2. **Login**, **create or open** a plan, and open the **plan workspace**.
3. Optionally edit **sections**, upload **documents**, add **action items**, and add **monitoring indicators** to reflect a realistic draft package.
4. Under **Export draft PDF package**, choose **Generate PDF draft** and wait for the job status to show (use **Refresh status** if the job is queued or running).
5. When the status is **Completed**, use **Download PDF** and confirm the browser saves a file (e.g. `lccap-draft-package.pdf`).
6. Confirm on-screen copy describes a **draft / working output** and does **not** present the export as an official submission or national reporting channel.

## Manual RBAC Validation

With the API running and the Demo Seed enabled (see above):

1. **Login as Planner** (`naga.planner@lccap.local`):
   - **Plans**: Can create, edit, and save plans.
   - **Sections**: Can edit and restore sections.
   - **Documents**: Can upload and edit metadata.
   - **Actions**: Can create and update action items.
   - **Monitoring**: Can create and update indicators.
   - **Export**: Can generate PDF exports.
   - **Archive**: Should be **blocked** (403 Forbidden) when trying to archive a plan, action item, document, or monitoring indicator.

2. **Login as Viewer** (`naga.viewer@lccap.local`):
   - **Read**: Can open and read plans, sections, documents, actions, and monitoring.
   - **Mutate**: Should be **blocked** (403 Forbidden) when trying to create, update, archive, restore, or upload any content.

3. **Login as Reviewer** (if configured):
   - **Read**: Can read all content.
   - **Export**: Can generate PDF exports.
   - **Mutate**: Should be **blocked** (403 Forbidden) for all create, update, archive, and restore operations.

4. **Login as Admin** (a tenant Admin):
   - **Full Control**: Can perform all operations including **Archive**.

## Audit History Validation

With the API running and the Demo Seed enabled:

1. **Login as Admin** (`naga.demo@lccap.local` or similar Admin role):
   - **Audit History**: Confirm "Audit History" appears in the sidebar.
   - **View**: Open Audit History and confirm accountability records load (e.g., PlanMetadataUpdated, PlanSectionUpdated).
   - **Details**: Click **View** on a row and confirm old/new values and metadata are displayed.
   - **Filters**: Test filtering by Entity Name (e.g., "Plan"), Action, and Date range.

2. **Login as Reviewer**:
   - **Audit History**: Confirm "Audit History" appears in the sidebar and loads records.

3. **Login as Planner**:
   - **Sidebar**: "Audit History" should be **hidden** in the sidebar.
   - **Direct Access**: Try navigating to `/audit` and confirm the UI shows "You do not have permission to view audit history."

4. **Login as Viewer**:
   - **Sidebar**: "Audit History" should be **hidden**.
   - **Direct Access**: Confirm access is blocked.

## Local API port

The backend API defaults to **`http://localhost:5243`** in Development (see `Properties/launchSettings.json`).

## CORS

Development CORS is configured in `Program.cs` to allow local frontend origins (`http://localhost:3000`, `3001`, `3010`). Production environments must explicitly configure allowed origins for the deployed frontend.

## Related docs

- [Environment variables](environment-variables.md) — names, binding, and production cautions.
- [Security notes](security-notes.md) — MVP security assumptions and future hardening.
- Slice 1 of auth/session hardening added `public.refresh_tokens` table, `RefreshToken` entity, EF config, and mapping tests (no runtime login/refresh behavior changed yet; cookie and endpoint work in later slices). Raw tokens are never stored.
- Slice 2 added backend RefreshTokenService, AuthSessionService, AuthCookieOptions, and updated AuthController with login (now sets HttpOnly refresh cookie), refresh, logout, and me endpoints. Response shape for login remains compatible; frontend integration in Slice 3.
