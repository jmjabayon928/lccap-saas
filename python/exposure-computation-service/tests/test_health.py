from fastapi.testclient import TestClient

from app.main import CONTRACT_VERSION, app


def test_health_returns_expected_facility_exposure_metadata() -> None:
    client = TestClient(app)

    resp = client.get("/health")
    assert resp.status_code == 200

    payload = resp.json()
    assert payload["status"] == "ok"
    assert payload["engineName"] == "FacilityExposureEngine"
    assert payload["engineVersion"] == "facility-v1"
    assert payload["contractVersion"] == CONTRACT_VERSION
    assert payload["computationSupported"] is True

