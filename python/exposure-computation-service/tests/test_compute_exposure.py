from fastapi.testclient import TestClient

from app.main import app


def _base_request_payload() -> dict:
    # Request payload that satisfies the contract shape; hazard/facility geo correctness is controlled per test.
    return {
        "jobId": "00000000-0000-0000-0000-000000000001",
        "accountId": "00000000-0000-0000-0000-000000000002",
        "planId": "00000000-0000-0000-0000-000000000003",
        "hazardLayerId": "00000000-0000-0000-0000-000000000004",
        "mode": None,
        "requestedAtUtc": None,
        "requestedByUserId": None,
        "computationVersion": "facility-v1-test",
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
        "hazardFeatures": {"type": "FeatureCollection", "features": []},
        "barangays": [],
        "criticalFacilities": [],
    }


def _polygon_hazard(feature_id: str, ring_coords: list[list[float]]) -> dict:
    # ring_coords must be closed: first point equals last point.
    return {
        "type": "FeatureCollection",
        "features": [
            {
                "type": "Feature",
                "id": feature_id,
                "geometry": {
                    "type": "Polygon",
                    "coordinates": [ring_coords],
                },
                "properties": {},
            }
        ],
    }


def _multipolygon_hazard(feature_id: str, polygons_coords: list[list[list[float]]]) -> dict:
    return {
        "type": "FeatureCollection",
        "features": [
            {
                "type": "Feature",
                "id": feature_id,
                "geometry": {
                    "type": "MultiPolygon",
                    "coordinates": polygons_coords,
                },
                "properties": {},
            }
        ],
    }


def _facility(facility_id: str, barangay_id: str | None, latitude: float | None, longitude: float | None) -> dict:
    return {
        "id": facility_id,
        "barangayId": barangay_id,
        "facilityType": "Hospital",
        "capacity": None,
        "latitude": latitude,
        "longitude": longitude,
    }


def test_compute_exposure_returns_exposed_facility_inside_polygon() -> None:
    client = TestClient(app)

    payload = _base_request_payload()
    payload["hazardFeatures"] = _polygon_hazard(
        "haz-1",
        [[0, 0], [0, 10], [10, 10], [10, 0], [0, 0]],
    )
    payload["criticalFacilities"] = [
        _facility("fac-1", "bar-1", latitude=5, longitude=5),
    ]

    resp = client.post("/compute/exposure", json=payload)
    assert resp.status_code == 200

    body = resp.json()
    assert body["success"] is True
    assert body["engineName"] == "FacilityExposureEngine"
    assert body["engineVersion"] == "facility-v1"
    assert len(body["results"]) == 1
    row = body["results"][0]
    assert row["criticalFacilityId"] == "fac-1"
    assert row["hazardType"] == "TestHazard"
    assert row["exposedAreaHectares"] is None
    assert row["exposedPopulation"] is None
    assert row["riskScore"] is None
    assert row["exposedFacilityCount"] == 1


def test_compute_exposure_excludes_facility_outside_polygon() -> None:
    client = TestClient(app)

    payload = _base_request_payload()
    payload["hazardFeatures"] = _polygon_hazard(
        "haz-1",
        [[0, 0], [0, 10], [10, 10], [10, 0], [0, 0]],
    )
    payload["criticalFacilities"] = [
        _facility("fac-1", "bar-1", latitude=20, longitude=20),
    ]

    resp = client.post("/compute/exposure", json=payload)
    assert resp.status_code == 200

    body = resp.json()
    assert body["success"] is True
    assert body["results"] == []


def test_compute_exposure_counts_boundary_facility_as_exposed() -> None:
    client = TestClient(app)

    payload = _base_request_payload()
    payload["hazardFeatures"] = _polygon_hazard(
        "haz-1",
        [[0, 0], [0, 10], [10, 10], [10, 0], [0, 0]],
    )
    # Point on the left boundary x=0
    payload["criticalFacilities"] = [
        _facility("fac-1", "bar-1", latitude=5, longitude=0),
    ]

    resp = client.post("/compute/exposure", json=payload)
    assert resp.status_code == 200

    body = resp.json()
    assert body["success"] is True
    assert len(body["results"]) == 1


def test_compute_exposure_skips_facility_missing_coordinates_with_warning() -> None:
    client = TestClient(app)

    payload = _base_request_payload()
    payload["hazardFeatures"] = _polygon_hazard(
        "haz-1",
        [[0, 0], [0, 10], [10, 10], [10, 0], [0, 0]],
    )
    payload["criticalFacilities"] = [
        _facility("fac-1", "bar-1", latitude=5, longitude=5),
        _facility("fac-2", "bar-2", latitude=None, longitude=5),
    ]

    resp = client.post("/compute/exposure", json=payload)
    assert resp.status_code == 200

    body = resp.json()
    assert body["success"] is True
    assert len(body["results"]) == 1
    assert any("fac-2" in w for w in body["diagnostics"]["warnings"])


def test_compute_exposure_rejects_unsupported_hazard_geometry() -> None:
    client = TestClient(app)

    payload = _base_request_payload()
    payload["hazardFeatures"] = {
        "type": "FeatureCollection",
        "features": [
            {
                "type": "Feature",
                "id": "haz-1",
                "geometry": {"type": "Point", "coordinates": [0, 0]},
                "properties": {},
            }
        ],
    }
    payload["criticalFacilities"] = [
        _facility("fac-1", "bar-1", latitude=5, longitude=5),
    ]

    resp = client.post("/compute/exposure", json=payload)
    assert resp.status_code == 200

    body = resp.json()
    assert body["success"] is False
    assert body["errorCode"] == "UnsupportedGeometry"
    assert body["results"] == []


def test_compute_exposure_rejects_invalid_geojson() -> None:
    client = TestClient(app)

    payload = _base_request_payload()
    # Not a valid FeatureCollection: missing "features" list
    payload["hazardFeatures"] = {"type": "FeatureCollection"}
    payload["criticalFacilities"] = [
        _facility("fac-1", "bar-1", latitude=5, longitude=5),
    ]

    resp = client.post("/compute/exposure", json=payload)
    assert resp.status_code == 200

    body = resp.json()
    assert body["success"] is False
    assert body["errorCode"] == "InvalidGeoJson"
    assert body["results"] == []


def test_compute_exposure_supports_multipolygon() -> None:
    client = TestClient(app)

    payload = _base_request_payload()
    payload["hazardFeatures"] = _multipolygon_hazard(
        "haz-1",
        polygons_coords=[
            [[[0, 0], [0, 10], [10, 10], [10, 0], [0, 0]]],
            [[[20, 20], [20, 30], [30, 30], [30, 20], [20, 20]]],
        ],
    )
    payload["criticalFacilities"] = [
        _facility("fac-1", "bar-1", latitude=5, longitude=5),
    ]

    resp = client.post("/compute/exposure", json=payload)
    assert resp.status_code == 200

    body = resp.json()
    assert body["success"] is True
    assert len(body["results"]) == 1


def test_compute_exposure_rejects_non_epsg_4326_crs() -> None:
    client = TestClient(app)

    payload = _base_request_payload()
    payload["crsPolicy"]["sourceEpsg"] = 3857
    payload["crsPolicy"]["targetEpsg"] = 3857
    payload["hazardFeatures"] = _polygon_hazard(
        "haz-1",
        [[0, 0], [0, 10], [10, 10], [10, 0], [0, 0]],
    )
    payload["criticalFacilities"] = [
        _facility("fac-1", "bar-1", latitude=5, longitude=5),
    ]

    resp = client.post("/compute/exposure", json=payload)
    assert resp.status_code == 200

    body = resp.json()
    assert body["success"] is False
    assert body["errorCode"] == "CrsUnsupported"
    assert body["results"] == []


def test_compute_exposure_diagnostics_counts_are_populated() -> None:
    client = TestClient(app)

    payload = _base_request_payload()
    payload["barangays"] = [
        {
            "id": "bar-1",
            "population": 100,
            "boundaryGeoJson": None,
            "latitude": 10.0,
            "longitude": 120.0,
        },
        {
            "id": "bar-2",
            "population": 200,
            "boundaryGeoJson": None,
            "latitude": 11.0,
            "longitude": 121.0,
        },
    ]
    payload["hazardFeatures"] = _polygon_hazard(
        "haz-1",
        [[0, 0], [0, 10], [10, 10], [10, 0], [0, 0]],
    )
    payload["criticalFacilities"] = [
        _facility("fac-1", "bar-1", latitude=5, longitude=5),
        _facility("fac-2", "bar-2", latitude=6, longitude=6),
    ]

    resp = client.post("/compute/exposure", json=payload)
    assert resp.status_code == 200

    body = resp.json()
    assert body["success"] is True
    diagnostics = body["diagnostics"]
    assert diagnostics["geometryFeatureCount"] == 1
    assert diagnostics["barangayCount"] == 2
    assert diagnostics["criticalFacilityCount"] == 2
    assert diagnostics["crsDescription"] == "EPSG:4326 (Explicit)"

