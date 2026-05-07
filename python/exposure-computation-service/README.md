# Exposure Computation Service (Facility Exposure)

This scaffold service exposes:
- `GET /health`
- `POST /compute/exposure` (facility-only point-in-polygon exposure computation)

It performs facility-only exposure computation and does **not** read or write the database, and does **not** create `ExposureSummary` rows.

## `POST /compute/exposure`

This endpoint computes whether each critical facility point is inside or on the boundary of hazard polygons:
- Supported hazard geometries: `Polygon`, `MultiPolygon`
- CRS required: explicit `EPSG:4326` (no transformation)
- Boundary-inclusive containment (a point on the polygon boundary counts as exposed)
- Deferred metrics: no exposed area (`exposedAreaHectares`), population approximation (`exposedPopulation`), or risk score (`riskScore`)
- No DB access and no exposure summary persistence (the .NET layer owns persistence later)

## Local development

1. Create and activate a venv
   - PowerShell:
     - `python -m venv .venv`
     - `.venv\Scripts\Activate.ps1`
2. Install dependencies
   - `python -m pip install -U pip`
   - `python -m pip install -e .`
3. Run the service
   - `uvicorn app.main:app --reload --port 8000`
4. Health check URL
   - `http://localhost:8000/health`
5. Run tests
   - `pytest`

