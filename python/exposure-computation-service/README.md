# Exposure Computation Service (Scaffold)

This scaffold service exposes only `GET /health`. It does **not** perform exposure computation, does **not** read or write the database, and does **not** create `ExposureSummary` rows.

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

