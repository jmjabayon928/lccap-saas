# LCCAP SaaS Architecture

> This document is the canonical engineering architecture reference for LCCAP SaaS.
>
> - **README.md** is optimized for onboarding, product overview, roadmap, setup, and current status.
> - **ARCHITECTURE.md** is optimized for maintainers: system design, invariants, contracts, database intent, security boundaries, phase discipline, and quality gates.

---

## 1) System Overview

LCCAP SaaS is a multi-tenant, enterprise-grade climate planning platform for Local Government Units.

The platform helps LGUs create, manage, monitor, and export Local Climate Change Action Plans through structured workflows instead of disconnected files.

The system is designed around a plan-centric architecture:

```text
Plan = Aggregate Root
```

All major planning entities depend on a Plan:

```text
Plan
  ├── PlanSections
  ├── Documents
  ├── FileAssets
  ├── ActionItems
  ├── MonitoringIndicators
  ├── ExportJobs
  └── Future AI / Maps / Spatial Analytics
```

This enables the platform to evolve from an MVP planning workspace into a full climate intelligence system.

---

## 2) Architectural Goals

LCCAP SaaS is deliberately engineered around the following goals.

### 2.1 Plan-centric correctness

The Plan is the core workspace entity.

All operations involving sections, documents, actions, monitoring, exports, maps, and AI context must be scoped through a Plan wherever applicable.

### 2.2 Multi-tenant isolation

Each LGU is represented as an Account.

```text
account_id = tenant boundary
```

No backend operation should read or write across tenants.

### 2.3 Schema fidelity

The application must match the PostgreSQL schema exactly.

Rules:

- Do not guess column names
- Do not invent fields
- Do not ignore nullability
- Do not bypass constraints
- Do not introduce entity fields that are not mapped intentionally
- Database-facing implementation prompts must include exact schema

### 2.4 File abstraction

Physical file storage and logical document usage are separate.

```text
FileAsset = physical storage metadata
Document = logical use of file in LCCAP context
```

This allows files to be reused later by documents, maps, exports, AI jobs, and other modules.

### 2.5 AI as asynchronous layer

AI must not be inline business logic.

The intended pattern is:

```text
User request
  → AI Job created
  → Python service processes job
  → Result stored
  → UI displays auditable result
```

### 2.6 Phase discipline

MVP, Phase 2, and Phase 3 must remain separate.

- MVP should not accidentally include full GIS / PostGIS analytics
- Phase 2 should not jump into advanced spatial intelligence
- Phase 3 should not backflow into MVP unless explicitly approved

---

## 3) High-Level Architecture

```text
┌─────────────────────────────────────────────┐
│                 Frontend                    │
│       Next.js + shadcn/ui planned           │
│ Dashboard | Plans | Sections | Docs | Maps  │
└──────────────────────┬──────────────────────┘
                       │ HTTP / JSON
                       ▼
┌─────────────────────────────────────────────┐
│                 Lccap.Api                   │
│ Controllers | Auth | Middleware | Options   │
└──────────────────────┬──────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────┐
│             Lccap.Application               │
│ Commands | Queries | Interfaces | DTOs      │
└──────────────────────┬──────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────┐
│            Lccap.Infrastructure              │
│ EF Core | PostgreSQL | Storage | Security   │
└──────────────────────┬──────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────┐
│                PostgreSQL                   │
│ Plans | Sections | Files | Actions | Jobs   │
└─────────────────────────────────────────────┘
```

Future AI layer:

```text
┌─────────────────────────────────────────────┐
│             Python FastAPI AI Service       │
│ Summarization | RAG | Recommendations       │
└─────────────────────────────────────────────┘
```

---

## 4) Solution Structure

Expected solution layout:

```text
src/
  Lccap.Domain/
  Lccap.Application/
  Lccap.Infrastructure/
  Lccap.Api/

tests/
  Lccap.Domain.Tests/
  Lccap.Application.Tests/
  Lccap.Infrastructure.Tests/
  Lccap.Api.Tests/

db/
  migrations/

docs/
  state/
```

---

## 5) Layer Responsibilities

### 5.1 Lccap.Domain

The domain layer owns core entities and domain methods.

Examples:

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
```

Responsibilities:

- entity shape
- domain methods
- value-related validation when appropriate
- no infrastructure dependencies
- no HTTP concerns
- no EF Core configuration

### 5.2 Lccap.Application

The application layer owns use cases.

Responsibilities:

- commands
- queries
- application interfaces
- request/result models
- tenant-scoped business rules
- orchestration of domain entities
- validation that belongs to use-case flow

Examples:

```text
CreatePlanCommand
SavePlanSectionCommand
UploadDocumentCommand
CreateActionItemCommand
CreateIndicatorCommand
CreateExportJobCommand
DownloadExportQuery
```

The application layer depends on abstractions such as:

```text
ILccapDbContext
ICurrentUserContext
IFileStorageService
IClock
```

It must not depend directly on infrastructure classes.

### 5.3 Lccap.Infrastructure

The infrastructure layer implements persistence, storage, security helpers, and external integrations.

Responsibilities:

- EF Core DbContext
- entity configurations
- PostgreSQL mapping
- file storage implementation
- password hashing
- JWT token generation
- future AI service client
- future observability and operational integrations

### 5.4 Lccap.Api

The API layer exposes HTTP endpoints.

Responsibilities:

- controller endpoints
- authentication wiring
- authorization wiring
- request binding
- HTTP response mapping
- middleware registration
- options binding

Controllers should stay thin and delegate business logic to application commands and queries.

---

## 6) Core Domain Model

### 6.1 Account

Account represents the tenant / LGU.

Responsibilities:

- owns plans
- owns file assets
- owns tenant-scoped users and settings
- defines the tenant boundary

### 6.2 User

User represents an authenticated actor.

Responsibilities:

- login identity
- account membership
- role information
- authorship and audit fields

### 6.3 Plan

Plan is the aggregate root.

Responsibilities:

- organize LCCAP content
- own sections
- own documents
- own action items
- own monitoring indicators
- own export jobs
- provide context for future maps and AI

### 6.4 PlanSection

PlanSection represents editable LCCAP content.

Rules:

- belongs to a Plan
- belongs to an Account
- unique by `(plan_id, section_key)` for active sections
- stores editable title/content
- tracks last edited user and timestamp

### 6.5 Document

Document is the logical use of a file in a plan.

Rules:

- belongs to Account
- belongs to Plan
- references FileAsset
- stores category, title, description, tags, and metadata

### 6.6 FileAsset

FileAsset represents physical file storage metadata.

Rules:

- belongs to Account
- stores generated storage path
- stores original filename separately
- stores content type, extension, size, and hash
- never uses original filename as trusted storage path

### 6.7 ActionItem

ActionItem represents an adaptation or mitigation intervention.

Rules:

- belongs to Account
- belongs to Plan
- action type must be Adaptation or Mitigation
- budget cannot be negative
- timeline start cannot be after timeline end
- status must use the allowed status set

### 6.8 MonitoringIndicator

MonitoringIndicator tracks LCCAP implementation progress.

Rules:

- belongs to Account
- belongs to Plan
- may optionally link to an ActionItem
- status must use the allowed status set

### 6.9 MonitoringUpdate

MonitoringUpdate stores progress updates for an indicator.

Rules:

- belongs to Account
- belongs to MonitoringIndicator
- progress percent must be between 0 and 100 if provided

### 6.10 ExportJob

ExportJob tracks report generation.

Rules:

- belongs to Account
- belongs to Plan
- may reference generated FileAsset
- status transitions include Queued, Running, Completed, Failed, Cancelled

---

## 7) Persistence Architecture

### 7.1 Database

Primary database:

```text
PostgreSQL
```

Important PostgreSQL types:

| PostgreSQL | C# |
|---|---|
| uuid | Guid |
| timestamptz | DateTimeOffset |
| date | DateOnly |
| numeric | decimal |
| jsonb | JsonDocument |
| bigint | long |
| bytea | byte[] |

### 7.2 DbContext Contract

Application depends on `ILccapDbContext`, not directly on infrastructure.

Current required sets include:

```text
Plans
PlanSections
ActionItems
FileAssets
Documents
ExportJobs
```

The real implementation is `LccapDbContext`.

### 7.3 EF Core Mapping

Entity mapping is handled in infrastructure configuration files.

Mapping responsibilities:

- table names
- column names
- column types
- defaults
- constraints
- relationships
- indexes
- row version concurrency tokens
- JSONB fields

### 7.4 Row Version / Concurrency

Major mutable entities use:

```text
row_version bytea
```

Mapped as concurrency token.

Purpose:

- prevent silent overwrite conflicts
- support optimistic concurrency
- preserve enterprise-grade data integrity

---

## 8) Multi-Tenancy and Security Model

### 8.1 Tenant Boundary

The tenant boundary is always:

```text
account_id
```

All major data access must include tenant scope.

### 8.2 Current User Context

Authenticated user context provides:

```text
UserId
AccountId
IsAuthenticated
```

Commands and queries must resolve tenant scope from `ICurrentUserContext`.

### 8.3 Strict Rule

Do not accept tenant identity from:

- request body
- query string
- route parameter
- client-controlled form field

The client may provide resource IDs, but the backend must verify those resources belong to the current account.

### 8.4 Cross-Tenant Behavior

For cross-tenant access attempts:

```text
Return 404
```

Do not leak whether another tenant’s resource exists.

### 8.5 Authentication

Authentication uses standard Microsoft JWT bearer middleware.

Token claims include:

```text
sub
account_id
email
role
```

`account_id` is the source of tenant context.

---

## 9) File Storage Architecture

### 9.1 Abstraction

Application layer depends on:

```text
IFileStorageService
```

Infrastructure implements:

```text
LocalFileStorageService
```

### 9.2 Storage Rules

- store files under tenant-scoped paths
- never trust original filename for storage path
- generate stored filename
- compute SHA256
- validate allowed extension
- validate max file size
- reject empty stream
- protect against path traversal

### 9.3 Storage Path

```text
uploads/{accountId}/{yyyy}/{MM}/{generatedGuid}{extension}
```

### 9.4 Future Providers

The storage abstraction allows future providers:

- Azure Blob Storage
- AWS S3
- Google Cloud Storage
- private object storage

---

## 10) API Surface

### 10.1 Auth

```text
POST /api/auth/login
```

### 10.2 Plans

```text
POST /api/plans
PUT  /api/plans/{planId}
GET  /api/plans/{planId}
```

### 10.3 Plan Sections

```text
GET /api/plans/{planId}/sections
GET /api/plans/{planId}/sections/{sectionKey}
PUT /api/plans/{planId}/sections/{sectionKey}
```

### 10.4 Documents

```text
POST /api/documents/upload
GET  /api/plans/{planId}/documents
```

### 10.5 Action Items

```text
POST /api/plans/{planId}/actions
PUT  /api/actions/{actionItemId}
GET  /api/plans/{planId}/actions
GET  /api/actions/{actionItemId}
```

### 10.6 Monitoring

```text
POST /api/monitoring/indicators
PUT  /api/monitoring/indicators/{indicatorId}
GET  /api/monitoring/plans/{planId}/indicators
```

### 10.7 Exports

```text
POST /api/plans/{planId}/exports/pdf
GET  /api/exports/{exportJobId}
GET  /api/exports/{exportJobId}/download
```

---

## 11) Export Architecture

Exports are represented as jobs.

Current MVP behavior:

```text
Create export request
  → create ExportJob
  → mark Running
  → generate minimal PDF
  → store PDF via IFileStorageService
  → create FileAsset
  → link FileAsset to ExportJob
  → mark Completed
```

Download behavior:

```text
GET /api/exports/{exportJobId}/download
  → verify current account
  → verify completed status
  → verify FileAsset belongs to account
  → open stream through IFileStorageService
  → return file response
```

Security rules:

- no stored path exposed
- cross-tenant access returns 404
- incomplete export returns 409
- missing/deleted file returns 404

---

## 12) Default Plan Workspace

When a Plan is created, the system automatically creates default sections.

Default sections:

```text
10 executive_summary
20 introduction
30 climate_risk_assessment
40 adaptation_actions
50 mitigation_actions
60 implementation_plan
70 monitoring_evaluation
80 references_annexes
```

This ensures every plan starts with a standard LCCAP workspace.

---

## 13) AI Architecture Vision

AI is planned as an asynchronous service layer.

### 13.1 AI Job Pattern

```text
User triggers AI action
  → .NET API creates AI job
  → Python FastAPI processes job
  → Result stored in PostgreSQL
  → UI displays result
```

### 13.2 Planned AI Capabilities

- draft plan sections
- summarize documents
- extract document metadata
- recommend action items
- identify data gaps
- support RAG queries
- support exposure analysis workflows

### 13.3 AI Governance

AI outputs must be:

- stored
- explainable
- evidence-linked
- auditable
- human-reviewed

---

## 14) Phase Boundaries

### 14.1 MVP

MVP includes:

- Auth
- Plans
- Plan sections
- Documents
- File assets
- Action items
- Monitoring
- PDF exports
- Download exports
- Frontend foundation
- Demo workflow

### 14.2 Phase 2

Phase 2 includes:

- GeoJSON upload
- Map visualization
- Barangay reference data
- Critical facilities
- Section comments
- Document summarization
- Excel exports
- richer dashboards

Explicitly not Phase 2:

- PostGIS intersection analytics
- raster analysis
- automated hazard exposure computation
- cross-LGU benchmarking

### 14.3 Phase 3

Phase 3 includes:

- PostGIS
- spatial intersections
- exposure analytics
- scenario simulations
- AI recommendations
- cross-LGU insights
- advanced observability
- operational hardening

---

## 15) Frontend Architecture Direction

Frontend is planned with:

```text
Next.js
TypeScript
Tailwind CSS
shadcn/ui
```

### 15.1 UI Principles

- enterprise SaaS look
- responsive desktop/tablet/mobile layout
- accessible components
- white/slate base
- emerald/teal climate accents
- clean dashboard cards
- clear plan workspace navigation

### 15.2 Planned Frontend Shell

```text
App Shell
  ├── Sidebar
  ├── Top Header
  ├── Dashboard
  ├── Plan Workspace
  ├── Sections
  ├── Documents
  ├── Actions
  ├── Monitoring
  └── Exports
```

### 15.3 Recommended Theme

```text
Background: White / Slate-50
Text: Slate-900
Muted: Slate-500
Primary: Emerald-700
Secondary: Teal-700
Info: Blue-600
Warning: Amber-600
Danger: Red-600
Success: Green-600
```

---

## 16) Testing Strategy

### 16.1 Required Quality Gate

Every completed slice must run:

```powershell
dotnet build Lccap.sln
```

And targeted tests for the affected module.

### 16.2 Current Test Types

- API/controller tests
- infrastructure mapping tests
- local file storage tests
- authentication tests
- smoke tests

### 16.3 Test Principles

- deterministic tests
- no hidden tenant assumptions
- no default interface throws for active DbContext contracts
- fake contexts must explicitly implement required sets
- provider-specific behavior must not leak into production DbContext
- mapping tests must verify production EF model where relevant

---

## 17) Quality Gates for AI / Cursor Work

When using AI or Cursor agents:

- use explicit read allowlist
- use explicit edit allowlist
- include exact schema for database work
- forbid schema guessing
- require build/tests before final report
- do not update tracker unless tests pass
- require final report only
- stop with blocker report when scope is insufficient

Database prompt rule:

```text
When a task reads from or writes to database-backed entities, include exact tables,
columns, nullability, defaults, constraints, FKs, and indexes needed for the task.
Do not allow inferred, invented, renamed, or unlisted columns.
```

---

## 18) Operational Notes

### 18.1 Tracker

`docs/state/LCCAP_IMPLEMENTATION_TRACKER.json` tracks implementation state.

Known note:

- Some tracker entries may lag behind actual work.
- `current_state` should be manually corrected when needed.
- Do not rely on tracker alone without verifying code/tests.

### 18.2 Git

Git is not currently initialized in this local project at the time of this documentation update.

Recommended future action:

```powershell
git init
git add .
git commit -m "Initial LCCAP MVP backend implementation"
```

After Git is initialized, use:

```powershell
git diff --name-only
git status
```

for safer change tracking.

---

## 19) Architecture Decision Records

### ADR-001: Plan as Aggregate Root

The Plan is the central aggregate root. Sections, documents, actions, monitoring, exports, maps, and AI context are organized through Plan.

### ADR-002: Account as Tenant Boundary

Every major entity is scoped by `account_id`. Cross-tenant access must return 404 where resource existence should not be leaked.

### ADR-003: Document and FileAsset Separation

Document represents logical use. FileAsset represents physical storage metadata.

### ADR-004: Clean Architecture

Application depends on interfaces. Infrastructure implements them. API orchestrates HTTP concerns only.

### ADR-005: AI as Asynchronous Job Layer

AI must run asynchronously through jobs/results, not as inline blocking business logic.

### ADR-006: PostgreSQL Schema Fidelity

EF Core mappings and C# entities must match PostgreSQL schema intentionally and exactly.

### ADR-007: Local Storage Behind Abstraction

MVP uses local storage, but all file operations go through `IFileStorageService` to support future cloud storage.

### ADR-008: Frontend Uses shadcn/ui

The frontend should use shadcn/ui for a clean, enterprise-grade, responsive SaaS interface.

---

## 20) Known Gaps

The following are known gaps or planned improvements:

- Frontend not yet implemented
- Swagger/OpenAPI not fully polished
- Demo seed data not finalized
- AI Python service not yet implemented
- Map/GeoJSON features are Phase 2
- PostGIS spatial intelligence is Phase 3
- Advanced observability is Phase 3
- Some tracker entries may require manual cleanup
- Git repository should be initialized for safer change tracking

---

## Appendix A — README Reference

The project README remains the canonical source for:

- product overview
- module list
- current MVP status
- local setup
- roadmap
- frontend direction
- author details

This architecture document focuses on maintainability, invariants, contracts, and architectural decisions.

See: `README.md`

---

## Author

**Jeff Martin Abayon**

Full-Stack Engineer  
Enterprise Systems Architecture  
Climate Planning SaaS / Engineering Data Platforms

Calgary, Canada

[jmjabayon@gmail.com](mailto:jmjabayon@gmail.com)

[LinkedIn Profile](https://www.linkedin.com/in/jeff-martin-abayon-calgary/)
