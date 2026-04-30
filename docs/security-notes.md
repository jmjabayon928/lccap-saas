# Security notes (MVP)

This document summarizes current MVP security rules and planned hardening. It is not a penetration-test report or formal threat model.

## Tenant isolation via `account_id`

- Each LGU (tenant) is represented as an **Account**; **`account_id` is the tenant boundary**.
- Application behavior must not read or write data across tenant boundaries.

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
