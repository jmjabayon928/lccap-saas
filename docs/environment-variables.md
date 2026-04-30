# Environment variables

This document describes configuration that can be supplied via environment variables (double-underscore nesting matches `appsettings` sections) or via `appsettings*.json`. Do not commit real secrets.

## API runtime

| Variable | Required | Used by | Safe local example | Production |
|----------|----------|---------|--------------------|------------|
| `ASPNETCORE_ENVIRONMENT` | Recommended | ASP.NET Core host, logging, optional dev-only behavior | `Development` | Set to `Production` (or your deployment standard). Never run production with `Development`. |

## Database

| Variable | Required | Used by | Safe local example | Production |
|----------|----------|---------|--------------------|------------|
| `ConnectionStrings__Postgres` | Required for a running API | EF Core / Infrastructure data access | `Host=localhost;Port=5432;Database=lccap_dev;Username=postgres;Password=CHANGE_ME` | Use a strong password, restricted network access, and secret storage (Key Vault, managed identity, platform secrets). Never log connection strings. |
| `ConnectionStrings__DefaultConnection` | Optional | N/A unless you add a `DefaultConnection` entry to configuration | Not used by default in this repo | If you rename configuration keys, align env vars with the exact key names in appsettings. |

**Note:** Default `appsettings*.json` files use the connection name **`Postgres`**, not `DefaultConnection`.

## JWT / Auth

| Variable | Required | Used by | Safe local example | Production |
|----------|----------|---------|--------------------|------------|
| `Jwt__Issuer` | Required for token validation | JWT bearer authentication | `Lccap.Api.Dev` | Use stable issuer URLs or logical issuer names agreed for your deployment. |
| `Jwt__Audience` | Required for token validation | JWT bearer authentication | `Lccap.Client.Dev` | Must match tokens issued by your identity flow. |
| `Jwt__SigningKey` | Required for signing/validation | JWT bearer authentication | Obvious dev-only placeholder string (≥ 32 characters for HS256 key material) | **Secret:** cryptographically random key, managed outside source control (rotation, least privilege). |
| `Jwt__ExpirationMinutes` | Optional | Token lifetime when issuing JWTs | `60` | Tune for your security posture and UX; shorter is safer for sensitive tenants. |

## File storage

| Variable | Required | Used by | Safe local example | Production |
|----------|----------|---------|--------------------|------------|
| `FileStorage__RootPath` | Recommended | Local file storage service | `data/dev-files` | Use an absolute path on a secured volume; avoid world-writable directories. |
| `FileStorage__MaxUploadBytes` | Optional | Upload size limits | `10485760` (10 MiB) | Set per product/compliance limits. |
| `FileStorage__AllowedExtensions__0`, `__1`, … | Optional | Upload allowlist | `.pdf`, `.docx`, … | Restrict to required types; review regularly. |

Array-style settings use indexed suffixes: `FileStorage__AllowedExtensions__0`, `FileStorage__AllowedExtensions__1`, etc.

## Python AI (planned)

| Variable | Required | Used by | Safe local example | Production |
|----------|----------|---------|--------------------|------------|
| `PythonAi__BaseUrl` | Optional until AI integration is active | Planned HTTP client to Python FastAPI service | `http://localhost:8000` | Use internal DNS, TLS, and network policies appropriate to your platform. |
| `PythonAi__TimeoutSeconds` | Optional | Planned outbound HTTP timeouts | `60` | Set high enough for jobs, low enough to avoid resource exhaustion. |

## Frontend (planned)

| Variable | Required | Used by | Safe local example | Production |
|----------|----------|---------|--------------------|------------|
| TBD | — | Next.js and related tooling (not yet in this repository) | e.g. public API base URL for browser calls | Use build-time or runtime config per hosting provider; never embed service secrets in the browser bundle. |

Document concrete names when the frontend project lands.

## Production safety notes

- Treat **JWT signing keys** and **database passwords** as high-value secrets: no source control, no screenshots in tickets, no shared chat pastes.
- Prefer **platform secret managers** and short-lived credentials where available.
- **Rotate** signing keys and DB credentials on a defined process (incident, staff change, scheduled).
- Keep **environment-specific** values out of shared defaults; use deployment-specific configuration layers.
- Review **CORS**, **TLS termination**, and **network boundaries** before exposing any environment beyond local dev.
