# Security notes (MVP)

This document summarizes current MVP security rules and planned hardening. It is not a penetration-test report or formal threat model.

## CORS

Development CORS is configured in `Program.cs` to allow local frontend origins (`http://localhost:3000`, `3001`, `3010`) for MVP velocity. Production environments must explicitly configure allowed origins for the deployed frontend and restrict headers/methods to the minimum required.

## Tenant isolation via `account_id`

- Each LGU (tenant) is represented as an **Account**; **`account_id` is the tenant boundary**.
- Application behavior must not read or write data across tenant boundaries.

## Role-Based Access Control (RBAC) - MVP

- **Server-Side Enforcement**: RBAC is enforced on the server for all workspace APIs (Plans, Sections, Documents, Actions, Monitoring, Exports).
- **Role Claim**: The user's role is sourced from the `role` claim in the JWT.
- **MVP Role Policy**:
  - **Viewer**: Read-only access to all workspace content. Cannot create, update, archive, restore, or upload.
  - **Reviewer**: Read-only access + Export capability. Cannot create, update, archive, or restore.
  - **Planner**: Full read, create, update, and restore capabilities. Cannot **Archive** (Admin-only).
  - **Admin**: Full control over tenant workspace content, including **Archive**.
  - **Platform Roles** (`SystemAdmin`, `NationalAdmin`, `AgencyAdmin`): Do not grant tenant content mutation by default unless the user also has a tenant account context and appropriate role.
- **403 Forbidden**: Authenticated users attempting actions not permitted by their role receive a **403 Forbidden** response.
- **UI Hiding**: Frontend role-based hiding is a convenience only; the backend remains the authoritative source of truth for all permissions.
- **Isolation Priority**: RBAC is checked **in addition to** tenant isolation. A user must belong to the correct tenant AND have the correct role to perform an action.

## No `accountId` from the request

- Callers must not supply tenant identity in an unconstrained way (e.g. arbitrary `accountId` on the URL or body) to assume another tenant’s context.
- Tenant scope is expected to come from the **authenticated principal** (e.g. JWT claims), not from client-supplied tenant IDs.

## JWT bearer authentication

- API authentication uses **JWT bearer** tokens.
- **Issuer**, **audience**, and **signing key** must be configured consistently for token issue and validation.
- **Signing keys** are secrets: protect them in all non-local environments.

## Password hashing

- User passwords must be stored using a strong one-way hash suitable for credentials (not plain text).

## File upload safety

- Uploads are constrained by **size limits** and an **allowed extensions** list (configuration-driven).
- Files are stored in a **tenant-scoped** layout; API responses should not expose raw server paths unnecessarily.

## Local file storage and path traversal

- The storage layer is responsible for resolving paths under a configured **root** and rejecting **path traversal** (`..`, absolute paths outside the root, etc.) so a tenant cannot read or write another tenant’s files via crafted names.

## Export download safety

- Export downloads must respect **tenant scope**; cross-tenant access should **not** leak whether a resource exists (e.g. **404** for unauthorized or cross-tenant requests).

## Cross-tenant 404 behavior

- When a resource belongs to another tenant or the caller is not permitted to see it, prefer **404** (or equivalent non-leaking response) over **403** where that is the product decision, to avoid **resource enumeration** across tenants.

## Known future hardening

The following are **not** necessarily implemented in the MVP but are expected directions:

- **Rate limiting** — protect auth and upload endpoints from abuse.
- **Security headers** — HSTS, CSP, X-Content-Type-Options, etc., as appropriate to hosting.
- **Audit service** — structured, tamper-evident logging of security-relevant actions.
- **Swagger auth docs** — document bearer auth and error shapes for integrators.
- **Production secret management** — Key Vault / platform secrets, rotation, and separation of dev/stage/prod credentials.

For environment-specific secret handling, see [Environment variables](environment-variables.md).

## Frontend auth (MVP)

- The Next.js app calls the API using **`NEXT_PUBLIC_API_BASE_URL`** (public origin only; never put secrets in `NEXT_PUBLIC_*` variables).
- After **login**, the browser stores the JWT in **`localStorage`** for MVP velocity. This is **not** equivalent to httpOnly cookie sessions; XSS within the origin could theoretically access the token. Treat this as a development-oriented tradeoff until hardened.
- **Cross-tab behavior:** the UI listens for the browser `storage` event on `localStorage` so sign-in or sign-out in **another tab** updates session state in this tab. Same-tab updates still rely on navigation and in-app state; `localStorage` does not fire `storage` in the same document that wrote the value.
- **Production hardening target (non-exhaustive):** prefer **httpOnly, Secure, SameSite** cookies (or BFF) for session delivery; pair cookie sessions with a deliberate **CSRF** strategy for mutating requests if cookies are used cross-site; deploy a strict **Content-Security-Policy** and other security headers to reduce XSS reach; use **short-lived access tokens** with **refresh token rotation** (or equivalent) where a refresh flow exists — design token lifetimes and revocation with your API team.
- **Recommended later:** issue short-lived tokens with refresh handled server-side, store session identifiers in **httpOnly, Secure, SameSite** cookies, add **CSP** and XSS defenses, and avoid storing bearer tokens in `localStorage` for production tenant workloads.
- The frontend must **never** log tokens, show tokens in the UI, or persist passwords client-side.

See `frontend/lib/auth/auth-storage.ts` for the isolated storage layer so the strategy can be swapped without rewriting API callers.

## Frontend action items (MVP)

- **Client validation is convenience only** — required fields, numeric ranges, and date ordering mirror the API contract but **do not replace** server-side validation, authorization, or tenant rules.
- **Backend remains the source of truth** for allowed values, persistence, and cross-tenant isolation.
- **Do not send `accountId`** from the frontend; tenant scope must come from the authenticated session (e.g. JWT), not client-supplied tenant identifiers.
- **Positioning:** action items support **LGU working preparation** and export-ready draft packaging inside the workspace. They are **not** an official government submission, approval workflow, or replacement for national or agency systems.

## Frontend monitoring indicators (MVP)

- **Client validation is convenience only** — required fields, percent-complete range, and optional numeric fields mirror the API contract but **do not replace** server-side validation, authorization, or tenant rules.
- **Backend remains the source of truth** for allowed values, persistence, and cross-tenant isolation.
- **Do not send `accountId`** from the frontend; tenant scope must come from the authenticated session (e.g. JWT), not client-supplied tenant identifiers.
- **Positioning:** monitoring indicators support **LGU working implementation tracking** and export-ready draft packaging inside the workspace. They are **not** official reporting or submission to government systems, **not** a national dashboard, and **not** an approval workflow.

## Frontend PDF exports (MVP)

- **Draft working outputs only** — generated files are **LGU preparation and export-ready draft packages** from the workspace, **not** official government submissions, certifications, or approval decisions.
- **Backend controls tenant access** to create, poll, and download exports; the UI does not substitute for server authorization.
- **Do not send `accountId`** from the frontend; tenant scope must come from the authenticated session (e.g. JWT).
- **Never display `storedPath` / `stored_path` or other internal storage locations** in the UI; downloads use the API download endpoint only.
- **Download flow** — the client requests the backend `GET /api/exports/{id}/download` with bearer auth, receives bytes as a `Blob`, triggers a save via a short-lived object URL, then **revokes** that URL after use.
- **Do not log** tokens, raw blob contents, or sensitive response payloads.
- **Positioning:** exports **complement existing official systems**; they are **not** a replacement for CCC/DILG/LGA/NICCDIES/PlanSmart/PSF/SHIELD processes or official channels.

## Frontend uploads (MVP)

- **Client checks are convenience only** — extension lists, size caps (e.g. 25 MB in the UI), and category pickers mirror the MVP contract but **do not replace** server validation, quotas, or malware defenses.
- **Backend remains the source of truth** for allowlists, tenant scope, storage paths, and rejection reasons.
- **Do not trust file names or client-supplied MIME hints** for security decisions; filenames may be misleading or duplicated.
- **Never render `storedPath`, `stored_path`, or raw storage URIs** if they appear in responses; the UI must not leak server filesystem layout.
- Allowed extensions in the frontend follow the same MVP set described for the API (e.g. `.pdf`, Office, common images); production systems may tighten independently.

## Document metadata and archive (MVP)

- **`PUT /api/documents/{id}/metadata`** updates catalog fields only; it does not replace the uploaded file bytes.
- **`DELETE /api/documents/{id}`** archives the **document** row (soft delete: `is_deleted` and related columns). It is **not** implemented as a physical purge of tenant files in this MVP slice.
- **`file_assets`** rows are **not** soft-deleted by archive in this slice; blobs remain in tenant-scoped storage until any future lifecycle or purge process.
- **Audit** — successful metadata updates and archives write **`audit_logs`** rows (`DocumentMetadataUpdated`, `DocumentArchived`) with tenant and user linkage and JSON snapshots for accountability.
- **Do not send `accountId`** from the client for these routes; scope comes from the JWT/session only.

## Action items — update, archive, and audit (MVP)

- **`PUT /api/actions/{actionItemId}`** updates allowed catalog-style fields on the **action item** row only; `account_id` and `plan_id` are not accepted from the client.
- **`DELETE /api/actions/{actionItemId}`** archives the **action item** row only (soft delete: `is_deleted` and related columns). There is **no** hard delete or purge in this MVP slice.
- **Active lists** — plan-scoped action lists return only rows where `is_deleted` is false; archived rows stay out of the LGU workspace list.
- **Audit** — successful updates and archives write **`audit_logs`** rows (`ActionItemUpdated`, `ActionItemArchived`) with tenant and user linkage and JSON snapshots for accountability.
- **Do not send `accountId`** from the client for these routes; scope comes from the JWT/session only.

## Monitoring indicators — update, archive, and audit (MVP)

- **`PUT /api/monitoring/indicators/{indicatorId}`** updates allowed fields on the **monitoring indicator** row; extended workspace fields that are not table columns are stored in **`metadata_json`** using the existing app convention (`currentValue`, `progressPercent`, `frequency`, `responsibleOffice`). `account_id` and `plan_id` are not accepted from the client.
- **`DELETE /api/monitoring/indicators/{indicatorId}`** archives the **monitoring indicator** row only (soft delete: `is_deleted` and related columns). It is **not** a physical purge; **`monitoring_updates`** rows are not modified or hard-deleted by this handler.
- **Active lists** — plan-scoped indicator lists return only rows where `is_deleted` is false.
- **Audit** — successful updates and archives write **`audit_logs`** rows (`MonitoringIndicatorUpdated`, `MonitoringIndicatorArchived`) with tenant and user linkage and JSON snapshots for accountability.
- **Do not send `accountId`** from the client for these routes; scope comes from the JWT/session only.

## Plan metadata and archive (MVP)

- **`PUT /api/plans/{id}`** updates plan metadata fields (title, years, status, template mode, version, description); `account_id` is not accepted from the client.
- **`DELETE /api/plans/{id}`** archives the **plan** row (soft delete: `is_deleted = true` and `status = 'Archived'`). There is **no** hard delete or purge of child records (sections, documents, actions, etc.) in this MVP slice.
- **Active lists** — plan lists and workspace queries return only rows where `is_deleted` is false and `status != 'Archived'`; archived plans stay out of the LGU workspace.
- **Audit** — successful updates and archives write **`audit_logs`** rows (`PlanMetadataUpdated`, `PlanArchived`) with tenant and user linkage and JSON snapshots for accountability.
- **Do not send `accountId`** from the client for these routes; scope comes from the JWT/session only.

## Section revision history and restore (MVP)

- **Audit-backed history** — Section revisions are sourced from `audit_logs` where `entity_name = 'PlanSection'`.
- **Tenant-scoped restore** — Restore operations strictly verify that the audit log entry, the section, and the parent plan all belong to the current authenticated account.
- **Action audit** — Section updates and restores write new `audit_logs` entries (`PlanSectionUpdated`, `PlanSectionRestored`) with old/new value snapshots.
- **No raw JSON exposure** — The API and UI extract only the necessary fields (title, content) from the audit log JSON; raw unrelated audit data is not exposed.
- **No schema changes** — This MVP implementation uses the existing `audit_logs` table and does not require a dedicated revisions table.
- **Do not send `accountId`** from the client for these routes; scope comes from the JWT/session only.

## Optimistic Concurrency and RowVersion (MVP)

- **Authoritative Backend** — The backend strictly enforces optimistic concurrency using `RowVersion` (bytea) for Plans, Action Items, and Monitoring Indicators.
- **Latest Detail Fetch** — Frontend edit flows must ensure they use the latest concurrency token. If a list response is lightweight (e.g. Plans list), the UI must fetch the full detail by ID before opening an edit form.
- **User-Friendly Conflicts** — Concurrency conflicts (HTTP 409) are mapped to clear user instructions: "This record was changed elsewhere. Refresh and try again."
- **Legacy Data Handling and Repair** — Legacy records missing a concurrency token are handled gracefully. The backend automatically repairs missing or empty `RowVersion` tokens for active, tenant-scoped records during detail or list retrieval. The UI remains tolerant of missing tokens but blocks editing until a valid token is obtained via refresh.
- **Token Rotation** — Concurrency tokens are rotated (assigned a new random 8-byte value) on every successful update to ensure the next update requires the latest token.
- **Cryptographic Randomness** — New tokens are generated using a cryptographically strong random number generator (`RandomNumberGenerator.Fill`).

## Frontend uploads — hardening (directional)

- Content validation beyond extension (magic-bytes / MIME sniffing), antivirus scanning, asynchronous malware pipelines, per-tenant quotas, and **signed URLs** or gateway-controlled downloads from object storage instead of exposing internal paths.

## Demo Seed Security (Development-only)

- **Development-only**: The demo seed service is explicitly restricted to the `Development` environment and must be enabled via configuration.
- **Credential Safety**: Do not commit real passwords to `appsettings.Development.json`. Use environment variables or local secrets.
- **Platform Admin**: The platform admin demo user is seeded with `account_id: null` per schema requirements.
- **Tenant Isolation**: LGU demo users are seeded with their respective `account_id` to support tenant isolation testing.
- **Not Official**: Demo accounts and users are for development/testing only and do not represent official government entities or approval authorities.

## Audit History Viewer (MVP)

- **Read-Only**: The audit history viewer is strictly read-only. No audit records can be created, updated, or deleted via the viewer.
- **Tenant-Scoped**: Users can only view audit logs belonging to their own `account_id`. Cross-tenant access is strictly forbidden and enforced server-side.
- **RBAC Enforced**: Access is restricted to **Admin** and **Reviewer** roles only. **Planner**, **Viewer**, and **PublicViewer** roles are blocked with a **403 Forbidden** response.
- **No `accountId` from Client**: The tenant scope is determined from the authenticated principal (JWT), not from client-supplied IDs.
- **Accountability**: The viewer provides a history of who changed what, when, and includes snapshots of old and new values for transparency.
- **Metadata Visibility**: Metadata such as `planId` or `sectionKey` is exposed where available to provide context for the changes.
- **Positioning**: Audit history supports **LGU-facing accountability** and internal review. It is not a formal government audit report or national reporting channel.

## Auth/Session Hardening — Slice 1 (Refresh Token Persistence Foundation)

- **Schema added**: New table `public.refresh_tokens` (additive migration `002_add_refresh_tokens.sql`) provides the persistence foundation for future production-grade refresh token rotation, revocation, family tracking, and IP/user-agent auditing.
- **No raw tokens**: Only secure `token_hash` (varchar 128) is ever stored. Raw refresh tokens are never persisted.
- **Slice 1 scope**: This slice adds only the domain entity, EF mapping, DbSet registration, and mapping tests. Login, refresh, logout, `/me` endpoints, cookie issuance, and any runtime auth behavior are **unchanged**.
- **Later slices**: Cookie-based session delivery, refresh token rotation/revocation endpoints, and BFF-style auth hardening will follow in subsequent slices.
- **DB constraints**: Check constraints enforce non-blank hash, expiry > issued, and optional non-blank revoke_reason. Partial unique index prevents duplicate active hashes; filtered indexes support efficient active-token and family queries.
