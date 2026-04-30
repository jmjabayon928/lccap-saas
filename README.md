# LCCAP SaaS — Enterprise Local Climate Action Planning Platform

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)
![PostgreSQL](https://img.shields.io/badge/Database-PostgreSQL-336791)
![Architecture](https://img.shields.io/badge/Architecture-Clean%20Architecture-success)
![AI](https://img.shields.io/badge/AI-Python%20FastAPI-blue)
![Status](https://img.shields.io/badge/Status-MVP%20Development-green)
![License](https://img.shields.io/badge/License-Private-lightgrey)

LCCAP SaaS is an enterprise-grade, multi-tenant planning and intelligence platform for **Local Climate Change Action Plan (LCCAP)** formulation, management, monitoring, document handling, action planning, and reporting.

The system is designed for Local Government Units (LGUs), planners, reviewers, national agencies, and climate governance stakeholders who need a structured, auditable, and scalable platform for climate adaptation, mitigation, disaster risk reduction, and monitoring workflows.

Unlike static document-based LCCAP preparation, this platform treats climate planning as a structured, lifecycle-aware workflow:

- Plans are created as governed workspace entities
- Sections are editable and version-ready
- Supporting documents are managed as FileAssets
- Actions are tracked as structured climate interventions
- Monitoring indicators are recorded and updated
- Exports are generated from system data
- AI features are planned as asynchronous, auditable jobs

The long-term goal is to evolve from an MVP planning workspace into a full climate intelligence and decision-support platform with spatial analysis, exposure analytics, document intelligence, recommendation engines, and cross-LGU insights.

---

## Table of Contents

- Executive Summary
- Product Overview
- Target Users
- Current MVP Status
- Core Modules
- Completed Backend Capabilities
- In Progress / Planned Capabilities
- System Architecture
- Technology Stack
- Multi-Tenancy and Security
- Data Model Overview
- AI and Intelligence Roadmap
- Frontend Direction
- Local Setup & Development
- Testing and Quality Gates
- Roadmap
- Enterprise Design Principles
- Author

---

## Executive Summary

Local Climate Change Action Plans require LGUs to gather climate data, assess risks, identify adaptation and mitigation actions, define monitoring indicators, prepare reports, and align with national climate policy frameworks.

In many real-world environments, this process is still managed through disconnected Word documents, spreadsheets, PDFs, shared folders, and manual review processes.

LCCAP SaaS provides a structured SaaS platform for:

- Creating LCCAP plans
- Managing plan sections
- Uploading and organizing supporting documents
- Defining climate actions
- Tracking monitoring indicators
- Generating exportable reports
- Maintaining tenant-scoped data isolation
- Preparing for future AI-assisted drafting, analysis, and recommendations

The MVP backend is currently implemented using **.NET 8 Clean Architecture**, **Entity Framework Core**, and **PostgreSQL**, with planned frontend development using **Next.js**, **shadcn/ui**, and responsive enterprise dashboard patterns.

---

## Product Overview

LCCAP SaaS is designed as a **plan-centric climate planning platform**.

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

- National Government Reviewer
- Policy / Compliance Officer
- Platform Administrator
- Technical Support Operator

### External / Future Users

- Citizens
- NGOs
- Development partners
- Funding agencies
- Public portal viewers

---

## Current MVP Status

The backend MVP is actively under development.

### Completed Backend Slices

| Module | Status |
|---|---|
| Solution structure | Complete |
| PostgreSQL baseline schema | Complete |
| Clean Architecture project setup | Complete |
| Domain base entities | Complete |
| EF Core DbContext and mappings | Complete |
| Auth / JWT bearer authentication | Complete |
| Current user / tenant context | Complete |
| Plans API | Complete |
| Default plan section seeding | Complete |
| Plan Sections API | Complete |
| Documents API | Complete |
| Local File Storage Service | Complete |
| Monitoring API | Complete |
| Action Items API | Complete |
| Export Job / PDF generation | Complete |
| Export download endpoint | Complete |
| Mapping tests for key entities | Complete |

### In Progress / Planned MVP Items

| Module | Status |
|---|---|
| Frontend foundation | Not started |
| Responsive enterprise UI shell | Not started |
| shadcn/ui setup | Not started |
| Dashboard preview | Not started |
| Plan workspace UI | Not started |
| Documents UI | Not started |
| Actions UI | Not started |
| Monitoring UI | Not started |
| Export UI | Not started |
| Swagger/OpenAPI polish | Planned |
| Seed/demo data | Planned |
| End-to-end smoke validation | Planned |

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

Tenant isolation is enforced through the authenticated current user context.

---

### 2. Plans

Plans are the root LCCAP workspace entity.

Capabilities:

- Create plan
- Update plan
- Get plan by ID
- Tenant-scoped access
- Default plan sections seeded after plan creation
- Validation for title, years, status, and template mode

Current API surface:

```text
POST /api/plans
PUT  /api/plans/{planId}
GET  /api/plans/{planId}
```

---

### 3. Plan Sections

Plan sections represent editable sections of the LCCAP document.

Default sections are created automatically when a plan is created:

| Order | Section Key | Title |
|---:|---|---|
| 10 | executive_summary | Executive Summary |
| 20 | introduction | Introduction and LGU Profile |
| 30 | climate_risk_assessment | Climate and Disaster Risk Assessment |
| 40 | adaptation_actions | Adaptation Actions |
| 50 | mitigation_actions | Mitigation Actions |
| 60 | implementation_plan | Implementation Plan |
| 70 | monitoring_evaluation | Monitoring and Evaluation |
| 80 | references_annexes | References and Annexes |

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

## Completed Backend Capabilities

The current backend implements the major MVP workflow:

```text
Authenticate
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

- Frontend foundation
- Responsive dashboard layout
- Plan workspace UI
- Section editor UI
- Document upload UI
- Action item UI
- Monitoring UI
- Export UI
- Demo seed data
- Swagger/OpenAPI polishing
- End-to-end smoke scripts

### Phase 2

- GeoJSON map upload and visualization
- Barangay reference data
- Critical facilities module
- Section comments / review notes
- Improved document intelligence
- Excel exports
- Richer dashboard cards
- Collaboration features

### Phase 3

- PostGIS spatial analysis
- Exposure analytics
- Scenario simulation
- AI recommendation engine
- Cross-LGU analytics
- Advanced observability
- Security hardening
- Operational runbooks

---

## System Architecture

LCCAP SaaS uses a modular Clean Architecture approach.

```text
┌────────────────────────────────────┐
│              Frontend              │
│     Next.js + shadcn/ui planned    │
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

### Frontend Planned

- Next.js
- TypeScript
- Tailwind CSS
- shadcn/ui
- Responsive dashboard shell
- Mobile-friendly layouts

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

The frontend is planned as an enterprise SaaS interface using:

- Next.js
- TypeScript
- Tailwind CSS
- shadcn/ui
- responsive layouts
- accessible components
- dashboard-oriented design

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
- PostgreSQL
- PowerShell
- Node.js / npm for future frontend
- Python for future AI service

### Restore and Build

```powershell
dotnet restore Lccap.sln
dotnet build Lccap.sln
```

### Run API

```powershell
dotnet run --project src/Lccap.Api/Lccap.Api.csproj
```

### Run Tests

```powershell
dotnet test tests/Lccap.Api.Tests/Lccap.Api.Tests.csproj
dotnet test tests/Lccap.Infrastructure.Tests/Lccap.Infrastructure.Tests.csproj
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

## Testing and Quality Gates

Every completed slice must pass:

```powershell
dotnet build Lccap.sln
```

And targeted tests for the affected module.

Current test categories:

- API/controller tests
- EF Core mapping tests
- storage service tests
- authentication tests
- smoke tests

Quality rules:

- Do not deliver code with compile errors
- Do not deliver code with failing tests
- Do not update tracker unless build/tests pass
- Do not guess database schema
- Always preserve tenant isolation
- Use exact schema when implementing database-facing tasks

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

### Phase 2

- Maps / GeoJSON
- Barangays
- Facilities
- Collaboration
- Document intelligence
- Better dashboards
- Excel exports
- Health checks and observability improvements

### Phase 3

- PostGIS spatial analysis
- Exposure analysis
- Scenario simulation
- AI recommendations
- Cross-LGU benchmarking
- Funding workflows
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
- Phase discipline: MVP, Phase 2, and Phase 3 must not be mixed without explicit approval

---

## Author

**Jeff Martin Abayon**

Full-Stack Engineer  
Enterprise Systems Architecture  
Climate Planning SaaS / Engineering Data Platforms

Calgary, Canada

[jmjabayon@gmail.com](mailto:jmjabayon@gmail.com)

[LinkedIn Profile](https://www.linkedin.com/in/jeff-martin-abayon-calgary/)
