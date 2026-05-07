# Phase 3 Slice 3Y - Manual E2E Exposure Workflow Verification Document

## 1. Purpose
This document provides a clear, repeatable checklist to verify the **real Phase 3 end-to-end exposure workflow** across:

- Python exposure computation service (facility-only point-in-polygon)
- .NET API processing (including the PythonAi adapter wiring when enabled)
- PostgreSQL persistence (`exposure_analysis_jobs`, `exposure_summaries`)
- Frontend exposure readiness panel messaging and summary display

This is a **manual verification** only. It is documentation-only and is intended to prevent “it looks wired” assumptions by validating the real runtime contracts and persistence behavior.

## 2. What this verification proves
If all checks below pass, you have verified that:

- The Python service computes facility-only exposure using:
  - hazard geometries: `Polygon` / `MultiPolygon`
  - explicit CRS: EPSG:4326 only
  - boundary-inclusive containment (facility on boundary counts as exposed)
  - deferred metrics: `exposedAreaHectares`, `exposedPopulation`, `riskScore` are not calculated
- The .NET API calls Python when `PythonAi:Enabled=true`.
- Successful Python computation result rows are persisted into `exposure_summaries`.
- The `exposure_analysis_jobs` row transitions to `status = Completed`.
- `exposure_analysis_jobs.output_json` stores engine + persistence metadata (engineName/version and replace-for-job mode + persisted counts).
- The frontend shows:
  - exposure summaries after a successful run with exposed facilities
  - the completed-zero behavior when the computation succeeds but produces zero stored summaries
- Failures mark the job as `Failed` and do not persist new non-deleted exposure summaries.

## 3. What this verification does not prove
Passing this document does **not** prove:

- Any future metrics (area calculation, population approximation, risk scoring)
- Any barangay polygon intersection logic (not present for facility-only point-in-polygon)
- Production deployment/runtime validation (containers, ingress, CDN, etc.)
- Any automated E2E/CI coverage (this is manual-only; no automated E2E tests are created or run here)

## 4. Runtime prerequisites
Before starting, confirm these are running and reachable:

1. **PostgreSQL**
   - The .NET API must be able to connect using the configured `ConnectionStrings:Postgres` value.
2. **.NET API**
   - The API must be running and serving the endpoints used by the frontend map workspace and exposure readiness panel.
3. **Frontend app**
   - You must be able to open the plan map workspace and use the exposure readiness UI.
4. **Python exposure-computation service**
   - Running locally (or reachable) with `GET /health` and `POST /compute/exposure`.
5. **Authenticated frontend user**
   - The user must have workspace role permissions that allow:
     - `Read` for viewing jobs/summaries
     - `CreateOrEdit` for registering hazard layers, queuing jobs, and processing jobs
6. **Safe local/dev tenant + plan**
   - Use a tenant/plan where it is acceptable to queue and process exposure jobs and persist summaries.

## 5. Runtime configuration
Important: **Do not commit appsettings changes.** Keep PythonAi OFF in committed config and enable it via runtime/environment overrides for the manual test.

### 5.1 Safe overrides to use (recommended)
Set these environment overrides for the `.NET API` process (PowerShell example shown):

```powershell
$env:PythonAi__Enabled="true"
$env:PythonAi__BaseUrl="http://localhost:8000"
$env:PythonAi__TimeoutSeconds="60"
```

Notes:
- `PythonAi__Enabled` must be `true` to force the .NET layer to use the Python adapter.
- `PythonAi__BaseUrl` must match where the Python service is listening.
- `PythonAi__TimeoutSeconds` should be high enough for local runs.

### 5.2 Verify adapter expectations (by behavior, not by code)
You should see Python requests issued by the adapter during job processing (confirmed indirectly by successful persistence + output_json engine metadata, and explicitly by failure behavior when Python is unavailable).

## 6. Start Python service
Use the Python service’s local development steps from `python/exposure-computation-service/README.md`.

From the repository’s Python service folder:

```powershell
cd c:\projects\LCCAP\python\exposure-computation-service

# Create/activate venv (if needed)
python -m venv .venv
.venv\Scripts\Activate.ps1

# Install dependencies (if needed)
python -m pip install -U pip
python -m pip install -e .

# Start the service
uvicorn app.main:app --reload --port 8000
```

Verify health:

```text
http://localhost:8000/health
```

Expected (key fields):
- `status` is `"ok"`
- `engineName` is `"FacilityExposureEngine"`
- `engineVersion` is `"facility-v1"`
- `computationSupported` is `true`

## 7. Direct Python compute smoke test
Before involving .NET/DB/UI, run a direct compute request to confirm that Python, CRS policy, geometry validation, and boundary-inclusive containment are working.

### 7.1 Minimal valid success payload (facility inside polygon)
This payload uses:
- CRS: explicit EPSG:4326
- geometry policy: fail-fast / no repair
- hazard geometry: `FeatureCollection` with `Polygon`
- one facility inside the polygon ring

Use any HTTP client (example shown using `curl`-style JSON).

Request:

```json
{
  "jobId": "00000000-0000-0000-0000-000000000001",
  "accountId": "00000000-0000-0000-0000-000000000002",
  "planId": "00000000-0000-0000-0000-000000000003",
  "hazardLayerId": "00000000-0000-0000-0000-000000000004",
  "mode": null,
  "requestedAtUtc": null,
  "requestedByUserId": null,
  "computationVersion": "facility-v1-test",
  "crsPolicy": {
    "sourceType": "Explicit",
    "sourceEpsg": 4326,
    "targetType": "Explicit",
    "targetEpsg": 4326,
    "failOnAmbiguity": true
  },
  "geometryPolicy": {
    "failOnInvalidGeoJson": true,
    "failOnUnsupportedGeometryTypes": true,
    "failOnEmptyGeometry": true,
    "repairStrategy": "None"
  },
  "hazardLayer": {
    "id": "00000000-0000-0000-0000-000000000004",
    "hazardType": "TestHazard",
    "severity": "High",
    "mapAssetId": null
  },
  "hazardFeatures": {
    "type": "FeatureCollection",
    "features": [
      {
        "type": "Feature",
        "id": "haz-1",
        "geometry": {
          "type": "Polygon",
          "coordinates": [
            [
              [0, 0],
              [0, 10],
              [10, 10],
              [10, 0],
              [0, 0]
            ]
          ]
        },
        "properties": {}
      }
    ]
  },
  "barangays": [],
  "criticalFacilities": [
    {
      "id": "fac-1",
      "barangayId": "bar-1",
      "facilityType": "Hospital",
      "capacity": null,
      "latitude": 5,
      "longitude": 5
    }
  ]
}
```

Endpoint:

```text
POST http://localhost:8000/compute/exposure
```

Expected (key fields):
- `success` is `true`
- `engineName` is `"FacilityExposureEngine"`
- `engineVersion` is `"facility-v1"`
- `results` has 1 row
- In the result row:
  - `exposedAreaHectares` is `null`
  - `exposedPopulation` is `null`
  - `riskScore` is `null`
  - `exposedFacilityCount` is `1`
  - `summaryJson.mode` is `"FacilityOnlyPointInPolygon"`
  - `summaryJson.boundaryPolicy` is `"BoundaryInclusive"`

### 7.2 Optional Python boundary check
To validate boundary-inclusive behavior, move the facility point to a boundary point (e.g., `longitude=0`, `latitude=5`) and confirm it still returns `results.length === 1`.

### 7.3 Optional Python zero-result smoke test
Keep the same polygon and set the facility point clearly outside (e.g., `latitude=20`, `longitude=20`). Expected:
- `success=true`
- `results=[]`

## 8. App data prerequisites
For the full end-to-end run, you must have app data available in the local/dev tenant:

Required:
- A **plan** you can open in the frontend map workspace.
- A **hazard GeoJSON map asset** in that plan workspace (map format must be `GeoJson` so it appears in selection).
- An **active registered HazardLayer** created from that hazard GeoJSON layer.
- **Barangay reference data** such that `barangayCount > 0` in the exposure readiness panel.
- **At least one critical facility** with:
  - non-null `latitude`
  - non-null `longitude`
- For success test:
  - hazard polygon coordinates and facility coordinates must overlap such that the point-in-polygon includes boundary-inclusive containment.

## 9. Frontend success path checklist
This validates the complete flow: hazard registration → job queue → job processing → persistence → UI refresh.

1. Open the plan map workspace in the frontend.
2. In the exposure readiness panel:
   - select the hazard GeoJSON layer (GeoJSON map asset)
3. Register the hazard layer (if not already registered/active).
4. Confirm the panel shows:
   - Hazard layer status: `Ready`
   - Run button enabled (requires `barangayCount > 0` and an active registered hazard layer)
5. Queue an exposure analysis job:
   - click `Run exposure analysis`
   - confirm the recent jobs list shows the job with status `Queued`
6. Process the queued job:
   - click `Process job` for that `Queued` job
7. Verify Completed state:
   - the job transitions to `Completed`
   - the top status message should indicate completion and that stored summaries were refreshed
8. Verify summaries are displayed:
   - the exposure summaries list becomes non-empty
   - each summary card shows:
     - `hazardType`
     - `Facilities: {n}`
9. Complete the DB verification steps in Section 12.

## 10. Frontend zero-result checklist
This validates success with zero persisted summaries and the completed-zero UI messaging.

1. Use the same plan workspace data and configuration as the success path.
2. Ensure your critical facility points are strictly outside the hazard polygon (no boundary touch), **or** otherwise ensure Python skips all facilities (e.g., missing coordinates) so that Python returns `results=[]`.
3. Queue the exposure job and process it.
4. Verify job state:
   - job status becomes `Completed`
5. Verify summaries:
   - exposure summaries list remains empty
6. Verify UI messaging:
   - the panel displays the completed-zero message:
     “Exposure analysis completed with zero stored exposure summaries. No exposed facilities were found for the completed run.”
7. Complete DB verification in Section 12.

## 11. Frontend failure checklist
Use controlled failure modes that are safe and expected.

### 11.1 Failure: Python unavailable / wrong BaseUrl
1. Stop Python service (or temporarily set an incorrect `PythonAi__BaseUrl` override).
2. Ensure the rest of the app data is still valid.
3. Queue and process the same kind of job as the success test.
4. Expected:
   - job status becomes `Failed`
   - UI displays a failure message (job error displayed in the recent jobs list, and/or status message at top)
   - exposure summaries remain empty for that failed job
5. Complete DB verification (job status/error/output_json) in Section 12.

### 11.2 Failure: unsupported hazard geometry type (e.g., `Point`)
1. Prepare/register a hazard GeoJSON with a geometry feature type other than `Polygon` or `MultiPolygon` (e.g., `Point`).
2. Queue and process the job.
3. Expected:
   - job status becomes `Failed`
   - error message indicates invalid/unsupported geometry behavior
   - zero persisted exposure summaries for that job run

## 12. Read-only SQL verification
Use PostgreSQL with **SELECT-only** queries. No UPDATE/DELETE/INSERT in this doc.

Before each run, record the `planId` you are using and capture the `jobId` you process (from the frontend job list).

### 12.1 Latest job for a plan (non-deleted)
```sql
SELECT *
FROM public.exposure_analysis_jobs
WHERE plan_id = :planId
  AND is_deleted = false
ORDER BY created_at_utc DESC
LIMIT 1;
```

### 12.2 Job status + error + output_json fields
```sql
SELECT
  id,
  status,
  error_message,
  created_at_utc,
  started_at_utc,
  completed_at_utc,
  output_json
FROM public.exposure_analysis_jobs
WHERE id = :jobId;
```

### 12.3 output_json engine + persistence metadata (Completed success expectations)
```sql
SELECT
  output_json->>'engineName' AS engine_name,
  output_json->>'engineVersion' AS engine_version,
  output_json->'persistence'->>'mode' AS persistence_mode,
  (output_json->'persistence'->>'persistedSummaryCount')::int AS persisted_summary_count,
  (output_json->>'resultCount')::int AS result_count
FROM public.exposure_analysis_jobs
WHERE id = :jobId;
```

### 12.4 Active summaries for a job
```sql
SELECT
  id,
  exposure_analysis_job_id,
  hazard_layer_id,
  hazard_type,
  severity,
  barangay_id,
  critical_facility_id,
  exposed_area_hectares,
  exposed_facility_count,
  exposed_population,
  risk_score,
  summary_json,
  created_at_utc
FROM public.exposure_summaries
WHERE exposure_analysis_job_id = :jobId
  AND is_deleted = false
ORDER BY created_at_utc ASC;
```

### 12.5 Active summary count (success vs zero-result)
```sql
SELECT COUNT(*)::int AS active_summary_count
FROM public.exposure_summaries
WHERE exposure_analysis_job_id = :jobId
  AND is_deleted = false;
```

### 12.6 Duplicate active summaries check (same job)
```sql
SELECT
  COUNT(*)::int AS active_summary_count,
  COUNT(DISTINCT critical_facility_id)::int AS distinct_facilities
FROM public.exposure_summaries
WHERE exposure_analysis_job_id = :jobId
  AND is_deleted = false;
```

### 12.7 Replace-for-job evidence (active vs archived)
If you re-run a job in a controlled way, verify replace-for-job semantics:
```sql
SELECT
  (SELECT COUNT(*)::int
   FROM public.exposure_summaries
   WHERE exposure_analysis_job_id = :jobId
     AND is_deleted = false) AS active_count,
  (SELECT COUNT(*)::int
   FROM public.exposure_summaries
   WHERE exposure_analysis_job_id = :jobId
     AND is_deleted = true) AS archived_count;
```

## 13. API verification checklist
These checks validate API-level correctness for the UI flow.

### 13.1 GET exposure analysis jobs for the plan
```text
GET /api/plans/{planId}/exposure-analysis-jobs
```
Expected:
- Response includes the most recent job with `status` set appropriately (`Queued`, `Running`, `Completed`, `Failed`)
- `errorMessage` is `null` on success, non-null on failures

### 13.2 POST process a specific job
```text
POST /api/plans/{planId}/exposure-analysis-jobs/{jobId}/process
Body: {}
```
Expected:
- On success:
  - job `status = Completed`
  - `errorMessage = null`
- On failure:
  - job `status = Failed`
  - `errorMessage` present

### 13.3 GET exposure summaries for the plan
```text
GET /api/plans/{planId}/exposure-summaries
```
Expected:
- Non-empty after success-with-exposed-facilities
- Empty after zero-result success

## 14. Expected values
Use these expected values to decide pass/fail quickly.

### 14.1 Success (exposed facility)
- `exposure_analysis_jobs.status = Completed`
- `exposure_analysis_jobs.error_message IS NULL`
- `output_json.engineName = FacilityExposureEngine`
- `output_json.persistence.mode = ReplaceForJob`
- `output_json.persistence.persistedSummaryCount >= 1`
- persisted `exposure_summaries` rows:
  - `exposedAreaHectares = null`
  - `exposedPopulation = null`
  - `riskScore = null`
  - `exposedFacilityCount` matches what the setup expects (often `1` for minimal test)
  - `summaryJson.mode = FacilityOnlyPointInPolygon`

### 14.2 Success (zero exposed facilities)
- `exposure_analysis_jobs.status = Completed`
- `exposure_analysis_jobs.error_message IS NULL`
- `output_json.persistence.persistedSummaryCount = 0`
- active `exposure_summaries` for the job: count `0`
- frontend displays completed-zero message

## 15. Risk controls
To avoid accidental or persistent damage:

- Use a local/dev tenant and plan only.
- Do not change committed config files for this verification.
- Do not commit PythonAi-enabled configuration.
- Do not run write SQL cleanup commands from this doc.
- Record `planId` and `jobId` used for the verification run so you can trace precisely.

## 16. Pass/fail checklist
Mark each item:

- [ ] Python health passed (`/health` returns FacilityExposureEngine + facility-v1)
- [ ] Python direct compute smoke test passed (success with expected facility exposure)
- [ ] .NET API started with PythonAi enabled via runtime/env overrides
- [ ] Frontend success path passed (job completed + summaries displayed)
- [ ] DB success verification passed (job Completed + correct output_json + active summaries present)
- [ ] Zero-result path passed (job Completed + zero summaries + completed-zero UI message)
- [ ] Failure path passed (job Failed + no summaries + correct UI error display)
- [ ] No duplicate active summaries found for the processed job
- [ ] No outdated UI copy or wrong-state messaging observed

## 17. Follow-up if verification fails
If any required check fails:

1. Categorize the failure (Python unreachable, compute validation, adapter/wiring, persistence, or frontend display).
2. Capture the following artifacts for debugging:
   - Python logs (service console output)
   - .NET API logs (service console output)
   - `planId` and `jobId` used for the run
   - `output_json` from `exposure_analysis_jobs` (SELECT-only)
   - failed job `errorMessage` shown in the frontend/UI and confirmed via DB/API
3. Do not change code until the failure is categorized and the root cause is confirmed.

