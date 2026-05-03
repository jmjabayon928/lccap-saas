# LCCAP SaaS — Enterprise Local Climate Action Planning Platform

.NET
PostgreSQL
Architecture
AI
Status
License

**LCCAP SaaS** is an **LGU-facing operating workspace** for organizing **Local Climate Change Action Plan (LCCAP)** preparation: plan sections, supporting documents and evidence, climate actions, monitoring indicators, and **export-ready draft packages** that teams can refine offline and route through their own official channels. It **complements existing official government and donor systems**—it does not supersede them.

The same workspace treats climate planning as structured, lifecycle-aware preparation rather than a single static file:

- Plans are governed workspace entities
- Sections are editable and version-ready
- Supporting documents are organized as evidence-linked FileAssets
- Actions are structured climate interventions
- Monitoring indicators are recorded and updated
- Exports produce **working outputs** from structured plan data (draft-ready packages, not a substitute for mandated submission portals)
- AI features are planned as asynchronous, auditable jobs (later phases)

Later phases extend capability (evidence indexing, richer exports, interoperability, spatial analytics—see **Phase roadmap**); the **MVP** remains an internal preparation and organization workspace for LGU teams.

---

## Table of Contents

- Executive Summary
- Product positioning & complementary role
- MVP emphasis & non-goals
- Phase roadmap
- Product Overview
- Target Users
- Current MVP Status
- Core Modules
- Completed MVP Capabilities
- In Progress / Planned Capabilities
- System Architecture
- Technology Stack
- Multi-Tenancy and Security
- Data Model Overview
- AI and Intelligence Roadmap
- Frontend Direction
- Local Setup & Development
- Demo Login Users
- End-to-End Demo Script
- Testing and Quality Gates
- Roadmap
- Enterprise Design Principles
- Author

---

## Executive Summary

Local Climate Change Action Plans require LGUs to gather climate data, assess risks, identify adaptation and mitigation actions, define monitoring indicators, prepare reports, and align with national climate policy frameworks.

In many real-world environments, this process is still managed through disconnected Word documents, spreadsheets, PDFs, shared folders, and manual review processes.

LCCAP SaaS provides a structured SaaS **planning workspace** for:

- Creating LCCAP plans and organizing sections
- Uploading and organizing supporting documents and evidence
- Defining climate actions
- Tracking monitoring indicators
- Generating **export-ready draft** outputs for offline refinement and use with official processes
- Maintaining tenant-scoped data isolation
- Preparing for later-phase AI-assisted drafting, analysis, and recommendations

It **complements** official government and donor systems; it does not replace mandated portals, diagnostics, or approvals.

The MVP is implemented using **.NET 8 Clean Architecture**, **Entity Framework Core**, **PostgreSQL**, and a **Next.js / TypeScript / Tailwind CSS** frontend. The core MVP flow has been locally validated end-to-end: login, tenant-scoped plan workspace, section editing, document upload/listing, action items, monitoring indicators, PDF export generation, and PDF download.

---

## Product positioning & complementary role

LCCAP SaaS is positioned as an **enterprise-grade, LGU-facing LCCAP operating workspace**: a planning workspace that helps teams **prepare better internal working packages**—organized sections, evidence, actions, monitoring, and **export-ready draft outputs**—while **existing official systems remain the authority** for national reporting, donor-specific portals, mandated diagnostics, and approvals.

**Complementary role (non-replacement).** This product is **not** a substitute for, and does not replicate the mandate of, systems and channels such as CCC, DILG, LGA, NICCDIES, PSF-related portals, PlanSmart, UNDP SHIELD, official LGU approval or clearance workflows, general LGU enterprise document management suites, or other official government or donor climate-governance tools. Where those systems define submission, eligibility, or certification, LCCAP SaaS supports **upstream preparation and organization** only.

---

## MVP emphasis & non-goals

### MVP emphasis

- Organizing **LCCAP plan sections** and narrative structure
- Organizing **supporting documents and evidence** in plan context
- **Linking** documentation and references relevant to risk and hazard discussion (as structured plan content—not a replacement for official risk diagnostic tools)
- **Defining climate actions** (adaptation and mitigation) with structured fields
- **Tracking monitoring indicators** and progress for implementation visibility
- **Preparing export-ready working outputs** (e.g. PDF drafts) for offline refinement and use with official processes
- **Complementing** government and donor workflows rather than replacing mandated portals or guidance

### MVP non-goals

- Not an official national or agency **submission** channel for any government or donor program
- Not a replacement for **NICCDIES** or other mandated national reporting systems
- Not a replacement for **DILG**, **LGA**, or CCC **guidance, training, or policy interpretation**
- Not a replacement for **PlanSmart**, **UNDP SHIELD**, or other **official risk diagnostic** or assessment platforms
- Not a PSF or donor **application portal**; not funding-system-specific eligibility processing
- Not a **government approval**, certification, or clearance system
- Not a general-purpose **LGU document management** or records-management platform
- Not a **national climate dashboard** or cross-country situational picture
- Not a **full GIS / spatial analytics** platform in MVP (advanced spatial work is explicitly deferred to later phases)

---

## Phase roadmap

Boundaries keep **MVP** narrowly focused on the LGU workspace; **Phase 2** adds preparation depth and richer outputs; **Phase 3** adds interoperability and advanced analytics. Features must not **bleed across phases** without an explicit decision.


| Phase               | Focus                                                                                                                                                                                           |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **MVP**             | LGU-facing operating workspace: plan sections, documents/evidence, action items, monitoring indicators, export-ready draft package                                                              |
| **Phase 2** (later) | Evidence index; review comments; CCET / funding-readiness tagging; PSF proposal **package helper** (preparation aid, not a portal); richer exports; deeper monitoring and operational readiness |
| **Phase 3** (later) | Interoperability; PostGIS / spatial analytics; exposure summaries; scenario comparison; recommendation-style assistance; integration-ready APIs and exports                                     |


---

## Product Overview

LCCAP SaaS is designed as a **plan-centric, LGU-facing climate planning workspace**.

At the center of the system is the **Plan**, which acts as the aggregate root for the LCCAP workspace.

```text
Account / LGU
  └── Plan
        ├── Plan Sections
        ├── Documents
        ├── File Assets
        ├── Action Items
        ├── Monitoring Indicators
        ├── Export Jobs
        └── Future AI / Maps / Analytics
```

This architecture enables the system to organize climate planning work around a clear, auditable structure rather than treating LCCAP as a single static file.

---

## Target Users

### LGU-Level Users

- LGU Admin
- Climate Planner
- Technical Staff
- Department Reviewer
- Monitoring Officer
- Viewer / Read-only Stakeholder

### Platform-Level Users

- Platform Administrator
- Technical Support Operator
- Product / operations roles (tenant onboarding, configuration—where deployed)

Enterprise deployments may define additional **internal** roles; MVP does **not** imply a national oversight or submission-review role for this product.

### External / Future Users

- Citizens
- NGOs
- Development partners
- Funding agencies
- Public portal viewers

---

## Current MVP Status

The MVP is now **locally end-to-end validated** for the core LGU workspace flow.

Validated flow:

```text
Login
  → View existing tenant plans
  → Open real plan workspace
  → Edit and save LCCAP section content
  → Upload and list supporting documents
  → Create action item
  → Create monitoring indicator
  → Generate PDF draft package
  → Download generated PDF
```

### Completed Backend Slices


| Module                             | Status   |
| ---------------------------------- | -------- |
| Solution structure                 | Complete |
| PostgreSQL baseline schema         | Complete |
| Clean Architecture project setup   | Complete |
| Domain base entities               | Complete |
| EF Core DbContext and mappings     | Complete |
| Auth / JWT bearer authentication   | Complete |
| Current user / tenant context      | Complete |
| Development CORS configuration     | Complete |
| Development-only demo seed service | Complete |
| Plans API                          | Complete |
| Tenant-scoped plans list API       | Complete |
| Default plan section seeding       | Complete |
| Plan Sections API                  | Complete |
| Documents API                      | Complete |
| Local File Storage Service         | Complete |
| Monitoring API                     | Complete |
| Action Items API                   | Complete |
| Export Job / PDF generation        | Complete |
| Export download endpoint           | Complete |
| Mapping tests for key entities     | Complete |
| API controller tests               | Complete |


### Completed Frontend Slices


| Module                             | Status   |
| ---------------------------------- | -------- |
| Next.js frontend foundation        | Complete |
| Responsive enterprise UI shell     | Complete |
| Tailwind CSS setup                 | Complete |
| Local shadcn-style UI primitives   | Complete |
| Login UI and auth session handling | Complete |
| API client and typed HTTP layer    | Complete |
| Dashboard preview                  | Complete |
| Existing plans list                | Complete |
| Create plan UI                     | Complete |
| Plan workspace UI                  | Complete |
| Plan sections editor               | Complete |
| Documents upload/list UI           | Complete |
| Action items UI                    | Complete |
| Monitoring indicators UI           | Complete |
| PDF export/download UI             | Complete |
| Production-like `npm start` flow   | Complete |


### Remaining MVP Polish Items


| Module                              | Status      |
| ----------------------------------- | ----------- |
| Swagger/OpenAPI polish              | Planned     |
| README/demo documentation refresh   | In progress |
| UI polish and screenshot capture    | Planned     |
| Optional cleanup of local test data | Planned     |
| Broader E2E regression script       | Planned     |


---

## Core Modules

### 1. Authentication and Tenant Context

Authentication is implemented using JWT bearer authentication.

Key capabilities:

- Login endpoint
- Secure password hashing
- JWT token generation
- Standard Microsoft JWT bearer middleware
- `account_id` claim for tenant isolation
- Current user context used by commands and queries
- No public `accountId` accepted from request bodies, routes, or query strings
- Development-only seeded demo users for local testing

Tenant isolation is enforced through the authenticated current user context.

---

### 2. Plans

Plans are the root LCCAP workspace entity.

Capabilities:

- Create plan
- Update plan
- List plans for the authenticated tenant
- Get plan by ID
- Tenant-scoped access
- Default plan sections seeded after plan creation
- Validation for title, years, status, and template mode

Current API surface:

```text
GET  /api/plans
POST /api/plans
PUT  /api/plans/{planId}
GET  /api/plans/{planId}
```

---

### 3. Plan Sections

Plan sections represent editable sections of the LCCAP document.

Default sections are created automatically when a plan is created:


| Order | Section Key             | Title                                |
| ----- | ----------------------- | ------------------------------------ |
| 10    | executive_summary       | Executive Summary                    |
| 20    | introduction            | Introduction and LGU Profile         |
| 30    | climate_risk_assessment | Climate and Disaster Risk Assessment |
| 40    | adaptation_actions      | Adaptation Actions                   |
| 50    | mitigation_actions      | Mitigation Actions                   |
| 60    | implementation_plan     | Implementation Plan                  |
| 70    | monitoring_evaluation   | Monitoring and Evaluation            |
| 80    | references_annexes      | References and Annexes               |


Current API surface:

```text
GET /api/plans/{planId}/sections
GET /api/plans/{planId}/sections/{sectionKey}
PUT /api/plans/{planId}/sections/{sectionKey}
```

---

### 4. Documents and File Assets

The system separates logical document usage from physical file storage.

- `Document` = logical document record attached to a plan
- `FileAsset` = physical file metadata and storage reference

Capabilities:

- Upload document
- List documents by plan
- Tenant-scoped document visibility
- Local file storage
- SHA256 hashing
- Safe generated stored filenames
- Extension validation
- Path traversal protection
- Frontend-safe rendering without exposing stored server paths

Current API surface:

```text
POST /api/documents/upload
GET  /api/plans/{planId}/documents
```

---

### 5. Local File Storage

The MVP includes a local file storage abstraction.

Capabilities:

- Save uploaded file stream
- Generate tenant-scoped storage path
- Open file stream for download
- Delete file
- Reject path traversal
- Reject empty streams
- Enforce max upload size
- Enforce allowed file extensions
- Compute SHA256 hash

Storage path format:

```text
uploads/{accountId}/{yyyy}/{MM}/{generatedGuid}{extension}
```

This is implemented behind `IFileStorageService` so future cloud storage providers can be introduced without changing application logic.

---

### 6. Action Items

Action Items represent structured climate interventions.

Capabilities:

- Create action item
- Update action item
- List action items by plan
- Get action item by ID
- Validate action type, sector, status, budget, and timeline
- Tenant-scoped reads and writes

Supported action types:

```text
Adaptation
Mitigation
```

Supported statuses:

```text
Planned
InProgress
OnTrack
Delayed
Completed
Cancelled
```

Current API surface:

```text
POST /api/plans/{planId}/actions
PUT  /api/actions/{actionItemId}
GET  /api/plans/{planId}/actions
GET  /api/actions/{actionItemId}
```

---

### 7. Monitoring

Monitoring tracks climate action progress through indicators.

Capabilities:

- Create monitoring indicator
- Update monitoring indicator
- List indicators by plan
- Tenant-scoped access
- Validation for indicator name, status, and progress values

Supported statuses:

```text
NotStarted
InProgress
OnTrack
Delayed
Completed
```

Current API surface:

```text
POST /api/monitoring/indicators
PUT  /api/monitoring/indicators/{indicatorId}
GET  /api/monitoring/plans/{planId}/indicators
```

---

### 8. Export Jobs

Export jobs generate LCCAP outputs from structured plan data.

Capabilities:

- Create PDF export job
- Generate minimal valid PDF from plan and section data
- Store generated PDF as FileAsset
- Link export job to FileAsset
- Download completed export safely
- Prevent cross-tenant downloads
- Return conflict for incomplete exports

Current API surface:

```text
POST /api/plans/{planId}/exports/pdf
GET  /api/exports/{exportJobId}
GET  /api/exports/{exportJobId}/download
```

---

## Completed MVP Capabilities

The current MVP implements and validates the major LGU workspace workflow:

```text
Authenticate
  → View Existing Plans
  → Create Plan
  → Auto-create Default Sections
  → Edit Sections
  → Upload Documents
  → Add Action Items
  → Add Monitoring Indicators
  → Generate PDF Export
  → Download Export
```

This is the core LCCAP planning loop.

---

## In Progress / Planned Capabilities

### MVP Remaining

- Swagger/OpenAPI polish
- Demo script and screenshot capture
- UI copy polish where needed
- Optional local test-data cleanup
- E2E regression checklist or automated smoke script

### Phase 2 (later)

- Evidence index
- Review comments
- CCET / funding-readiness tagging
- PSF proposal package helper (draft preparation support—not a funding portal)
- Richer exports
- Monitoring and operational readiness depth

### Phase 3 (later)

- Interoperability with external systems
- PostGIS / spatial analytics
- Exposure summaries
- Scenario comparison
- Recommendation engine
- Integration-ready APIs and exports
- Advanced observability, security hardening, operational runbooks as needed

---

## System Architecture

LCCAP SaaS uses a modular Clean Architecture approach.

```text
┌────────────────────────────────────┐
│              Frontend              │
│   Next.js / TypeScript / Tailwind  │
└─────────────────┬──────────────────┘
                  │ HTTP / JSON
                  ▼
┌────────────────────────────────────┐
│             Lccap.Api              │
│  Controllers / Auth / Middleware   │
└─────────────────┬──────────────────┘
                  │
                  ▼
┌────────────────────────────────────┐
│         Lccap.Application           │
│ Commands / Queries / Interfaces    │
└─────────────────┬──────────────────┘
                  │
                  ▼
┌────────────────────────────────────┐
│        Lccap.Infrastructure         │
│ EF Core / PostgreSQL / Storage     │
└─────────────────┬──────────────────┘
                  │
                  ▼
┌────────────────────────────────────┐
│            PostgreSQL              │
│ Plans / Sections / Files / Jobs    │
└────────────────────────────────────┘
```

Planned AI layer:

```text
┌────────────────────────────────────┐
│          Python FastAPI AI          │
│  Summarization / RAG / Analysis    │
└────────────────────────────────────┘
```

---

## Technology Stack

### Backend

- .NET 8
- ASP.NET Core Web API
- Clean Architecture
- Entity Framework Core
- PostgreSQL
- Npgsql
- JWT bearer authentication
- Local file storage abstraction

### Frontend

- Next.js
- TypeScript
- Tailwind CSS
- Local shadcn-style UI primitives
- Responsive dashboard shell
- Mobile-friendly layouts
- Typed API clients and defensive response parsing

### AI Planned

- Python FastAPI
- Async AI jobs
- Document intelligence
- Summarization
- RAG search
- Recommendation engine

### Testing

- xUnit
- EF model mapping tests
- API/controller tests
- Storage service tests
- Frontend type-checking
- Frontend linting
- Production frontend build validation
- Targeted build/test validation per slice

---

## Multi-Tenancy and Security

LCCAP SaaS is designed around strict tenant isolation.

Core rule:

```text
account_id = tenant boundary
```

Security principles:

- No cross-tenant reads
- No cross-tenant writes
- No public `accountId` accepted from requests
- Tenant context comes from authenticated JWT claims
- Commands and queries enforce account scope
- Export downloads return 404 for cross-tenant access
- File storage paths are tenant-scoped
- Stored server paths are not exposed in API responses
- Development demo seed is disabled by default and must be explicitly enabled
- LocalStorage JWT is acceptable for MVP development only; production should move toward httpOnly cookies, CSRF protection, CSP, and refresh-token rotation

---

## Data Model Overview

Core entities:

```text
Account
User
Plan
PlanSection
Document
FileAsset
ActionItem
MonitoringIndicator
MonitoringUpdate
ExportJob
AuditLog
Role
Permission
UserRole
```

Planned / phase entities:

```text
Barangay
CriticalFacility
GeoJsonLayerFeature
SectionComment
AiJob
ExposureAnalysisJob
ExposureSummary
FundingSource
FundingProgram
FundingApplication
```

---

## AI and Intelligence Roadmap

AI is intentionally designed as an asynchronous support layer, not inline application logic.

Planned AI capabilities:

- Draft plan sections
- Summarize uploaded documents
- Extract document metadata
- Recommend adaptation and mitigation actions
- Generate evidence-linked insights
- Support RAG over LCCAP documents and plan data
- Support future exposure analysis and prioritization workflows

AI outputs should be:

- explainable
- auditable
- linked to source data
- stored as jobs/results
- reviewed by humans before adoption

---

## Frontend Direction

The frontend is implemented as an enterprise SaaS interface using:

- Next.js
- TypeScript
- Tailwind CSS
- Local shadcn-style UI primitives
- Responsive layouts
- Accessible components
- Dashboard-oriented design
- Typed API client modules for auth, plans, documents, actions, monitoring, and exports

Recommended visual identity:

```text
Base: White / Slate
Primary Accent: Emerald
Secondary Accent: Teal / Climate Blue
Risk Colors: Amber / Red
Success: Green
```

The product should feel like a:

```text
Modern LGU Climate Planning Command Center
```

Not a generic green environmental brochure.

The UI should be:

- white-based
- clean
- responsive
- enterprise-grade
- map and dashboard friendly
- usable on desktop, laptop, tablet, and mobile

---

## Local Setup & Development

### Prerequisites

- .NET 8 SDK
- Docker Desktop or a local PostgreSQL 16 instance
- PowerShell
- Node.js / npm
- Python for future AI service

### Start PostgreSQL

From the repository root:

```powershell
cd C:\projects\LCCAP
docker compose up -d
```

The committed `docker-compose.yml` defaults to:

```text
Host: localhost
Port: 55432
Database: lccap_db
Username: lccap_user
Password: lccap_password
```

If your existing Docker volume was initialized earlier with a different password, use the password stored in that local volume or recreate the volume. During local testing, one existing environment used `Password=123456`.

### Restore and Build Backend

```powershell
cd C:\projects\LCCAP
dotnet restore Lccap.sln
dotnet build Lccap.sln
```

### Run Backend API

Set local environment variables before running the API:

```powershell
cd C:\projects\LCCAP

$env:ConnectionStrings__Postgres="Host=localhost;Port=55432;Database=lccap_db;Username=lccap_user;Password=lccap_password"
$env:Jwt__Issuer="Lccap.Api.Dev"
$env:Jwt__Audience="Lccap.Client.Dev"
$env:Jwt__SigningKey="DEV_ONLY_HS256_SIGNING_KEY_MINIMUM_32_CHARACTERS_CHANGE_BEFORE_PRODUCTION_00000000"
$env:Jwt__ExpirationMinutes="60"
$env:DemoSeed__Enabled="false"

dotnet run --project .\src\Lccap.Api\Lccap.Api.csproj
```

If your local PostgreSQL volume uses `123456` instead of the committed Docker Compose password, use:

```powershell
$env:ConnectionStrings__Postgres="Host=localhost;Port=55432;Database=lccap_db;Username=lccap_user;Password=123456"
```

Expected API URL:

```text
http://localhost:5243
```

### Seed Demo Users Locally

Demo seeding is **development-only**, disabled by default, and requires an explicit password.

To seed the demo accounts and users once:

```powershell
cd C:\projects\LCCAP

$env:ConnectionStrings__Postgres="Host=localhost;Port=55432;Database=lccap_db;Username=lccap_user;Password=lccap_password"
$env:Jwt__Issuer="Lccap.Api.Dev"
$env:Jwt__Audience="Lccap.Client.Dev"
$env:Jwt__SigningKey="DEV_ONLY_HS256_SIGNING_KEY_MINIMUM_32_CHARACTERS_CHANGE_BEFORE_PRODUCTION_00000000"
$env:Jwt__ExpirationMinutes="60"
$env:DemoSeed__Enabled="true"
$env:DemoSeed__Password="DemoPassword123!"

dotnet run --project .\src\Lccap.Api\Lccap.Api.csproj
```

After seeding succeeds, restart the API with:

```powershell
$env:DemoSeed__Enabled="false"
```

Do not commit real passwords. Do not enable demo seed outside local development.

### Run Frontend

Create or update `frontend/.env.local`:

```powershell
cd C:\projects\LCCAP\frontend
Set-Content .env.local "NEXT_PUBLIC_API_BASE_URL=http://localhost:5243"
```

Run the frontend in production-like mode:

```powershell
cd C:\projects\LCCAP\frontend
npm install
npm run build
$env:PORT="3010"
npm start
```

Frontend URL:

```text
http://localhost:3010
```

Use `npm start` after `npm run build` for local demo testing. Use `npm run dev` only when actively developing frontend code and needing hot reload.

### Run Tests

```powershell
dotnet test tests/Lccap.Api.Tests/Lccap.Api.Tests.csproj
dotnet test tests/Lccap.Infrastructure.Tests/Lccap.Infrastructure.Tests.csproj
```

### Frontend Quality Checks

```powershell
cd C:\projects\LCCAP\frontend
npm run type-check
npm run lint
npm run build
```

### Targeted Test Examples

```powershell
dotnet test tests/Lccap.Api.Tests/Lccap.Api.Tests.csproj --filter PlansControllerTests
dotnet test tests/Lccap.Api.Tests/Lccap.Api.Tests.csproj --filter PlanSectionsControllerTests
dotnet test tests/Lccap.Api.Tests/Lccap.Api.Tests.csproj --filter DocumentsControllerTests
dotnet test tests/Lccap.Api.Tests/Lccap.Api.Tests.csproj --filter ActionItemsControllerTests
dotnet test tests/Lccap.Api.Tests/Lccap.Api.Tests.csproj --filter MonitoringControllerTests
dotnet test tests/Lccap.Api.Tests/Lccap.Api.Tests.csproj --filter ExportControllerTests
```

---

## Demo Login Users

All demo users use this local development password when seeded with the instructions above:

```text
DemoPassword123!
```


| User type      | Email                        | Role          | Scope    | LGU / Account        | Login status                                                            |
| -------------- | ---------------------------- | ------------- | -------- | -------------------- | ----------------------------------------------------------------------- |
| Platform admin | `platform.admin@lccap.local` | `SystemAdmin` | Platform | None                 | Seeded, but may not log in until platform-user login support is enabled |
| LGU planner    | `naga.planner@lccap.local`   | `Planner`     | Tenant   | Naga City Demo LGU   | Use for primary MVP demo                                                |
| LGU viewer     | `naga.viewer@lccap.local`    | `Viewer`      | Tenant   | Naga City Demo LGU   | Tenant demo user                                                        |
| LGU planner    | `pasig.planner@lccap.local`  | `Planner`     | Tenant   | Pasig City Demo LGU  | Tenant demo user                                                        |
| LGU viewer     | `pasig.viewer@lccap.local`   | `Viewer`      | Tenant   | Pasig City Demo LGU  | Tenant demo user                                                        |
| LGU planner    | `quezon.planner@lccap.local` | `Planner`     | Tenant   | Quezon City Demo LGU | Tenant demo user                                                        |
| LGU viewer     | `quezon.viewer@lccap.local`  | `Viewer`      | Tenant   | Quezon City Demo LGU | Tenant demo user                                                        |


Recommended MVP demo login:

```text
Email: naga.planner@lccap.local
Password: DemoPassword123!
```

Demo users are for local MVP testing only. They do not represent official government accounts, submission reviewers, approval authorities, or national platform operators.

---

## End-to-End Demo Script

Use this script after the backend and frontend are running.

### 1. Login

Open:

```text
http://localhost:3010/login
```

Login with:

```text
naga.planner@lccap.local
DemoPassword123!
```

Expected result:

- Login succeeds
- User lands on `/dashboard`
- Topbar shows `naga.planner@lccap.local`

### 2. Open or Create Plan

Go to:

```text
/plans
```

Expected result:

- Existing tenant plans appear in **Your workspaces**
- Click **Open workspace** for an existing plan, or create a new one

Suggested new plan values:

```text
Title: Naga City LCCAP 2026-2030
Start year: 2026
End year: 2030
Status: Draft
Template mode: New
```

Expected result:

- The app redirects to `/plans/{realPlanId}`
- Default sections appear in the workspace

### 3. Edit Section

Open the **Executive Summary** section and enter:

```text
This is a draft executive summary for the Naga City LCCAP 2026-2030. This section will summarize priority climate risks, planned adaptation and mitigation actions, evidence references, and monitoring approach.
```

Click **Save section**.

Expected result:

- Save succeeds
- Last edited timestamp appears
- Refreshing the page keeps the saved content

### 4. Upload Document

Use a small PDF, DOCX, XLSX, PNG, JPG, or JPEG.

Suggested values:

```text
Title: CLUP Reference Map
Category: Map
Description: Sample supporting document for local climate planning evidence.
```

Expected result:

- Upload succeeds
- Document appears in the attached documents list
- Refreshing the workspace reloads the document list

### 5. Create Action Item

Suggested values:

```text
Title: Improve flood early warning coverage
Description: Expand flood early warning coverage by updating barangay-level response protocols, improving communication channels, and coordinating preparedness drills with local disaster risk reduction teams.
Action type: Adaptation
Status: Planned
Sector: Disaster Risk Reduction and Management
Responsible office: City DRRMO
Budget amount (PHP): 500000
Funding source: Local DRRM Fund
Timeline start: 2026-01-01T08:00
Timeline end: 2026-12-31T17:00
KPI: Number of barangays covered by updated flood early warning and response protocol
Priority score: 8
```

Expected result:

- Action is created
- Action appears in the action items list

### 6. Create Monitoring Indicator

Suggested values:

```text
Name: Barangays covered by flood early warning protocol
Description: Tracks the number of barangays with updated flood early warning and response protocols linked to the local DRRM implementation plan.
Unit: barangays
Status: InProgress
Baseline value: 5
Current value: 5
Target value: 20
Progress (%): 25
Frequency: Quarterly
Responsible office: City DRRMO
```

Expected result:

- Indicator is created
- Indicator appears in the monitoring indicators list

### 7. Generate and Download PDF Draft Package

In the export section:

1. Click **Generate PDF draft**
2. Wait for the latest export job to show **Completed**
3. Click **Download PDF**

Expected result:

- PDF downloads as `lccap-draft-package.pdf`
- PDF contains the plan title and saved section content

This PDF is a draft working output for internal preparation. It is not an official submission package.

---

## Testing and Quality Gates

Every completed backend slice must pass:

```powershell
dotnet build Lccap.sln
```

And targeted tests for the affected module.

Every completed frontend slice must pass:

```powershell
cd frontend
npm run type-check
npm run lint
npm run build
```

Current test categories:

- API/controller tests
- EF Core mapping tests
- storage service tests
- authentication tests
- frontend type-check / lint / build checks
- manual MVP E2E validation

Quality rules:

- Do not deliver code with compile errors
- Do not deliver code with failing tests
- Do not update tracker unless build/tests pass
- Do not guess database schema
- Always preserve tenant isolation
- Use exact schema when implementing database-facing tasks
- Keep MVP, Phase 2, and Phase 3 scope separated

---

## Roadmap

### MVP

- Backend core APIs
- Auth and tenant context
- Plan workspace
- Plan sections
- Documents and file storage
- Action items
- Monitoring
- PDF exports
- Download exports
- Frontend foundation
- Demo workflow

### Phase 2 (later)

- Evidence index and review comments
- CCET / funding-readiness tagging
- PSF proposal package helper (preparation aid)
- Richer exports and monitoring / operational readiness features

### Phase 3 (later)

- Interoperability
- PostGIS / spatial analytics and exposure summaries
- Scenario comparison and recommendation engine
- Integration-ready APIs and exports
- Operational hardening

---

## Enterprise Design Principles

- Plan-centric architecture
- Strict multi-tenant isolation
- Clean Architecture boundaries
- Explicit database schema fidelity
- FileAsset storage abstraction
- Async AI processing
- Auditable workflows
- Test-first hardening
- Secure-by-default API behavior
- Responsive enterprise UI
- Phase discipline: MVP, Phase 2, and Phase 3 must not be mixed without explicit product approval

---

## Author

**Jeff Martin Abayon**

Full-Stack Engineer  
Enterprise Systems Architecture  
Climate Planning SaaS / Engineering Data Platforms

Calgary, Canada

[jmjabayon@gmail.com](mailto:jmjabayon@gmail.com)

[LinkedIn Profile](https://www.linkedin.com/in/jeff-martin-abayon-calgary/)