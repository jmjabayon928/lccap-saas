# Exposure Computation Service (Scaffold)

This scaffold service exposes:
- `GET /health`
- `POST /compute/exposure` (scaffold-only contract stub)

It does **not** perform exposure computation, does **not** read or write the database, and does **not** create `ExposureSummary` rows.

## `POST /compute/exposure`

This endpoint is scaffold-only and returns a structured safe failure:
- HTTP status: `200`
- `success`: `false`
- `errorCode`: `EngineUnavailable`
- `results`: `[]`

No exposure computation is performed and no database access is performed.

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

