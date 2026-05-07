# LCCAP SaaS - Enterprise-Style Local Climate Action Planning Workspace

[![CI](https://github.com/jmjabayon928/lccap-saas/actions/workflows/ci.yml/badge.svg)](https://github.com/jmjabayon928/lccap-saas/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)]()
[![PostgreSQL](https://img.shields.io/badge/Database-PostgreSQL-336791)]()
[![Next.js](https://img.shields.io/badge/Frontend-Next.js%20%7C%20TypeScript-blue)]()
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20Architecture-success)]()
[![SaaS](https://img.shields.io/badge/SaaS-Multi--Tenant-orange)]()
[![Tests](https://img.shields.io/badge/Tests-.NET%20CI%20%2B%20Local%20Frontend-brightgreen)]()
[![Status](https://img.shields.io/badge/Status-MVP%20%2B%20Phase%202%20Complete%20%2B%20Phase%203%20Exposure%20Partial-brightgreen)]()
[![AI](https://img.shields.io/badge/AI-Roadmap%20Only%20(Exposure%20Compute%20Implemented)-lightgrey)]()
[![License](https://img.shields.io/badge/License-Private-lightgrey)]()

**LCCAP SaaS** is an **LGU-facing operating workspace** for organizing **Local Climate Change Action Plan (LCCAP)** preparation: plan sections, supporting documents and evidence, climate actions, monitoring indicators, accountability history, and **export-ready draft packages** that teams can refine offline and route through their own official channels.

It **complements existing official government and donor systems**. It does **not** supersede, replace, or act as an official submission, approval, national reporting, funding, certification, or diagnostic platform.

**Current product state.** The **MVP plus Phase 2** workspace foundation is **locally validated**: evidence organization (index and links), section review comments, CCET and funding-readiness surfaces, funding source/program catalogs and planned action allocations, richer export helpers (package manifest and CSV working outputs), monitoring update history, an operational dashboard-style activity view, a GeoJSON/map-layer workspace foundation, and an in-app **notifications and read-only collaboration summary** foundation with **notification events wired from real workspace actions**. Additionally, **Phase 3 exposure workflow is partially implemented** for **facility-only exposure analysis** using a feature-flagged **Python FastAPI exposure-computation service** and .NET job processing/persistence, with a **frontend exposure readiness panel** and **exposure summary display/status messaging**. This remains an **LGU preparation workspace** only—not a production-national SaaS, official submission portal, PSF application system, government approval platform, national reporting system, PostGIS exposure engine, full GIS analytics product, or AI decision-making platform.

The same workspace treats climate planning as structured, lifecycle-aware preparation rather than a single static file:

- Plans are governed workspace entities.
- Sections are editable, auditable, and restorable from revision history.
- Supporting documents are organized as evidence-linked FileAssets.
- Documents can be uploaded, listed, edited, archived, and audited.
- Actions are structured climate interventions with lifecycle status.
- Action items can be created, edited, archived, and audited.
- Monitoring indicators are recorded, updated, archived, and audited.
- Exports produce **working outputs** from structured plan data.
- PDF exports are draft-ready packages, not a substitute for mandated submission portals.
- Admin and Reviewer users can review accountability history through an Audit Viewer.
- Server-side RBAC protects workspace operations.
- Optimistic concurrency prevents accidental overwrites.
- Paginated lists prevent unbounded tenant responses.
- Refresh-token-backed sessions support safer local demo and pilot workflows.
- AI features are planned as asynchronous, auditable jobs in later phases (Phase 3 exposure computation is intentionally limited to facility-only geometry evaluation, not AI decision-making).

Later phases extend capability with interoperability, PostGIS / spatial analytics, scenario comparison, recommendation-style assistance, and optional broader AI/Python services if adopted. **Phase 3 exposure workflow is intentionally limited** to facility-only analysis (explicit `EPSG:4326` and `Polygon` / `MultiPolygon` hazard geometries with boundary-inclusive point-in-polygon), and **does not replace** PostGIS-based analytics or official risk assessment outputs. The shipped **MVP plus Phase 2** scope remains an **internal preparation and organization** workspace for LGU teams, not a mandated channel or analytics authority.

---

## Table of Contents

- Executive Summary
- Product positioning and complementary role
- MVP emphasis and non-goals
- Phase roadmap
- Product Overview
- Target Users
- Current MVP + Phase 2 + Phase 3 Exposure Status
- Core Modules
- Completed MVP Capabilities
- In Progress / Planned Capabilities
- System Architecture
- Technology Stack
- Multi-Tenancy and Security
- Server-Side RBAC Matrix
- Data Model Overview
- AI and Intelligence Roadmap
- Frontend Direction
- Local Setup and Development
- Demo Login Users
- End-to-End Demo Script
- Testing and Quality Gates
- Roadmap
- Enterprise Design Principles
- Current Enterprise MVP and Phase 2 Caveats
- Author

---

## Executive Summary

Local Climate Change Action Plans require LGUs to gather climate data, assess risks, identify adaptation and mitigation actions, define monitoring indicators, prepare reports, and align with national climate policy frameworks.

In many real-world environments, this process is still managed through disconnected Word documents, spreadsheets, PDFs, shared folders, email threads, manual review notes, and offline coordination.

LCCAP SaaS provides a structured SaaS **planning workspace** for:

- Creating LCCAP plans and organizing sections.
- Uploading and organizing supporting documents and evidence.
- Defining climate actions.
- Tracking monitoring indicators.
- Editing and archiving records without hard deletion.
- Preserving accountability through audit logs.
- Restoring section content from revision history.
- Generating **export-ready draft** outputs for offline refinement and use with official processes.
- Maintaining tenant-scoped data isolation.
- Preparing for later-phase AI-assisted drafting, analysis, and recommendations.

It **complements** official government and donor systems. It does not replace mandated portals, diagnostics, reporting systems, approvals, or policy guidance.

The MVP is implemented using **.NET 8 Clean Architecture**, **Entity Framework Core**, **PostgreSQL**, and a **Next.js / TypeScript / Tailwind CSS** frontend.

**Phase 2** adds evidence index and CSV exports, document evidence links to sections and actions, **section review comments**, **CCET / funding-readiness** tagging and panels, **funding source and program catalogs** with **action funding allocations** (planned allocations only; not a funding portal), **monitoring update history**, **richer export package** helpers (manifest and matrices as working outputs), **operational dashboard** data for the plan workspace, **map workspace / GeoJSON layer** registration, and **in-app notifications** plus a **read-only collaboration summary** (groups populated via tenant-admin or seed data in Phase 2—no self-service group CRUD, invites, email, or WebSocket delivery).

**Phase 3 exposure workflow is partially implemented** for facility-only exposure analysis: a hazard-layer registration flow from GeoJSON map assets, exposure analysis job queue/processing, a feature-flagged Python FastAPI facility-only computation (explicit `EPSG:4326` with `Polygon` / `MultiPolygon` hazards and boundary-inclusive point-in-polygon containment), .NET persistence of validated facility-only `ExposureSummary` rows, and a frontend exposure readiness panel with completed/failed/zero-summary messaging. This remains limited to facility-only computation (no full PostGIS spatial analytics and no exposed area/population/risk metrics or official risk scoring outputs).

The updated **MVP plus Phase 2** flow has been locally validated end-to-end for the core preparation workspace:

```text
Login
  -> Refresh-token-backed session
  -> View paginated tenant plans
  -> Open real plan workspace
  -> Edit and archive plan metadata
  -> Edit and save section content
  -> View section history
  -> Restore section revisions
  -> Upload, list, edit, and archive documents
  -> Create, edit, and archive action items
  -> Create, edit, and archive monitoring indicators
  -> Generate PDF draft package
  -> Download generated PDF
  -> Review audit history as Admin or Reviewer
```

The updated **MVP plus Phase 2** codebase has also been hardened with:

- Server-side RBAC enforcement.
- Tenant isolation through authenticated current-user context.
- Soft-delete archive behavior.
- Audit logs with old/new/metadata snapshots.
- Audit Viewer for Admin and Reviewer roles.
- Optimistic concurrency with rowVersion tokens.
- Legacy empty rowVersion repair where supported.
- Paginated list endpoints.
- HttpOnly refresh-token cookies.
- Refresh-token rotation.
- Memory-only access-token handling in the frontend.
- Frontend type-check and lint validation.
- Backend solution-wide test coverage.

This is an enterprise-style **MVP plus Phase 2** foundation for demos and controlled pilot preparation. It is not yet a finished production SaaS and should not be presented as a national submission platform, official PSF channel, or high-scale production deployment without the remaining production hardening work.

---

## Product positioning and complementary role

LCCAP SaaS is positioned as an **enterprise-style, LGU-facing LCCAP operating workspace**: a planning workspace that helps teams **prepare better internal working packages** through organized sections, evidence, actions, monitoring, audit history, and **export-ready draft outputs**.

Existing official systems remain the authority for national reporting, donor-specific portals, mandated diagnostics, statutory approvals, certification, and official submissions.

**Complementary role.** This product is **not** a substitute for, and does not replicate the mandate of, systems and channels such as:

- CCC systems and guidance channels.
- DILG systems and guidance channels.
- LGA training and capacity-building programs.
- NICCDIES or other mandated national reporting systems.
- PSF-related official portals or eligibility systems.
- PlanSmart.
- UNDP SHIELD.
- Official LGU approval or clearance workflows.
- General LGU enterprise document management suites.
- Other official government or donor climate-governance tools.

Where those systems define submission, eligibility, assessment, certification, or approval, LCCAP SaaS supports **upstream preparation and organization** only.

The product language should avoid implying that LCCAP SaaS is:

- An official government platform.
- A mandated submission channel.
- A funding approval system.
- A national dashboard.
- A replacement for climate-risk diagnostics.
- A replacement for LGU policy guidance.
- A replacement for local legislative approval processes.

The product should be described as:

- A preparation workspace.
- A structured planning workspace.
- An internal LGU operating workspace.
- A draft-package generator.
- An accountability and organization tool.
- A complementary system of work for LCCAP preparation.

---

## MVP emphasis and non-goals

### MVP emphasis

- Organizing **LCCAP plan sections** and narrative structure.
- Organizing **supporting documents and evidence** in plan context.
- **Linking** documentation and references relevant to risk and hazard discussion as structured plan content.
- Defining climate actions for adaptation and mitigation.
- Tracking monitoring indicators and progress for implementation visibility.
- Preparing export-ready working outputs such as PDF drafts.
- Supporting edit/archive workflows for user correction.
- Preserving accountability through audit logs.
- Providing section revision history and restore capability.
- Protecting tenant data through server-side authorization and tenant scoping.
- Complementing government and donor workflows rather than replacing mandated portals or guidance.

### MVP non-goals

- Not an official national or agency **submission** channel for any government or donor program.
- Not a replacement for **NICCDIES** or other mandated national reporting systems.
- Not a replacement for **DILG**, **LGA**, or CCC **guidance, training, or policy interpretation**.
- Not a replacement for **PlanSmart**, **UNDP SHIELD**, or other **official risk diagnostic** or assessment platforms.
- Not a PSF or donor **application portal**.
- Not funding-system-specific eligibility processing.
- Not a **government approval**, certification, or clearance system.
- Not a general-purpose **LGU document management** or records-management platform.
- Not a **national climate dashboard** or cross-country situational picture.
- Not a **full GIS / spatial analytics** platform in MVP.
- Not a production BFF architecture.
- Not a final high-scale enterprise SaaS deployment without production hardening.

---

## Phase roadmap

**MVP** delivered the core LGU workspace foundation. **Phase 2** (now reflected in this codebase) deepened preparation workflows, exports, monitoring history, operational visibility, map-layer registration, and notifications/collaboration awareness. **Phase 3 exposure workflow is partially implemented** for facility-only exposure analysis (hazard layer registration → exposure analysis jobs → facility-only exposure summary persistence → frontend display), while broader Phase 3 GIS/metrics/AI remains future scope: interoperability, PostGIS / spatial analytics, area/population/risk scoring, scenario comparison, recommendation-style assistance, and broader AI/Python services.

Features must not **bleed across phases** without an explicit decision.

| Phase | Focus |
| --- | --- |
| **MVP** | Core LGU workspace foundation: plan sections, documents/evidence, action items, monitoring indicators, audit history, server-side RBAC, archive/restore, pagination, and export-ready draft PDF package. |
| **Phase 2** | Evidence index (JSON/CSV); document evidence links to sections and actions; section review comments; CCET / funding-readiness tagging; funding source and program catalogs; action funding allocations (preparation aid, **not** a PSF or funding portal); richer export package helpers (manifest and CSV working outputs); **monitoring update history**; **operational dashboard / activity feed**; **GeoJSON / map layer foundation**; **collaboration and notifications foundation** (in-app feed; events from workspace actions; read-only collaboration summary; groups tenant-admin/seed-managed). |
| **Phase 3** | Facility-only exposure workflow (hazard layer registration → exposure analysis jobs → persisted **ExposureSummary** rows + frontend display) with explicit `EPSG:4326` and `Polygon` / `MultiPolygon` hazard geometries; remaining Phase 3 scope includes **PostGIS** / spatial analytics, exposed area/population/risk metrics, scenario comparison, recommendation-style assistance, and broader AI / Python services. |

### Phase 2: Collaboration and notifications (boundary)

- The **Collaboration** summary in the workspace is **read-only** in Phase 2: it reflects configured groups and members for awareness only.
- **Collaboration groups and membership** are populated through **tenant administrator–managed configuration or seed data**, not through in-app self-service.
- Phase 2 does **not** include self-service group CRUD, user invitations, email delivery, WebSocket or real-time co-editing, or per-user notification channel preferences.
- **Later phases** (including admin-oriented work) may add group management, invitations, role-based notification routing, preference surfaces, and deeper collaboration workflows.

---

## Product Overview

LCCAP SaaS is designed as a **plan-centric, LGU-facing climate planning workspace**.

At the center of the system is the **Plan**, which acts as the aggregate root for the LCCAP workspace.

```text
Account / LGU
  └── Plan
        ├── Plan Sections
        ├── Section History from Audit Logs
        ├── Documents
        ├── File Assets
        ├── Action Items
        ├── Monitoring Indicators
        ├── Export Jobs
        ├── Audit Logs
        ├── Monitoring updates, evidence index, comments, funding readiness, richer exports
        ├── Operational dashboard, map workspace layers, notifications (Phase 2)
        ├── HazardLayer + ExposureAnalysisJobs + ExposureSummaries (Phase 3 exposure workflow, facility-only)
        └── Future broader AI / advanced spatial analytics (Phase 3 remaining scope)
```

This architecture enables the system to organize climate planning work around a clear, auditable structure rather than treating LCCAP as a single static file.

The updated **MVP plus Phase 2** workspace supports:

- Workspace creation.
- Workspace editing.
- Workspace correction.
- Soft archive instead of hard deletion.
- Version-aware section restoration.
- Audit visibility for authorized users.
- Paginated list navigation.
- Secure tenant boundaries.
- Role-based operation control.
- Draft export generation.
- Evidence index and document evidence linking.
- Section review comments.
- CCET / funding readiness surfaces, catalogs, and planned action funding allocations (preparation only).
- Monitoring update history.
- Export package manifest and CSV working outputs.
- Operational dashboard / activity views.
- Map workspace / GeoJSON layer registration (foundation).
- In-app notifications and read-only collaboration summary (admin/seed-managed groups).

---

## Target Users

### LGU-Level Users

- LGU Admin.
- Climate Planner.
- Technical Staff.
- Department Reviewer.
- Monitoring Officer.
- Viewer / Read-only Stakeholder.

### Platform-Level Users

- Platform Administrator.
- Technical Support Operator.
- Product / operations roles for tenant onboarding and configuration where deployed.

Enterprise deployments may define additional **internal** roles. MVP does **not** imply a national oversight or submission-review role for this product.

### External / Future Users

- Citizens.
- NGOs.
- Development partners.
- Funding agencies.
- Public portal viewers.

External/public users are future-phase concepts and are not part of the current MVP workspace flow.

---

## Current MVP + Phase 2 + Phase 3 Exposure Status

The updated **MVP plus Phase 2** scope is **locally end-to-end validated** for the core LGU workspace flow, the Phase 2 preparation modules listed below, and the major enterprise-style hardening layers.

### Phase 2 closure status

| Phase 2 module | Status |
| --- | --- |
| Monitoring update history | Complete |
| Evidence index and evidence links | Complete |
| Section review comments | Complete |
| Funding readiness and CCET catalogs | Complete |
| Funding allocation foundation | Complete |
| Richer export packages (manifest / CSV working outputs) | Complete |
| Operational dashboard / activity feed | Complete |
| GeoJSON / map layer foundation | Complete |
| Collaboration / notifications foundation | Complete |

### Phase 3 exposure workflow status (facility-only)

| Phase 3 exposure module | Status |
| --- | --- |
| Hazard layer registration (GeoJSON-based) | Complete |
| Exposure analysis job queue + processing | Complete |
| Python FastAPI facility-only exposure computation (Polygon/MultiPolygon, `EPSG:4326`, boundary-inclusive point-in-polygon) | Complete (feature-flagged) |
| .NET Python adapter and feature flag (`PythonAi:Enabled`) | Complete |
| ExposureSummary persistence + replace-for-job semantics | Complete |
| Completed job state + `output_json` engine/diagnostics/persistence metadata | Complete |
| Frontend exposure readiness panel + completed/failed/zero-summary messaging | Complete |
| Manual E2E verification document + run checklist | Complete (manual verification required per demo dataset) |

Validated flow (MVP core):

```text
Login
  -> Refresh-token-backed session
  -> View paginated tenant plans
  -> Open real plan workspace
  -> Edit and archive plan metadata
  -> Edit and save LCCAP section content
  -> View section revision history
  -> Restore a previous section version
  -> Upload and list supporting documents
  -> Edit and archive document records
  -> Create, edit, and archive action items
  -> Create, edit, and archive monitoring indicators
  -> Generate PDF draft package
  -> Download generated PDF
  -> Review accountability history through Audit Viewer
```

Phase 2 extensions exercised in the same workspace (local validation):

```text
  -> Add monitoring updates and view indicator update history
  -> Link documents to section/action evidence fields
  -> View evidence index (JSON) and download evidence index CSV
  -> Create, resolve, reopen, and archive section review comments
  -> View CCET and funding-readiness surfaces; use funding source/program catalogs
  -> Create and archive planned action funding allocations (preparation aid; not a portal)
  -> Download export package manifest and working CSV exports (actions, monitoring, funding readiness)
  -> View operational dashboard / activity feed for the plan
  -> Register a GeoJSON layer and inspect map workspace metadata
  -> View in-app notifications (events from workspace actions) and read-only collaboration summary
```

### Completed Backend Slices

| Module | Status |
| --- | --- |
| Solution structure | Complete |
| PostgreSQL baseline schema | Complete |
| Additive refresh token migration | Complete |
| Clean Architecture project setup | Complete |
| Domain base entities | Complete |
| EF Core DbContext and mappings | Complete |
| Auth / JWT bearer authentication | Complete |
| Refresh-token persistence foundation | Complete |
| Refresh token rotation / logout / me endpoints | Complete |
| Current user / tenant context | Complete |
| Development CORS configuration | Complete |
| Development-only demo seed service | Complete |
| Plans API | Complete |
| Tenant-scoped paginated plans list API | Complete |
| Default plan section seeding | Complete |
| Plan metadata edit/archive/audit | Complete |
| Plan Sections API | Complete |
| Section revision history and restore | Complete |
| Documents API | Complete |
| Document metadata edit/archive/audit | Complete |
| Local File Storage Service | Complete |
| Monitoring API | Complete |
| Monitoring indicator edit/archive/audit | Complete |
| **Monitoring update history API** | **Complete** |
| **Evidence index JSON/CSV APIs** | **Complete** |
| **Section comments API** | **Complete** |
| Action Items API | Complete |
| Action item edit/archive/audit | Complete |
| **Funding / CCET APIs (tags, sources, programs, allocations)** | **Complete** |
| **Funding source/program catalog APIs** | **Complete** |
| **Action funding allocation APIs** | **Complete** |
| Export Job / PDF generation | Complete |
| Export download endpoint | Complete |
| **Export package manifest and CSV endpoints** | **Complete** |
| **Operational dashboard endpoint** | **Complete** |
| **Map workspace / GeoJSON layer APIs** | **Complete** |
| **Notifications API (feed, mark read, mark all read)** | **Complete** |
| **Collaboration summary API** | **Complete** |
| **Notification event wiring from workspace actions** | **Complete** |
| Server-side RBAC enforcement | Complete |
| Audit log viewer API | Complete |
| Optimistic concurrency / rowVersion hardening | Complete |
| Paginated workspace list APIs | Complete |
| Mapping tests for key entities | Complete |
| API controller tests | Complete |
| Full .NET solution tests | Complete |
| HazardLayer registration | Complete |
| ExposureAnalysisJobs API + processing | Complete |
| ExposureSummaries read APIs | Complete |
| Python exposure-computation contract/client/adapter (facility-only) | Complete |
| PythonAi feature flag wiring (optional adapter) | Complete |
| ExposureSummary persistence service (transaction boundary) | Complete |
| Replace-for-job semantics (archive+insert) | Complete |
| `output_json` engine/diagnostics/persistence metadata | Complete |
| Completed job state handling (Queued → Completed/Failed) | Complete |
| Manual Phase 3 exposure verification document | Complete |
| Phase 3 exposure workflow controller tests | Complete |

### Completed Frontend Slices

| Module | Status |
| --- | --- |
| Next.js frontend foundation | Complete |
| Responsive enterprise UI shell | Complete |
| Tailwind CSS setup | Complete |
| Local shadcn-style UI primitives | Complete |
| Login UI and auth session handling | Complete |
| Memory-only access token session handling | Complete |
| Refresh-on-reload auth restoration | Complete |
| API client and typed HTTP layer | Complete |
| One-time 401 refresh retry | Complete |
| Dashboard preview | Complete |
| Existing plans list | Complete |
| Paginated plans navigation | Complete |
| Create plan UI | Complete |
| Plan workspace UI | Complete |
| Plan metadata edit/archive UI | Complete |
| Plan sections editor | Complete |
| Section revision history / restore UI | Complete |
| Documents upload/list UI | Complete |
| Document edit/archive UI | Complete |
| Paginated document list navigation | Complete |
| **Evidence link dropdown UX (section/action)** | **Complete** |
| **Evidence index panel** | **Complete** |
| **Section comments panel** | **Complete** |
| Action items UI | Complete |
| Action item edit/archive UI | Complete |
| Paginated action list navigation | Complete |
| **CCET catalog / funding readiness panel** | **Complete** |
| **Funding allocation UI** | **Complete** |
| **Funding source/program dropdown UX** | **Complete** |
| Monitoring indicators UI | Complete |
| Monitoring indicator edit/archive UI | Complete |
| Paginated monitoring list navigation | Complete |
| **Monitoring update form/history** | **Complete** |
| PDF export/download UI | Complete |
| **Export package panel (manifest / CSV downloads)** | **Complete** |
| **Operational dashboard / activity feed panel** | **Complete** |
| **Map workspace metadata panel** | **Complete** |
| **Notification center panel** | **Complete** |
| **Collaboration summary panel** | **Complete** |
| Audit history viewer UI | Complete |
| RBAC-aware sidebar/actions | Complete |
| Production-like `npm start` flow | Complete |
| Frontend type-check and lint validation | Complete |
| Exposure readiness panel | Complete |
| Hazard layer registration UI | Complete |
| Exposure job queue/process UI | Complete |
| Exposure summaries read-only display | Complete |
| Completed/Failed/zero-summary messaging | Complete |
| Refresh jobs and summaries after processing | Complete |

### Remaining MVP Polish Items

| Module | Status |
| --- | --- |
| Swagger/OpenAPI polish | Planned |
| UI polish and screenshot capture | Planned |
| Optional cleanup of local test data | Planned |
| Broader E2E regression checklist or automated smoke script | Planned |
| CI quality gate | Planned |
| Production deployment hardening checklist | Planned |

---

## Core Modules

### 1. Authentication and Tenant Context

Authentication is implemented using JWT bearer authentication plus refresh-token-backed session hardening.

Key capabilities:

- Login endpoint.
- Refresh endpoint.
- Logout endpoint.
- Current user endpoint.
- Secure password hashing.
- JWT token generation.
- JWT access tokens held in frontend memory only.
- HttpOnly refresh-token cookie.
- Refresh-token hashes stored in PostgreSQL.
- Refresh-token rotation on refresh.
- Refresh-token family revocation on misuse where supported.
- Logout revokes refresh token and clears cookie.
- Refresh-on-reload restores session from HttpOnly cookie.
- Standard Microsoft JWT bearer middleware.
- `account_id` claim for tenant isolation.
- `role` claim for RBAC.
- Current user context used by commands and queries.
- No public `accountId` accepted from request bodies, routes, or query strings.
- Development-only seeded demo users for local testing.

Current API surface:

```text
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/logout
GET  /api/auth/me
```

Tenant isolation is enforced through the authenticated current user context.

Session model:

```text
Access token:
  - short-lived JWT
  - stored in frontend memory only
  - used as Bearer token for API requests

Refresh token:
  - stored as HttpOnly cookie
  - hashed in PostgreSQL
  - rotated on refresh
  - revoked on logout
```

---

### 2. Plans

Plans are the root LCCAP workspace entity.

Capabilities:

- Create plan.
- Update plan metadata.
- Archive plan through soft delete.
- List plans for the authenticated tenant.
- Paginated tenant-scoped plan list.
- Get plan by ID.
- Tenant-scoped access.
- Default plan sections seeded after plan creation.
- Validation for title, years, status, and template mode.
- Audit old/new metadata changes.
- Optimistic concurrency with rowVersion.
- Legacy empty rowVersion repair where supported.

Current API surface:

```text
GET    /api/plans?page=1&pageSize=25
POST   /api/plans
PUT    /api/plans/{planId}
GET    /api/plans/{planId}
DELETE /api/plans/{planId}
```

Archive behavior:

```text
Plan archive = soft delete + archived state
No hard delete
No child records are physically removed
Audit log records the archive event
```

---

### 3. Plan Sections

Plan sections represent editable sections of the LCCAP document.

Default sections are created automatically when a plan is created:

| Order | Section Key | Title |
| --- | --- | --- |
| 10 | executive_summary | Executive Summary |
| 20 | introduction | Introduction and LGU Profile |
| 30 | climate_risk_assessment | Climate and Disaster Risk Assessment |
| 40 | adaptation_actions | Adaptation Actions |
| 50 | mitigation_actions | Mitigation Actions |
| 60 | implementation_plan | Implementation Plan |
| 70 | monitoring_evaluation | Monitoring and Evaluation |
| 80 | references_annexes | References and Annexes |

Capabilities:

- List plan sections.
- Get a section by key.
- Save section content.
- Skip no-op saves to reduce audit noise.
- Record section update audit history.
- View revision history from audit snapshots.
- Restore a previous section version.
- Tenant-scoped access.
- Audit old/new content snapshots.

Current API surface:

```text
GET  /api/plans/{planId}/sections
GET  /api/plans/{planId}/sections/{sectionKey}
PUT  /api/plans/{planId}/sections/{sectionKey}
GET  /api/plans/{planId}/sections/{sectionKey}/history
POST /api/plans/{planId}/sections/{sectionKey}/restore
GET  /api/plans/{planId}/sections/{sectionKey}/comments
POST /api/plans/{planId}/sections/{sectionKey}/comments
POST /api/section-comments/{commentId}/resolve
POST /api/section-comments/{commentId}/reopen
DELETE /api/section-comments/{commentId}
```

Restore behavior:

```text
Section restore uses audit snapshots
Restore writes a new audit record
Restore does not delete history
```

---

### 4. Documents and File Assets

The system separates logical document usage from physical file storage.

```text
Document = logical document record attached to a plan
FileAsset = physical file metadata and storage reference
```

Capabilities:

- Upload document.
- List documents by plan.
- Paginated document list.
- Edit document metadata (including optional **plan section** and **action item** evidence links).
- Archive document records through soft delete.
- Audit metadata updates and archive actions.
- Tenant-scoped document visibility.
- Local file storage.
- SHA256 hashing.
- Safe generated stored filenames.
- Extension validation.
- Path traversal protection.
- Frontend-safe rendering without exposing stored server paths.

Current API surface:

```text
POST   /api/documents/upload
GET    /api/plans/{planId}/documents?page=1&pageSize=25
GET    /api/plans/{planId}/documents/evidence-index
GET    /api/plans/{planId}/documents/evidence-index.csv
PUT    /api/documents/{documentId}/metadata
DELETE /api/documents/{documentId}
```

Document archive behavior:

```text
Document archive = soft delete of logical document record
FileAsset is retained
Audit log records the archive event
Stored file path is not exposed to frontend
```

---

### 5. Local File Storage

The MVP includes a local file storage abstraction.

Capabilities:

- Save uploaded file stream.
- Generate tenant-scoped storage path.
- Open file stream for download.
- Delete file where supported.
- Reject path traversal.
- Reject empty streams.
- Enforce max upload size.
- Enforce allowed file extensions.
- Compute SHA256 hash.

Storage path format:

```text
uploads/{accountId}/{yyyy}/{MM}/{generatedGuid}{extension}
```

This is implemented behind `IFileStorageService` so future cloud storage providers can be introduced without changing application logic.

Future storage providers may include:

- Azure Blob Storage.
- AWS S3.
- Google Cloud Storage.
- Private object storage.

---

### 6. Action Items

Action Items represent structured climate interventions.

Capabilities:

- Create action item.
- Update action item.
- Archive action item.
- List action items by plan.
- Paginated action list.
- Get action item by ID.
- Validate action type, sector, status, budget, and timeline.
- Tenant-scoped reads and writes.
- Audit old/new action snapshots.
- Optimistic concurrency with rowVersion.

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
POST   /api/plans/{planId}/actions
GET    /api/plans/{planId}/actions?page=1&pageSize=25
GET    /api/actions/{actionItemId}
PUT    /api/actions/{actionItemId}
DELETE /api/actions/{actionItemId}
```

Action archive behavior:

```text
Action archive = soft delete
Admin-only archive operation
Audit log records old/new archive state
```

---

### 7. Monitoring

Monitoring tracks climate action progress through indicators.

Capabilities:

- Create monitoring indicator.
- Update monitoring indicator.
- Archive monitoring indicator.
- List indicators by plan.
- Paginated indicator list.
- Tenant-scoped access.
- Validation for indicator name, status, and progress values.
- Audit old/new indicator snapshots.
- Optimistic concurrency with rowVersion.
- Record **monitoring updates** (history) per indicator with dated notes/metrics.

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
POST   /api/monitoring/indicators
GET    /api/monitoring/plans/{planId}/indicators?page=1&pageSize=25
PUT    /api/monitoring/indicators/{indicatorId}
DELETE /api/monitoring/indicators/{indicatorId}
POST   /api/monitoring/indicators/{indicatorId}/updates
GET    /api/monitoring/indicators/{indicatorId}/updates
```

Monitoring archive behavior:

```text
Monitoring indicator archive = soft delete
Admin-only archive operation
Audit log records archive state
```

---

### 8. Export Jobs

Export jobs generate LCCAP outputs from structured plan data.

Capabilities:

- Create PDF export job.
- Generate minimal valid PDF from plan and section data.
- Store generated PDF as FileAsset.
- Link export job to FileAsset.
- Download completed export safely.
- Prevent cross-tenant downloads.
- Return conflict for incomplete exports.
- Use export status to determine readiness.
- Download **export package manifest** and **working CSV** outputs (actions matrix, monitoring matrix, funding readiness summary) as preparation aids—not official submission packages.

Current API surface:

```text
POST /api/plans/{planId}/exports/pdf
GET  /api/exports/{exportJobId}
GET  /api/exports/{exportJobId}/download
GET  /api/plans/{planId}/exports/package-manifest
GET  /api/plans/{planId}/exports/action-matrix.csv
GET  /api/plans/{planId}/exports/monitoring-matrix.csv
GET  /api/plans/{planId}/exports/funding-readiness.csv
```

Export behavior:

```text
Export output = working draft package
Export output is not official submission
Export download checks tenant ownership
Completed export must have a FileAsset
```

---

### 9. Audit History

Audit History provides tenant-scoped accountability for LGU workspace changes.

Capabilities:

- View audit logs for the authenticated tenant.
- Filter by entity.
- Filter by action.
- Filter by user.
- Filter by plan.
- Filter by date range.
- Review old values.
- Review new values.
- Review metadata snapshots.
- Support Admin and Reviewer access.
- Block Planner and Viewer access.
- Read-only audit history.
- No mutation endpoints.

Current API surface:

```text
GET /api/audit-logs?page=1&pageSize=25
```

Common audit actions:

```text
PlanMetadataUpdated
PlanArchived
PlanSectionUpdated
PlanSectionRestored
DocumentMetadataUpdated
DocumentArchived
ActionItemUpdated
ActionItemArchived
MonitoringIndicatorUpdated
MonitoringIndicatorArchived
ExportJobCreated
ExportJobCompleted
```

Audit logs are not a replacement for official government record-keeping systems. They are internal accountability records for the LCCAP workspace.

---

### 10. Server-Side RBAC

Workspace operations are protected by server-side role checks.

RBAC is not only a UI feature. Controllers and application flows enforce role-based authorization.

MVP role behavior:

| Role | Read | Create/Edit | Restore | Export | Archive | Audit Viewer |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Admin | Yes | Yes | Yes | Yes | Yes | Yes |
| Reviewer | Yes | No | No | Yes | No | Yes |
| Planner | Yes | Yes | Yes | Yes | No | No |
| Viewer | Yes | No | No | No | No | No |
| PublicViewer | No for tenant workspace APIs | No | No | No | No | No |

Authorization behavior:

```text
Not logged in -> 401 Unauthorized
Logged in but wrong role -> 403 Forbidden
Wrong tenant resource -> 404 or existing tenant-safe result pattern
```

The frontend may hide controls for convenience, but the backend remains the authority.

---

### 11. Optimistic Concurrency and RowVersion

The MVP uses rowVersion tokens to prevent accidental overwrites in editable records.

Capabilities:

- Detect stale updates.
- Return conflict responses for stale rowVersion.
- Rotate rowVersion after successful updates.
- Repair legacy empty rowVersion values where supported.
- Show refresh-required messages in the frontend.
- Protect editable records from silent last-write-wins behavior.

RowVersion is used for:

- Plans.
- Action items.
- Monitoring indicators.
- Documents where supported by backend contract.
- Plan sections where supported by update flow.

Frontend behavior:

```text
If rowVersion is missing:
  show refresh-required message

If rowVersion is stale:
  show conflict message
  user should refresh and retry
```

---

### 12. Pagination

Workspace list endpoints use pagination to avoid unbounded tenant responses.

Paginated resources include:

- Plans.
- Documents.
- Action items.
- Monitoring indicators.
- Audit logs.

Phase 2 adds other tenant-scoped list/feed surfaces (pagination shape varies by endpoint), such as **section comment** listings and the **in-app notifications** feed.

Standard query shape:

```text
?page=1&pageSize=25
```

Standard response shape:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 25,
  "totalCount": 0
}
```

Pagination rules:

- Page defaults to 1.
- Page size defaults to 25.
- Page size is clamped to a maximum of 100.
- Archived/deleted records are excluded from active lists.
- Tenant isolation remains enforced before records are returned.

---

### 13. Evidence index and evidence links

Capabilities:

- Plan-scoped **evidence index** (JSON) consolidating document metadata, linked section/action context, and tags.
- **CSV export** of the same index for offline review (working output; not an official report).
- Document metadata supports optional links to a **plan section** and/or **action item** for evidence traceability.

Current API surface (representative):

```text
GET /api/plans/{planId}/documents/evidence-index
GET /api/plans/{planId}/documents/evidence-index.csv
PUT /api/documents/{documentId}/metadata   (optional planSectionId, actionItemId, evidence fields)
```

---

### 14. Section review comments

Capabilities:

- Threaded-style **review comments** on plan sections (create, list, resolve, reopen, archive).
- Tenant isolation and workspace RBAC apply; comments are preparation workflow only (not a government approval queue).

Current API surface (representative):

```text
GET    /api/plans/{planId}/sections/{sectionKey}/comments
POST   /api/plans/{planId}/sections/{sectionKey}/comments
POST   /api/section-comments/{commentId}/resolve
POST   /api/section-comments/{commentId}/reopen
DELETE /api/section-comments/{commentId}
```

---

### 15. Funding readiness and catalogs

Capabilities:

- **Climate expenditure (CCET-style) tags** for tagging actions and readiness views.
- **Funding source** and **funding program** catalogs for structured dropdowns (not raw identifier entry).
- **Funding readiness** panels and CSV exports summarize preparation signals only—**not** PSF eligibility, submission, or funding approval.

Current API surface (representative):

```text
GET /api/funding/climate-expenditure-tags
GET /api/funding/sources
GET /api/funding/programs
GET /api/plans/{planId}/exports/funding-readiness.csv
```

---

### 16. Action funding allocations

Capabilities:

- Record **planned** funding allocations against actions using cataloged sources/programs.
- List allocations by plan or by action; archive allocations through soft-delete patterns where supported.

Current API surface (representative):

```text
GET    /api/plans/{planId}/funding-allocations
GET    /api/actions/{actionItemId}/funding-allocations
POST   /api/plans/{planId}/funding-allocations
DELETE /api/funding-allocations/{allocationId}
```

---

### 17. Operational dashboard

Capabilities:

- Plan-scoped **operational dashboard** data summarizing recent activity and workspace signals for pilot demos.
- Read-oriented; not a national operations command center.

Current API surface (representative):

```text
GET /api/plans/{planId}/operational-dashboard
```

---

### 18. Map workspace foundation

Capabilities:

- **Map workspace** metadata per plan and registration of **GeoJSON** layer assets.
- Feature read and map asset archive flows for the foundation layer—**not** PostGIS exposure analysis, dynamic GIS tools, or mandated spatial compliance outputs.

Current API surface (representative):

```text
GET    /api/plans/{planId}/map-workspace
POST   /api/plans/{planId}/geojson-layers
GET    /api/map-assets/{mapAssetId}/features
DELETE /api/map-assets/{mapAssetId}
```

---

### 19. Collaboration and notifications

Capabilities:

- **In-app notification** feed with mark-read and mark-all-read; **notification events** are created from real workspace actions (best-effort, non-blocking to primary operations).
- **Collaboration summary** is **read-only** in Phase 2 and reflects **tenant-administrator or seed-configured** groups and members—no self-service group CRUD, invitations, email delivery, WebSocket/real-time feeds, or end-user notification preference management.

Current API surface (representative):

```text
GET  /api/notifications
POST /api/notifications/{notificationId}/read
POST /api/notifications/read-all
GET  /api/collaboration/summary
```

---

### 20. Phase 3 Exposure Analysis Workflow

This workflow is intentionally limited to **facility-only exposure analysis** (critical facility points evaluated against hazard geometries). It is designed as preparation support for LGU workflows, not as an official or mandated GIS analytics product.

#### Purpose and limitations
- Facility-only exposure computation (no exposed area/population/risk metric calculations).
- Explicit CRS: `EPSG:4326` only (no transformation).
- Hazard geometries supported: `Polygon`, `MultiPolygon` only.
- Boundary-inclusive point-in-polygon containment is used (a point on the hazard boundary counts as exposed).
- Python does **not** access the database; .NET owns persistence and job state.

#### Current API surfaces
Exposure analysis jobs:
```text
GET  /api/plans/{planId}/exposure-analysis-jobs
POST /api/plans/{planId}/exposure-analysis-jobs
POST /api/plans/{planId}/exposure-analysis-jobs/{jobId}/process
```
Exposure summaries:
```text
GET  /api/plans/{planId}/exposure-summaries
GET  /api/plans/{planId}/exposure-analysis-jobs/{jobId}/exposure-summaries
GET  /api/plans/{planId}/exposure-summaries/{summaryId}
```

#### Job lifecycle (behavior)
- Job starts in `Queued`, transitions to `Running` when processing begins, and ends as `Completed` or `Failed`.
- Completed jobs persist validated facility-only `ExposureSummary` rows.
- Replace-for-job semantics: rerunning a job refreshes results by archiving old summaries for that job and inserting fresh ones.
- Completed jobs store `output_json` with engine/diagnostics/persistence metadata, including persisted summary counts.

#### Feature flag: optional Python adapter
- The .NET layer can use a Python FastAPI computation service when `PythonAi:Enabled=true`.
- When disabled, the application remains compatible with the broader computation contract, but facility exposure computation is not performed via the Python adapter.

#### Frontend behavior
- Hazard layer registration and exposure readiness UI drive the flow: register hazard layer → queue an exposure job → process a queued job.
- The frontend displays:
  - job status transitions (Queued/Processing/Completed/Failed),
  - completed-zero messaging when a successful computation yields zero stored summaries,
  - and exposure summary cards when results are persisted.

#### Non-goals / not implemented here
- Full PostGIS exposure analytics.
- Barangay polygon intersection logic.
- Exposed area calculation, exposed population approximation, or risk score calculation.
- Scenario comparison, recommendation engine outputs, or broad AI/RAG workflows.

---
## Completed MVP Capabilities

The current MVP implements and validates the major LGU workspace workflow:

```text
Authenticate with refresh-token-backed session
  -> Restore session after reload through HttpOnly refresh cookie
  -> View paginated existing plans
  -> Create plan
  -> Auto-create default sections
  -> Edit and archive plan metadata
  -> Edit sections
  -> View and restore section history
  -> Upload, list, edit, and archive documents
  -> Add, edit, and archive action items
  -> Add, edit, and archive monitoring indicators
  -> Generate and download PDF export
  -> Review audit history as Admin or Reviewer
```

**Phase 2** (same workspace, locally validated) additionally covers monitoring update history, evidence index and links, section review comments, funding readiness/catalog/allocation preparation flows, richer CSV/manifest export helpers, operational dashboard views, map layer registration, and the notifications/collaboration awareness surfaces described in the Phase 2 closure table—always as **draft/preparation outputs**, not official submission or approval systems.

This is the core LCCAP planning loop plus the enterprise-style operating controls needed for correction, accountability, tenant isolation, and role-based access.

---

## In Progress / Planned Capabilities

### MVP and Phase 2 remaining polish

- Swagger/OpenAPI polish.
- UI polish and screenshot capture.
- Optional local test-data cleanup.
- Broader E2E regression checklist or automated smoke script.
- Production deployment hardening checklist.
- CI quality gate for backend tests and frontend checks.
- Structured logging and request correlation.
- Rate limiting for auth and uploads.
- Audit retention or compaction strategy.

### Phase 3 and longer-term roadmap
- Phase 3 facility-only exposure workflow is partially implemented (facility-only Python FastAPI exposure computation + .NET job processing/persistence + frontend exposure summaries display), limited to explicit `EPSG:4326` and facility points evaluated against registered `Polygon` / `MultiPolygon` hazard geometries (boundary-inclusive point-in-polygon).
- Remaining Phase 3 roadmap items include PostGIS/spatial analytics, exposed area/population/risk metrics, scenario comparison, recommendation engine outputs, broader AI/RAG workflows, and production hardening.
- Interoperability with external systems.
- Integration-ready APIs and exports.
- Advanced observability.
- Operational runbooks.
- Production BFF or cookie-auth architecture if required.

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

Optional Phase 3 computation service (facility-only; feature-flagged):

```text
┌────────────────────────────────────┐
│   Python FastAPI Exposure Compute  │
│  Facility-only point-in-polygon    │
│  (explicit EPSG:4326; no DB access)│
└────────────────────────────────────┘
```

Future / planned AI layer (roadmap; not part of the current application build):

```text
┌────────────────────────────────────┐
│          Python FastAPI AI          │
│  Summarization / RAG / Analysis    │
└────────────────────────────────────┘
```

Architecture principles:

- Domain entities do not depend on HTTP or EF-specific infrastructure.
- Application layer depends on abstractions.
- Infrastructure implements persistence and storage.
- API layer maps HTTP requests to application commands and queries.
- Frontend uses typed clients and defensive parsers.
- Tenant scoping is server-side.
- Authorization is server-side.
- Audit logging is system-level accountability.

---

## Technology Stack

### Backend

- .NET 8.
- ASP.NET Core Web API.
- Clean Architecture.
- Entity Framework Core.
- PostgreSQL.
- Npgsql.
- JWT bearer authentication.
- HttpOnly refresh-token cookies.
- Refresh-token rotation.
- PostgreSQL-backed refresh token hashes.
- Local file storage abstraction.

### Frontend

- Next.js.
- TypeScript.
- Tailwind CSS.
- Local shadcn-style UI primitives.
- Responsive dashboard shell.
- Mobile-friendly layouts.
- Typed API clients and defensive response parsing.
- Memory-only access-token session handling.
- Refresh-on-reload session restoration.
- One-time 401 refresh retry.

### Phase 3 Exposure Computation (Implemented)

- Python FastAPI exposure-computation service (facility-only point-in-polygon)
- Shapely geometry evaluation
- pytest (local validation for the exposure compute service)

### AI Planned

- Async AI jobs.
- Document intelligence.
- Summarization.
- RAG search over LCCAP documents and plan data.
- Recommendation engine.

### Testing

- xUnit.
- EF model mapping tests.
- API/controller tests.
- Storage service tests.
- Authentication tests.
- Refresh-token tests.
- RBAC tests.
- Audit viewer tests.
- Optimistic concurrency tests.
- Pagination tests.
- Frontend type-checking.
- Frontend linting.
- Production frontend build validation for release checkpoints.
- Targeted build/test validation per slice.

---

## Multi-Tenancy and Security

LCCAP SaaS is designed around strict tenant isolation.

Core rule:

```text
account_id = tenant boundary
```

Security principles:

- No cross-tenant reads.
- No cross-tenant writes.
- No public `accountId` accepted from requests.
- Tenant context comes from authenticated JWT claims.
- Commands and queries enforce account scope.
- Export downloads return 404 for cross-tenant access.
- File storage paths are tenant-scoped.
- Stored server paths are not exposed in API responses.
- Development demo seed is disabled by default and must be explicitly enabled.
- Refresh tokens are stored in HttpOnly cookies and hashed in PostgreSQL.
- Access tokens are held in frontend memory only, not localStorage.
- Sessions are restored after reload through the refresh endpoint.
- 401 responses trigger a single refresh retry before forcing re-login.
- Server-side RBAC protects workspace operations.
- Archive actions use soft-delete behavior.
- Audit logs preserve old/new/metadata accountability snapshots.
- Production deployments should still add CSRF strategy, CSP, rate limiting, structured logging, stricter cookie/CORS policies, and monitoring.

### Refresh-token security model

```text
Raw refresh token:
  - generated server-side
  - sent only as HttpOnly cookie
  - never stored in database
  - never stored in localStorage

Refresh token hash:
  - stored in PostgreSQL
  - matched during refresh
  - rotated on refresh
  - revoked on logout
```

### Access-token security model

```text
Access token:
  - JWT bearer token
  - returned by login/refresh
  - held in frontend memory
  - not persisted to localStorage
  - restored after reload through refresh cookie
```

---

## Server-Side RBAC Matrix

| Role | Read Plans | Edit Records | Restore Sections | Export | Archive | Audit Viewer |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Admin | Yes | Yes | Yes | Yes | Yes | Yes |
| Reviewer | Yes | No | No | Yes | No | Yes |
| Planner | Yes | Yes | Yes | Yes | No | No |
| Viewer | Yes | No | No | No | No | No |
| PublicViewer | No for tenant workspace APIs | No | No | No | No | No |

Role policy notes:

- Admin has full tenant workspace control.
- Reviewer can read and export, and can view audit history.
- Planner can create and edit workspace records, restore sections, and export, but cannot archive.
- Viewer is read-only.
- PublicViewer is not part of authenticated tenant workspace operations.
- Platform roles are not automatically granted tenant mutation access in the MVP.
- UI hiding is convenience only.
- Backend authorization is the source of truth.

---

## Data Model Overview

Core entities:

```text
Account
User
RefreshToken
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
SectionComment
ClimateExpenditureTag
FundingSource
FundingProgram
ActionFundingAllocation
Barangay
CriticalFacility
MapAsset
MapAnnotation
GeoJsonLayerFeature
HazardLayer
ExposureAnalysisJob
ExposureSummary
NotificationEvent
UserNotification
NotificationTemplate
CollaborationGroup
CollaborationGroupMember
```

Future / Phase 3-oriented entities (not implemented as first-class product surfaces here):

```text
AiJob
```

### Core entity responsibilities

| Entity | Responsibility |
| --- | --- |
| Account | Tenant boundary for LGU workspace data. |
| User | Authenticated account user with role and scope. |
| RefreshToken | Hashed refresh-token session record. |
| Plan | Root LCCAP workspace aggregate. |
| PlanSection | Editable plan document section. |
| Document | Logical document attached to a plan. |
| FileAsset | Physical file metadata and storage pointer. |
| ActionItem | Structured climate intervention. |
| MonitoringIndicator | Climate action progress indicator. |
| MonitoringUpdate | **Indicator update history** (dated entries / progress notes). |
| SectionComment | **Section review comment** for preparation workflows (not an approval system). |
| ClimateExpenditureTag | **CCET-style** expenditure tag for readiness views. |
| FundingSource | Tenant-scoped **funding source** catalog entry. |
| FundingProgram | Tenant-scoped **funding program** catalog entry. |
| ActionFundingAllocation | **Planned** funding allocation against an action (preparation; not portal submission). |
| Barangay / CriticalFacility | Baseline geographic/facility reference records used by readiness and map foundations. |
| MapAsset / MapAnnotation / GeoJsonLayerFeature | **Map workspace foundation** assets and features (not PostGIS analytics). |
| HazardLayer | Registered hazard layer linked to a GeoJSON map asset with hazard type/severity used for facility-only exposure computation. |
| NotificationEvent | Immutable **notification event** record from workspace activity. |
| UserNotification | Per-user **in-app notification** inbox row. |
| NotificationTemplate | Templates for fan-out when creating user notifications. |
| CollaborationGroup / CollaborationGroupMember | **Read-only collaboration summary** support; Phase 2 population is **tenant-admin or seed-managed** (no self-service CRUD in-product). |
| ExportJob | PDF/package export job status and file link. |
| AuditLog | Accountability record for changes and restores. |
| ExposureAnalysisJob | Queued/running/completed/failed exposure job storing input/output JSON and error state (facility-only exposure computation orchestration). |
| ExposureSummary | Persisted result row from validated facility-only computation; currently stores exposed facility counts and deferred metrics as null (area/population/risk). |
| Role / Permission / UserRole | Future richer role/permission model support. |

---

## AI and Intelligence Roadmap

AI is intentionally designed as an asynchronous support layer, not inline application logic.

Implemented non-AI computation note:
The Phase 3 facility-only exposure workflow is computed via a Python FastAPI service (Shapely point-in-polygon with boundary-inclusive containment) and orchestrated by .NET job processing/persistence. This facility-only workflow is not AI decision-making, and the AI roadmap below covers broader drafting/RAG/recommendation-style assistance and future analytics beyond facility-only computation.

Planned AI capabilities:

- Draft plan sections.
- Summarize uploaded documents.
- Extract document metadata.
- Recommend adaptation and mitigation actions.
- Generate evidence-linked insights.
- Support RAG over LCCAP documents and plan data.
- Support future exposure analysis and prioritization workflows beyond the current facility-only point-in-polygon exposure computation.

AI outputs should be:

- Explainable.
- Auditable.
- Linked to source data.
- Stored as jobs/results.
- Reviewed by humans before adoption.

AI should not:

- Replace official policy interpretation.
- Replace mandated diagnostics.
- Automatically submit plans.
- Automatically approve actions.
- Produce unreviewed official documents.

---

## Frontend Direction

The frontend is implemented as an enterprise SaaS interface using:

- Next.js.
- TypeScript.
- Tailwind CSS.
- Local shadcn-style UI primitives.
- Responsive layouts.
- Accessible components.
- Dashboard-oriented design.
- Typed API client modules for auth, plans, documents, actions, monitoring, audit logs, exports, **evidence index**, **section comments**, **funding readiness and allocations**, **operational dashboard**, **map workspace**, **notifications**, and **collaboration summary**.

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

- White-based.
- Clean.
- Responsive.
- Enterprise-grade.
- Map and dashboard friendly.
- Usable on desktop, laptop, tablet, and mobile.

Frontend behavior priorities:

- No broken empty states.
- Clear forbidden/not-found states.
- No raw JSON-only user experience except as fallback in audit details.
- No client-side tenant selection for data access.
- No refresh token visible to JavaScript.
- No access token persisted to localStorage.
- Simple pagination controls for workspace lists.
- Fast enough for local demo and small pilot datasets.

---

## Local Setup and Development

### Prerequisites

- .NET 8 SDK.
- Docker Desktop or a local PostgreSQL 16 instance.
- PowerShell.
- Node.js / npm.
- Python for Phase 3 facility-only exposure computation (and future AI services).
- DBeaver or psql for local database inspection and SQL script execution.

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

### Apply Database Migrations

After the baseline schema, apply the additive refresh-token migration:

```powershell
psql "Host=localhost;Port=55432;Database=lccap_db;Username=lccap_user;Password=lccap_password" -f db/migrations/002_add_refresh_tokens.sql
```

Or run this file through DBeaver before starting the backend:

```text
db/migrations/002_add_refresh_tokens.sql
```

The migration adds:

```text
public.refresh_tokens
```

This table stores hashed refresh-token session records. It does not store raw refresh tokens.

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

### Run Python exposure-computation service (Phase 3)

The Phase 3 facility-only exposure computation service is a small FastAPI application. It computes whether each critical facility point is inside/on the boundary of registered hazard `Polygon` / `MultiPolygon` geometries using explicit `EPSG:4326` and boundary-inclusive point-in-polygon containment.

Start the service (from the repository’s Python service folder):

```powershell
cd C:\projects\LCCAP\python\exposure-computation-service

# Create/activate venv (if needed)
python -m venv .venv
.venv\Scripts\Activate.ps1

# Install dependencies
python -m pip install -U pip
python -m pip install -e .

# Start the service
uvicorn app.main:app --reload --port 8000
```

Verify health:

```text
http://localhost:8000/health
```

If you want the .NET layer to use the Python adapter during exposure job processing, set runtime overrides for the API process (do not commit appsettings changes):

```powershell
$env:PythonAi__Enabled="true"
$env:PythonAi__BaseUrl="http://localhost:8000"
$env:PythonAi__TimeoutSeconds="60"
```

Notes:
- `PythonAi__Enabled` is expected to be `false` in committed configuration.
- Python does **not** access the database; .NET owns job state and persistence.

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

### Optional Local Admin / Reviewer Users

Admin and Reviewer roles are required for full RBAC and Audit Viewer testing.

If the current demo seed does not automatically create tenant Admin or Reviewer users, create local test users by copying the planner password hash for local development only.

Example tenant Admin creation:

```sql
INSERT INTO public.users (
  id,
  account_id,
  email,
  password_hash,
  full_name,
  role,
  status,
  user_scope,
  created_at_utc,
  updated_at_utc,
  is_deleted,
  row_version
)
SELECT
  gen_random_uuid(),
  account_id,
  'naga.admin@lccap.local',
  password_hash,
  'Naga Admin Demo',
  'Admin',
  'Active',
  'Tenant',
  now(),
  now(),
  false,
  gen_random_bytes(8)
FROM public.users
WHERE email = 'naga.planner@lccap.local'
  AND is_deleted = false
  AND NOT EXISTS (
    SELECT 1
    FROM public.users
    WHERE email = 'naga.admin@lccap.local'
      AND is_deleted = false
  );
```

Example tenant Reviewer creation:

```sql
INSERT INTO public.users (
  id,
  account_id,
  email,
  password_hash,
  full_name,
  role,
  status,
  user_scope,
  created_at_utc,
  updated_at_utc,
  is_deleted,
  row_version
)
SELECT
  gen_random_uuid(),
  account_id,
  'naga.reviewer@lccap.local',
  password_hash,
  'Naga Reviewer Demo',
  'Reviewer',
  'Active',
  'Tenant',
  now(),
  now(),
  false,
  gen_random_bytes(8)
FROM public.users
WHERE email = 'naga.planner@lccap.local'
  AND is_deleted = false
  AND NOT EXISTS (
    SELECT 1
    FROM public.users
    WHERE email = 'naga.reviewer@lccap.local'
      AND is_deleted = false
  );
```

Both users use:

```text
DemoPassword123!
```

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

### Run Backend Tests

```powershell
dotnet test Lccap.sln
```

Targeted examples:

```powershell
dotnet test tests/Lccap.Api.Tests/Lccap.Api.Tests.csproj --filter PlansControllerTests
dotnet test tests/Lccap.Api.Tests/Lccap.Api.Tests.csproj --filter PlanSectionsControllerTests
dotnet test tests/Lccap.Api.Tests/Lccap.Api.Tests.csproj --filter DocumentsControllerTests
dotnet test tests/Lccap.Api.Tests/Lccap.Api.Tests.csproj --filter ActionItemsControllerTests
dotnet test tests/Lccap.Api.Tests/Lccap.Api.Tests.csproj --filter MonitoringControllerTests
dotnet test tests/Lccap.Api.Tests/Lccap.Api.Tests.csproj --filter ExportControllerTests
dotnet test tests/Lccap.Api.Tests/Lccap.Api.Tests.csproj --filter AuditLogsControllerTests
dotnet test tests/Lccap.Api.Tests/Lccap.Api.Tests.csproj --filter AuthTests
dotnet test tests/Lccap.Infrastructure.Tests/Lccap.Infrastructure.Tests.csproj --filter RefreshToken
```

### Frontend Quality Checks

```powershell
cd C:\projects\LCCAP\frontend
npm run type-check
npm run lint
```

Before release/demo checkpoint:

```powershell
npm run build
```

### Session Verification

After login:

- Open DevTools.
- Go to Application.
- Check Local Storage.
- Confirm no JWT access token is stored.
- Check Cookies.
- Confirm `lccap_refresh_token` exists and is HttpOnly.
- Hard-refresh `/plans`.
- Confirm the session restores through `/api/auth/refresh`.
- Logout.
- Confirm `/api/auth/logout` is called.
- Confirm `/plans` requires login after logout.

---

## Demo Login Users

All demo users use this local development password when seeded with the instructions above:

```text
DemoPassword123!
```

| User type | Email | Role | Scope | LGU / Account | Login status |
| --- | --- | --- | --- | --- | --- |
| Platform admin | `platform.admin@lccap.local` | `SystemAdmin` | Platform | None | Seeded, but may not log in until platform-user login support is enabled |
| LGU planner | `naga.planner@lccap.local` | `Planner` | Tenant | Naga City Demo LGU | Use for primary MVP demo |
| LGU viewer | `naga.viewer@lccap.local` | `Viewer` | Tenant | Naga City Demo LGU | Tenant demo user |
| LGU planner | `pasig.planner@lccap.local` | `Planner` | Tenant | Pasig City Demo LGU | Tenant demo user |
| LGU viewer | `pasig.viewer@lccap.local` | `Viewer` | Tenant | Pasig City Demo LGU | Tenant demo user |
| LGU planner | `quezon.planner@lccap.local` | `Planner` | Tenant | Quezon City Demo LGU | Tenant demo user |
| LGU viewer | `quezon.viewer@lccap.local` | `Viewer` | Tenant | Quezon City Demo LGU | Tenant demo user |
| Optional LGU admin | `naga.admin@lccap.local` | `Admin` | Tenant | Naga City Demo LGU | Create manually if not seeded |
| Optional LGU reviewer | `naga.reviewer@lccap.local` | `Reviewer` | Tenant | Naga City Demo LGU | Create manually if not seeded |

Recommended MVP demo login:

```text
Email: naga.planner@lccap.local
Password: DemoPassword123!
```

Recommended Audit Viewer login:

```text
Email: naga.admin@lccap.local
Password: DemoPassword123!
```

or:

```text
Email: naga.reviewer@lccap.local
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

- Login succeeds.
- User lands on `/dashboard` or can navigate to `/plans`.
- Topbar shows `naga.planner@lccap.local`.
- Backend sets the `lccap_refresh_token` HttpOnly cookie.
- Local Storage does not contain a JWT access token.

### 2. Open or Create Plan

Go to:

```text
/plans
```

Expected result:

- Existing tenant plans appear in **Your workspaces**.
- Plan list uses pagination.
- Click **Open workspace** for an existing plan, or create a new one.

Suggested new plan values:

```text
Title: Naga City LCCAP 2026-2030
Start year: 2026
End year: 2030
Status: Draft
Template mode: New
```

Expected result:

- The app redirects to `/plans/{realPlanId}`.
- Default sections appear in the workspace.

### 3. Edit Plan Metadata

Open the plan summary card and edit the description or status.

Expected result:

- Save succeeds.
- The plan summary updates.
- Audit history records `PlanMetadataUpdated`.
- A stale rowVersion update should return a conflict and ask the user to refresh.

### 4. Edit Section

Open the **Executive Summary** section and enter:

```text
This is a draft executive summary for the Naga City LCCAP 2026-2030. This section will summarize priority climate risks, planned adaptation and mitigation actions, evidence references, and monitoring approach.
```

Click **Save section**.

Expected result:

- Save succeeds.
- Last edited timestamp appears.
- Refreshing the page keeps the saved content.
- Audit history records `PlanSectionUpdated`.

### 5. View Section History and Restore

Save a second version of the Executive Summary.

Example second version:

```text
This second draft adds more detail about local flood exposure, priority barangays, and monitoring responsibilities for the planning cycle.
```

Then:

1. Open **Revision History**.
2. Select the earlier version.
3. Restore it.

Expected result:

- The previous content is restored.
- Restore creates a new audit entry.
- History remains available.

### 6. Upload Document

Use a small PDF, DOCX, XLSX, PNG, JPG, or JPEG.

Suggested values:

```text
Title: CLUP Reference Map
Category: Map
Description: Sample supporting document for local climate planning evidence.
```

Expected result:

- Upload succeeds.
- Document appears in the attached documents list.
- Refreshing the workspace reloads the document list.
- Document list uses pagination.

### 7. Edit and Archive Document

Edit the document metadata.

Suggested update:

```text
Title: CLUP Reference Map - Updated Metadata
Description: Updated evidence description for the draft workspace.
```

Expected result:

- Metadata update succeeds.
- Audit history records `DocumentMetadataUpdated`.

Archive only a test/duplicate document.

Expected result:

- Document disappears from the active list.
- FileAsset remains retained.
- Audit history records `DocumentArchived`.

### 8. Create Action Item

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

- Action is created.
- Action appears in the action items list.
- Action list uses pagination.

### 9. Edit and Archive Action Item

Edit the action item status or budget.

Expected result:

- Update succeeds.
- Audit history records `ActionItemUpdated`.
- Stale rowVersion updates return conflict.

Archive only a test/duplicate action item.

Expected result:

- Active list no longer shows the archived action.
- Audit history records `ActionItemArchived`.

### 10. Create Monitoring Indicator

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

- Indicator is created.
- Indicator appears in the monitoring indicators list.
- Monitoring list uses pagination.

### 11. Edit and Archive Monitoring Indicator

Edit the monitoring indicator values or status.

Expected result:

- Update succeeds.
- Audit history records `MonitoringIndicatorUpdated`.
- Stale rowVersion updates return conflict.

Archive only a test/duplicate monitoring indicator.

Expected result:

- Active list no longer shows the archived indicator.
- Audit history records `MonitoringIndicatorArchived`.

### 12. Generate and Download PDF Draft Package

In the export section:

1. Click **Generate PDF draft**.
2. Wait for the latest export job to show **Completed**.
3. Click **Download PDF**.

Expected result:

- PDF downloads as `lccap-draft-package.pdf`.
- PDF contains the plan title and saved section content.

This PDF is a draft working output for internal preparation. It is not an official submission package.

### 13. Review Audit History

Login as Admin or Reviewer.

Open:

```text
/audit
```

Expected result:

- Audit History page loads.
- Recent plan, section, document, action, monitoring, and archive events appear.
- Filters work by entity/action/date.
- Detail view shows old values, new values, and metadata.
- Planner and Viewer cannot access this page.

### 14. RBAC Check

Login as Viewer:

```text
naga.viewer@lccap.local
DemoPassword123!
```

Expected result:

- Viewer can read plan workspace data.
- Viewer cannot create, edit, restore, export, or archive.
- Unauthorized API calls return 403.

Login as Planner:

```text
naga.planner@lccap.local
DemoPassword123!
```

Expected result:

- Planner can create, edit, restore, and export.
- Planner cannot archive.
- Archive API calls return 403.

Login as Admin:

```text
naga.admin@lccap.local
DemoPassword123!
```

Expected result:

- Admin can archive.
- Admin can view Audit History.

### 15. Session Check

After login:

1. Open DevTools.
2. Go to Application.
3. Check Local Storage.
4. Confirm no JWT appears in Local Storage.
5. Check Cookies.
6. Confirm `lccap_refresh_token` exists as an HttpOnly cookie.
7. Hard-refresh `/plans`.
8. Confirm the session restores.
9. Logout.
10. Confirm `/api/auth/logout` is called.
11. Confirm `/plans` requires login after logout.

### Phase 2 demo extension (optional; same workspace)

After completing the MVP script through PDF export and audit checks, you can walk these additional preparation flows locally. All remain **draft workspace outputs**—not official submissions, funding approvals, national reporting, PostGIS exposure products, full GIS analytics, real-time collaboration, email notifications, or AI-generated mandates.

1. **Monitoring update history** — Open a monitoring indicator, add an update, and confirm history lists the new entry (`POST`/`GET` monitoring updates APIs).
2. **Evidence linking** — Edit a document and link it to a section and/or an action via metadata; confirm the evidence index reflects the link.
3. **Evidence index** — Open the evidence index panel; download **evidence-index.csv** as a working extract.
4. **Section comments** — Create a comment on a section; resolve and reopen; archive when finished testing.
5. **Funding readiness** — Open CCET/readiness panels; review cataloged sources/programs; avoid implying PSF eligibility or portal submission.
6. **Funding allocation** — Create a **planned** allocation against an action; archive a test allocation if needed.
7. **Richer exports** — Download **package manifest** and one or more CSV exports (actions matrix, monitoring matrix, funding readiness).
8. **Operational dashboard** — Open the plan operational dashboard / activity view and confirm recent workspace activity appears.
9. **Map foundation** — Register a GeoJSON layer from the plan workspace; confirm map workspace metadata updates (foundation feature, not exposure analytics).
10. **Notifications and collaboration** — Trigger a workspace action that produces an in-app notification (for example a comment event); open the notification center; view the read-only collaboration summary and confirm there is **no** group configuration UI in Phase 2.

### Phase 3 demo extension: facility-only exposure workflow

After completing the Phase 2 demo extension locally, you can test the **implemented Phase 3 facility-only exposure workflow**. All outcomes below remain **draft/analysis support** only—limited to explicit `EPSG:4326` and facility points evaluated against registered hazard `Polygon` / `MultiPolygon` geometries (no full PostGIS GIS analytics, and no official risk scoring outputs).

Prerequisites (do these first):
1. Start the Python exposure-computation service and verify `GET /health` is returning `engineName = "FacilityExposureEngine"`.
2. Start the .NET API with runtime overrides so the Python adapter is enabled:
   - `PythonAi__Enabled=true`
   - `PythonAi__BaseUrl=http://localhost:8000`

Then:
1. Open the plan map workspace.
2. Select the hazard GeoJSON layer (the UI selection is based on the plan’s GeoJSON layer assets).
3. Register the hazard layer (if it is not already active).
4. In the exposure readiness panel, click **Run exposure analysis** to queue a job.
5. Click **Process job** for the queued job.
6. Verify the job transitions to `Completed`.
7. Verify the **Exposure summaries** list:
   - becomes non-empty when at least one facility is exposed by boundary-inclusive point-in-polygon containment, and
   - stays empty (and shows the completed-zero message) when the run produces zero stored summaries.
8. If a run fails (for example Python unavailable or an unsupported hazard geometry), confirm the UI shows failure messaging and that no new stored summaries are persisted for that failed run.

For the repeatable end-to-end verification checklist, use `docs/manual-verification/phase3-exposure-workflow.md`.

---

## Testing and Quality Gates

For normal backend feature slices:

```powershell
dotnet build Lccap.sln
dotnet test tests/Lccap.Api.Tests/Lccap.Api.Tests.csproj --filter <AffectedTests>
```

For normal frontend feature slices:

```powershell
cd C:\projects\LCCAP\frontend
npm run type-check
npm run lint
```

Before release/demo checkpoint:

```powershell
cd C:\projects\LCCAP
dotnet test Lccap.sln

cd C:\projects\LCCAP\frontend
npm run type-check
npm run lint
npm run build
```

Current test categories:

- API/controller tests.
- EF Core mapping tests.
- Storage service tests.
- Authentication tests.
- Refresh-token persistence tests.
- Refresh-token rotation tests.
- RBAC tests.
- Audit viewer tests.
- Optimistic concurrency tests.
- Pagination tests.
- Frontend type-check.
- Frontend lint.
- Manual MVP and Phase 2 E2E validation.
- Python pytest for the Phase 3 facility-only exposure-computation service.
- Python `compileall` checks for the Phase 3 exposure-computation service.
- ExposureAnalysisJobsControllerTests for Phase 3 exposure job processing.
- ExposureSummariesControllerTests for Phase 3 exposure summary persistence/read behavior.
- Manual Phase 3 E2E verification via `docs/manual-verification/phase3-exposure-workflow.md`.

Quality rules:

- Do not deliver code with compile errors.
- Do not deliver code with failing tests.
- Do not update tracker unless build/tests pass.
- Do not guess database schema.
- Always preserve tenant isolation.
- Use exact schema when implementing database-facing tasks.
- Keep MVP, Phase 2, and Phase 3 scope separated.
- Preserve server-side RBAC as source of truth.
- Do not rely on UI hiding as authorization.
- Do not store refresh tokens in JavaScript-accessible storage.
- Do not store access tokens in localStorage.

Suggested regression commands:

```powershell
dotnet test Lccap.sln
```

```powershell
cd C:\projects\LCCAP\frontend
npm run type-check
npm run lint
```

---

## Roadmap

### MVP (complete)

- Backend core APIs.
- Auth and tenant context.
- Refresh-token-backed sessions.
- Memory-only access-token handling.
- Plan workspace.
- Plan sections.
- Section history and restore.
- Documents and file storage.
- Document edit/archive/audit.
- Action items.
- Action item edit/archive/audit.
- Monitoring indicators.
- Monitoring indicator edit/archive/audit.
- PDF exports.
- Download exports.
- Server-side RBAC.
- Audit viewer.
- Optimistic concurrency.
- Pagination.
- Frontend foundation.
- Demo workflow.

### Phase 2 (complete in codebase; preparation workspace only)

- Evidence index (JSON/CSV) and document evidence links.
- Section review comments.
- CCET / funding-readiness tagging and panels.
- Funding source/program catalogs and planned action funding allocations (not a funding portal).
- Monitoring update history.
- Richer export package manifest and CSV working outputs.
- Operational dashboard / activity feed.
- Map workspace / GeoJSON layer foundation.
- In-app notifications with workspace-driven events; read-only collaboration summary; tenant-admin/seed-managed groups.
- Continued positioning: complement official systems; no official submission, approval, national reporting, PostGIS exposure engine, full GIS analytics product, or AI decision platform.

### Phase 3 (partially implemented for facility-only exposure analysis)

- Phase 3 facility-only exposure workflow is partially implemented (hazard layer registration → exposure analysis jobs → persisted ExposureSummary rows + frontend display).
- Remaining Phase 3 roadmap items include PostGIS/spatial analytics, exposed area/population/risk metrics, scenario comparison, recommendation engine, broader AI/RAG workflows, and production hardening.
- Continued interoperability and integration-ready APIs/exports.
- Operational hardening and advanced observability.
- Potential BFF-style auth if production deployment requires it.

---

## Enterprise Design Principles

- Plan-centric architecture.
- Strict multi-tenant isolation.
- Clean Architecture boundaries.
- Explicit database schema fidelity.
- FileAsset storage abstraction.
- Async AI processing.
- Auditable workflows.
- Test-first hardening.
- Secure-by-default API behavior.
- Responsive enterprise UI.
- Server-side authorization, not UI-only permissions.
- Memory-only access-token handling.
- HttpOnly refresh-token rotation.
- Optimistic concurrency for editable records.
- Soft-delete archive with audit history.
- Pagination by default for tenant lists.
- No client-supplied account IDs.
- No hard deletes for user-correctable workspace records.
- No official-submission claims in MVP or Phase 2 shipped scope.
- Phase discipline: MVP, Phase 2, and Phase 3 must not be mixed without explicit product approval.

---

## Current Enterprise MVP and Phase 2 Caveats

The updated **MVP plus Phase 2** codebase is suitable for internal demos and controlled pilot conversations, with the following caveats:

- It is not an official submission platform.
- It is not a national reporting platform.
- It is not a funding portal or PSF application system.
- It is not a replacement for official risk assessment systems.
- It is not a PostGIS exposure analysis engine or full enterprise GIS product.
- It is not an AI decision-making or official recommendation authority.
- It is not yet a fully production-hardened SaaS deployment.
- Production should add stricter CORS/cookie settings.
- Production should add CSRF strategy if cookie-auth expands.
- Production should add rate limiting for auth and uploads.
- Production should add structured logging and correlation IDs.
- Production should add deployment runbooks.
- Production should add CI quality gates.
- Production should add monitoring/alerting.
- Production should define audit retention policy.

Safe to demo now:

- LGU workspace planning flow.
- Plan creation and editing.
- Section save/history/restore.
- Documents upload/edit/archive and evidence linking.
- Evidence index (JSON) and CSV export as a working output.
- Section review comments (create/resolve/reopen/archive).
- Action create/edit/archive.
- Monitoring create/edit/archive and **monitoring update history**.
- CCET / funding readiness panels and catalog-driven allocation UI (**planned** allocations only).
- Export package manifest and CSV matrix downloads as preparation aids.
- Operational dashboard / activity views for the plan.
- Map workspace metadata and GeoJSON layer registration (foundation).
- In-app notification feed fed by workspace events; read-only collaboration summary (admin/seed-managed groups).
- PDF draft export.
- RBAC behavior.
- Audit history.
- Refresh-token-backed session behavior.
- Phase 3 facility-only exposure workflow for critical facilities:
  - hazard layer registration from GeoJSON map assets
  - exposure analysis job queue/process
  - persisted ExposureSummary rows (replace-for-job semantics)
  - completed/failed/zero-summary UI messaging
- Python exposure-computation service for facility-only point-in-polygon evaluation:
  - explicit `EPSG:4326` only
  - `Polygon` / `MultiPolygon` hazard geometries
  - boundary-inclusive containment behavior
- Manual Phase 3 end-to-end verification using `docs/manual-verification/phase3-exposure-workflow.md`.

Do not claim yet:

- Official government submission.
- Production-scale national deployment.
- Funding eligibility processing.
- Full GIS analytics or mandated spatial compliance outputs.
- PostGIS exposure analysis or national exposure reporting.
- Exposed area calculation and exposed population approximation.
- Risk score calculation and numeric risk metrics.
- Real-time collaboration, email notification delivery, or end-user notification centers tied to external channels.
- Scenario comparison, recommendation engine outputs, or recommendation authority.
- Automated AI decision-making.
- Official submission, approval, national reporting, or mandated spatial compliance outputs.
- Replacement for CCC/DILG/NICCDIES/PlanSmart/UNDP SHIELD systems.

---

## Author

**Jeff Martin Abayon**

Full-Stack Engineer  
Enterprise Systems Architecture  
Climate Planning SaaS / Engineering Data Platforms

Calgary, Canada

[jmjabayon@gmail.com](mailto:jmjabayon@gmail.com)

[LinkedIn Profile](https://www.linkedin.com/in/jeff-martin-abayon-calgary/)


---

## Appendix A - Manual Verification Checklist

Use this appendix as a concise checklist before screenshots, demos, or local release checkpoints.

### Backend verification

- Run `dotnet build Lccap.sln`.
- Run `dotnet test Lccap.sln`.
- Confirm all Domain tests pass.
- Confirm all Application tests pass.
- Confirm all Infrastructure tests pass.
- Confirm all API tests pass.
- Confirm AuthTests pass.
- Confirm PlansControllerTests pass.
- Confirm PlanSectionsControllerTests pass.
- Confirm DocumentsControllerTests pass.
- Confirm ActionItemsControllerTests pass.
- Confirm MonitoringControllerTests pass.
- Confirm ExportControllerTests pass.
- Confirm AuditLogsControllerTests pass.
- Confirm RefreshToken mapping tests pass.

### Frontend verification

- Run `npm run type-check`.
- Run `npm run lint`.
- Run `npm run build` before demo checkpoint.
- Confirm login page loads.
- Confirm dashboard shell loads.
- Confirm `/plans` loads after login.
- Confirm `/audit` loads only for Admin/Reviewer.
- Confirm responsive layout still works on smaller viewport.

### Database verification

- Confirm `public.refresh_tokens` exists.
- Confirm `public.audit_logs` exists.
- Confirm `public.users` includes demo tenant users.
- Confirm `public.plans` has tenant records.
- Confirm archived records use `is_deleted = true` and remain in database.
- Confirm audit logs are tenant-scoped by `account_id`.
- Confirm refresh-token rows store hashes only.

### Auth/session verification

- Login as `naga.planner@lccap.local`.
- Confirm login succeeds.
- Confirm `lccap_refresh_token` cookie exists.
- Confirm `lccap_refresh_token` is HttpOnly.
- Confirm Local Storage has no JWT token.
- Hard refresh `/plans`.
- Confirm `/api/auth/refresh` restores the session.
- Logout.
- Confirm `/api/auth/logout` runs.
- Confirm UI returns to logged-out state.

### RBAC verification

- Login as Viewer.
- Confirm Viewer can read workspace data.
- Confirm Viewer cannot create or edit.
- Confirm Viewer cannot export.
- Confirm Viewer cannot archive.
- Confirm unauthorized mutation calls return 403.
- Login as Planner.
- Confirm Planner can create/edit/restore/export.
- Confirm Planner cannot archive.
- Login as Admin.
- Confirm Admin can archive.
- Login as Reviewer.
- Confirm Reviewer can view audit history.

### Audit verification

- Edit plan metadata.
- Confirm `PlanMetadataUpdated` appears.
- Save section.
- Confirm `PlanSectionUpdated` appears.
- Restore section.
- Confirm `PlanSectionRestored` appears.
- Edit document metadata.
- Confirm `DocumentMetadataUpdated` appears.
- Archive test document.
- Confirm `DocumentArchived` appears.
- Edit action item.
- Confirm `ActionItemUpdated` appears.
- Archive test action item.
- Confirm `ActionItemArchived` appears.
- Edit monitoring indicator.
- Confirm `MonitoringIndicatorUpdated` appears.
- Archive test monitoring indicator.
- Confirm `MonitoringIndicatorArchived` appears.

### Pagination verification

- Open `/plans`.
- Confirm Previous is disabled on first page.
- Confirm Next enables when more records exist.
- Confirm active page items display correctly.
- Confirm archived records do not appear.
- Confirm documents list pagination works.
- Confirm action list pagination works.
- Confirm monitoring list pagination works.
- Confirm audit log pagination works.

### Export verification

- Open a plan with saved section content.
- Generate PDF draft package.
- Confirm export job reaches Completed.
- Download PDF.
- Confirm PDF opens.
- Confirm plan title appears.
- Confirm section content appears.
- Confirm download is tenant-scoped.

### Phase 3 exposure verification

- Confirm the Python exposure-computation service health check is reachable and returns `FacilityExposureEngine` / `facility-v1` with `computationSupported=true` at `GET /health`.
- Confirm the facility-only compute contract works for a minimal success payload (explicit `EPSG:4326`, hazard `Polygon` / `MultiPolygon`, and boundary-inclusive point-in-polygon containment using `covers(point)`).
- Start the .NET API with runtime overrides so `PythonAi:Enabled` is enabled for the Phase 3 adapter path.
- In the frontend plan map workspace, register a hazard layer from the selected GeoJSON hazard layer asset.
- Queue an exposure analysis job from the exposure readiness panel, then process the queued job.
- Verify job state transitions to `Completed` and that exposure summaries are displayed after processing.
- Verify the zero-result path: when the computation succeeds but produces zero stored summaries, the frontend shows the completed-zero message and the summaries list remains empty.
- Verify safe failure behavior when Python is unavailable or when hazard geometry is unsupported: the job should become `Failed` and summaries should remain empty for that failed run.
- For a repeatable E2E checklist with runtime prerequisites, use `docs/manual-verification/phase3-exposure-workflow.md`.

### Positioning verification

- Confirm UI and docs do not claim official submission.
- Confirm PDF is described as draft working output.
- Confirm product language says complement, not replacement.
- Confirm no language implies funding approval.
- Confirm no language implies national reporting authority.

---

## Appendix B - Suggested Future Hardening Slices

These slices are intentionally deferred beyond the **MVP plus Phase 2** preparation workspace shipped in this repository.

### P1 - Production auth and browser security

- Add CSP.
- Add stricter production CORS.
- Add CSRF strategy if access-token cookies are introduced.
- Add auth and upload rate limiting.
- Add fail-fast guard when demo seed is enabled outside Development.

### P1 - Observability

- Add structured logging.
- Add request correlation IDs.
- Add export failure diagnostics.
- Add upload failure diagnostics.
- Add operational health/readiness checks.

### P1 - CI quality gate

- Run `dotnet test Lccap.sln`.
- Run frontend type-check.
- Run frontend lint.
- Run frontend build.
- Reject PR on failure.

### P2 - Audit management

- Define audit retention policy.
- Add audit export for tenant admins if needed.
- Add audit summary dashboard.
- Add retention or compaction job.

### P2 - Richer exports

- Add DOCX export.
- Add richer PDF layout.
- Add annex/evidence listing.
- Add action and monitoring summary sections.
- Add export templates.

### P2 - Collaboration (future hardening beyond Phase 2 foundation)

- Collaboration group **CRUD** and self-service administration.
- User **invitations** and onboarding workflows.
- **Notification preferences** and channel routing.
- **Email** and **WebSocket**/push delivery.
- Richer **role-based routing** for notifications and reviews.

### P3 - Exposure workflow hardening (remaining beyond facility-only)

- Extend from facility-only point-in-polygon evaluation to full PostGIS-based spatial analytics.
- Add barangay polygon intersection logic.
- Compute exposed area, exposed population approximation, and numeric risk metrics.
- Enable scenario comparison and recommendation-style outputs (still as auditable jobs, not automated decisions).
- Production hardening for exposure runs (operational readiness, failure diagnostics, and verification repeatability).

### P3 - Interoperability

- Add integration-ready APIs.
- Add structured import/export formats.
- Add PostGIS support for full spatial analytics beyond facility-only evaluation.
- Add external system mapping.
- Add formal data exchange contracts.

### P3 - AI support

- Add AI job queue.
- Add evidence-grounded section drafting.
- Add document summarization.
- Add recommendation-style assistance.
- Add RAG search over tenant evidence.
- Add human review workflow before adopting AI output.

---

## Appendix C - Common Local Commands

### Backend

```powershell
cd C:\projects\LCCAP
dotnet build Lccap.sln
dotnet test Lccap.sln
```

### Frontend

```powershell
cd C:\projects\LCCAP\frontend
npm run type-check
npm run lint
npm run build
```

### Python exposure-computation service (Phase 3)

```powershell
cd C:\projects\LCCAP\python\exposure-computation-service

# Run Python tests
python -m pytest -q

# Quick import/bytecode validation
python -m compileall app tests

# Start the service
uvicorn app.main:app --reload --port 8000
```

### Run API

```powershell
cd C:\projects\LCCAP
dotnet run --project .\src\Lccap.Api\Lccap.Api.csproj
```

### Run frontend production-like

```powershell
cd C:\projects\LCCAP\frontend
$env:PORT="3010"
npm start
```

### Git checkpoint

```powershell
git status -u
git add -A
git diff --cached --name-status
git commit -m "feat: describe change"
```

---

## Appendix D - Documentation Maintenance Rules

When updating the project:

- Keep README aligned with implemented behavior.
- Keep local-development.md aligned with actual commands.
- Keep security-notes.md aligned with current auth and RBAC behavior.
- Keep environment-variables.md aligned with real config keys.
- Keep tracker JSON updated only after verification passes.
- Do not document features as complete until tests and manual checks pass.
- Keep MVP, Phase 2, and Phase 3 boundaries clear.
- Keep Phase 3 exposure claims aligned to the current facility-only point-in-polygon limitations (explicit `EPSG:4326` and `Polygon` / `MultiPolygon` hazard geometries; deferred area/population/risk metrics).
- Avoid official-submission language unless a future integration is formally approved.

---

## Appendix E - Safe Demo Claims

Safe claims:

- LGU-facing planning workspace.
- Structured LCCAP preparation.
- Tenant-scoped workspace.
- Server-side RBAC.
- Audit history.
- Section restore.
- Draft PDF package.
- Phase 2 preparation aids: evidence index, review comments, funding readiness views, richer CSV exports, operational dashboard, map layer foundation, in-app notification feed, read-only collaboration summary (admin/seed groups).
- Phase 3 facility-only exposure workflow (hazard layer registration → exposure analysis job queue/process → persisted ExposureSummary rows with replace-for-job semantics; completed/failed/zero-summary messaging in the frontend).
- Python exposure-computation service for facility-only point-in-polygon evaluation (explicit `EPSG:4326`, `Polygon` / `MultiPolygon` hazards, boundary-inclusive containment).
- Completed exposure job state with `output_json` engine/diagnostics/persistence metadata persisted for review.
- Memory-only access token with HttpOnly refresh cookie.
- Locally validated end-to-end **MVP plus Phase 2** workspace foundation.

Avoid claims:

- Official government platform or submission channel.
- National reporting system.
- Funding approval system or PSF portal replacement.
- Full GIS analytics or PostGIS exposure authority.
- Exposed area/population/risk scoring metrics (deferred in facility-only exposure computation).
- Scenario comparison outputs.
- Recommendation engine outputs or recommendation authority.
- Real-time co-editing, email-delivered notifications, or self-service collaboration administration in Phase 2.
- Replacement for CCC/DILG/NICCDIES/PlanSmart/UNDP SHIELD.
- Production-scale national deployment.
- Fully automated climate action recommendation authority.
- AI-generated official plan approval.
