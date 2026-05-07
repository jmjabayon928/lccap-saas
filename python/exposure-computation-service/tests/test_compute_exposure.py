from fastapi.testclient import TestClient

from app.main import app


def _minimal_valid_request_payload() -> dict:
    # Scaffold request payload: must match the Pydantic contract shape, but does not require geo correctness.
    return {
        "jobId": "00000000-0000-0000-0000-000000000001",
        "accountId": "00000000-0000-0000-0000-000000000002",
        "planId": "00000000-0000-0000-0000-000000000003",
        "hazardLayerId": "00000000-0000-0000-0000-000000000004",
        "mode": None,
        "requestedAtUtc": None,
        "requestedByUserId": None,
        "computationVersion": "scaffold",
        "crsPolicy": {
            "sourceType": "Explicit",
            "sourceEpsg": 4326,
            "targetType": "Explicit",
            "targetEpsg": 4326,
            "failOnAmbiguity": True,
        },
        "geometryPolicy": {
            "failOnInvalidGeoJson": True,
            "failOnUnsupportedGeometryTypes": True,
            "failOnEmptyGeometry": True,
            "repairStrategy": "None",
        },
        "hazardLayer": {
            "id": "00000000-0000-0000-0000-000000000004",
            "hazardType": "TestHazard",
            "severity": "High",
            "mapAssetId": None,
        },
        "hazardFeatures": {},
        "barangays": [],
        "criticalFacilities": [],
    }


def test_compute_exposure_returns_engine_unavailable_failure_with_empty_results() -> None:
    client = TestClient(app)

    resp = client.post("/compute/exposure", json=_minimal_valid_request_payload())
    assert resp.status_code == 200

    payload = resp.json()
    assert payload["success"] is False
    assert payload["engineName"] == "ExposureComputationScaffold"
    assert payload["engineVersion"] == "scaffold"
    assert payload["computationRunId"] is None
    assert payload["errorCode"] == "EngineUnavailable"
    assert payload["errorMessage"] == "Exposure computation engine is not configured."
    assert payload["diagnostics"]["message"] == "Computation endpoint is scaffolded only."
    assert payload["diagnostics"]["warnings"] == []
    assert payload["diagnostics"]["validationNotes"] == []
    assert payload["results"] == []
    assert "completedAtUtc" in payload


def test_compute_exposure_response_shape_matches_contract() -> None:
    client = TestClient(app)

    resp = client.post("/compute/exposure", json=_minimal_valid_request_payload())
    assert resp.status_code == 200

    payload = resp.json()
    expected_top_level_keys = {
        "success",
        "engineName",
        "engineVersion",
        "computationRunId",
        "completedAtUtc",
        "errorCode",
        "errorMessage",
        "diagnostics",
        "results",
    }
    assert expected_top_level_keys.issubset(set(payload.keys()))

    diagnostics = payload["diagnostics"]
    expected_diagnostics_keys = {
        "message",
        "warnings",
        "validationNotes",
        "geometryFeatureCount",
        "barangayCount",
        "criticalFacilityCount",
        "crsDescription",
    }
    assert expected_diagnostics_keys.issubset(set(diagnostics.keys()))
    assert payload["results"] == []


def test_compute_exposure_does_not_return_results_when_engine_unavailable() -> None:
    client = TestClient(app)

    resp = client.post("/compute/exposure", json=_minimal_valid_request_payload())
    assert resp.status_code == 200

    payload = resp.json()
    assert payload["success"] is False
    assert payload["results"] == []

