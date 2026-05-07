from fastapi.testclient import TestClient

from app.main import CONTRACT_VERSION, app


def test_health_returns_expected_scaffold_metadata() -> None:
    client = TestClient(app)

    resp = client.get("/health")
    assert resp.status_code == 200

    payload = resp.json()
    assert payload["status"] == "ok"
    assert payload["engineName"] == "ExposureComputationScaffold"
    assert payload["engineVersion"] == "scaffold"
    assert payload["contractVersion"] == CONTRACT_VERSION
    assert payload["computationSupported"] is False

