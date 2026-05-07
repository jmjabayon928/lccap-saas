from datetime import datetime, timezone
from typing import Any, Dict, List, Literal, Optional

from fastapi import FastAPI
from pydantic import BaseModel, Field
from shapely.geometry import Point, shape

CONTRACT_VERSION = "2026-05-06.v1"


class HealthResponse(BaseModel):
    status: Literal["ok"]
    engineName: Literal["FacilityExposureEngine"]
    engineVersion: Literal["facility-v1"]
    contractVersion: str
    computationSupported: Literal[True]


class ExposureComputationCrsPolicy(BaseModel):
    sourceType: str
    sourceEpsg: Optional[int]
    targetType: str
    targetEpsg: Optional[int]
    failOnAmbiguity: bool


class ExposureComputationGeometryPolicy(BaseModel):
    failOnInvalidGeoJson: bool
    failOnUnsupportedGeometryTypes: bool
    failOnEmptyGeometry: bool
    repairStrategy: str


class ExposureComputationHazardLayerPayload(BaseModel):
    id: str
    hazardType: str
    severity: str
    mapAssetId: Optional[str]


class ExposureComputationBarangayPayload(BaseModel):
    id: str
    population: Optional[int]
    boundaryGeoJson: Optional[Dict[str, Any]]
    latitude: Optional[float]
    longitude: Optional[float]


class ExposureComputationCriticalFacilityPayload(BaseModel):
    id: str
    barangayId: Optional[str]
    facilityType: str
    capacity: Optional[int]
    latitude: Optional[float]
    longitude: Optional[float]


class ExposureComputationServiceRequest(BaseModel):
    jobId: str
    accountId: str
    planId: str
    hazardLayerId: str
    mode: Optional[str] = None
    requestedAtUtc: Optional[str] = None
    requestedByUserId: Optional[str] = None
    computationVersion: str
    crsPolicy: ExposureComputationCrsPolicy
    geometryPolicy: ExposureComputationGeometryPolicy
    hazardLayer: ExposureComputationHazardLayerPayload
    hazardFeatures: Dict[str, Any]
    barangays: List[ExposureComputationBarangayPayload]
    criticalFacilities: List[ExposureComputationCriticalFacilityPayload]


class ExposureComputationDiagnostics(BaseModel):
    message: Optional[str] = None
    warnings: List[str] = Field(default_factory=list)
    validationNotes: List[str] = Field(default_factory=list)
    geometryFeatureCount: Optional[int] = None
    barangayCount: Optional[int] = None
    criticalFacilityCount: Optional[int] = None
    crsDescription: Optional[str] = None


class ExposureComputationServiceResultRow(BaseModel):
    barangayId: Optional[str]
    criticalFacilityId: Optional[str]
    hazardLayerId: Optional[str]
    hazardType: str
    severity: Optional[str]
    exposedAreaHectares: Optional[float]
    exposedFacilityCount: int
    exposedPopulation: Optional[int]
    riskScore: Optional[float]
    summaryJson: Optional[Dict[str, Any]]


class ExposureComputationServiceResponse(BaseModel):
    success: bool
    engineName: str
    engineVersion: str
    computationRunId: Optional[str]
    completedAtUtc: datetime
    errorCode: Optional[str]
    errorMessage: Optional[str]
    diagnostics: ExposureComputationDiagnostics
    results: List[ExposureComputationServiceResultRow] = Field(default_factory=list)


app = FastAPI(
    title="LCCAP Exposure Computation Service",
    version="facility-v1",
)


ENGINE_NAME = "FacilityExposureEngine"
ENGINE_VERSION = "facility-v1"


def _make_diagnostics(
    *,
    message: Optional[str] = None,
    warnings: Optional[List[str]] = None,
    validation_notes: Optional[List[str]] = None,
    geometry_feature_count: Optional[int] = None,
    barangay_count: Optional[int] = None,
    critical_facility_count: Optional[int] = None,
    crs_description: Optional[str] = None,
) -> ExposureComputationDiagnostics:
    return ExposureComputationDiagnostics(
        message=message,
        warnings=warnings or [],
        validationNotes=validation_notes or [],
        geometryFeatureCount=geometry_feature_count,
        barangayCount=barangay_count,
        criticalFacilityCount=critical_facility_count,
        crsDescription=crs_description,
    )


def _make_response(
    *,
    success: bool,
    error_code: Optional[str],
    error_message: Optional[str],
    diagnostics: ExposureComputationDiagnostics,
    results: Optional[List[ExposureComputationServiceResultRow]] = None,
) -> ExposureComputationServiceResponse:
    return ExposureComputationServiceResponse(
        success=success,
        engineName=ENGINE_NAME,
        engineVersion=ENGINE_VERSION,
        computationRunId=None,
        completedAtUtc=datetime.now(timezone.utc),
        errorCode=error_code,
        errorMessage=error_message,
        diagnostics=diagnostics,
        results=results or [],
    )


def _fail(
    *,
    error_code: str,
    error_message: str,
    message: Optional[str] = None,
    warnings: Optional[List[str]] = None,
    validation_notes: Optional[List[str]] = None,
    geometry_feature_count: Optional[int] = None,
    barangay_count: Optional[int] = None,
    critical_facility_count: Optional[int] = None,
    crs_description: Optional[str] = None,
) -> ExposureComputationServiceResponse:
    diagnostics = _make_diagnostics(
        message=message,
        warnings=warnings,
        validation_notes=validation_notes,
        geometry_feature_count=geometry_feature_count,
        barangay_count=barangay_count,
        critical_facility_count=critical_facility_count,
        crs_description=crs_description,
    )
    return _make_response(
        success=False,
        error_code=error_code,
        error_message=error_message,
        diagnostics=diagnostics,
        results=[],
    )


@app.get("/health")
def health() -> HealthResponse:
    return HealthResponse(
        status="ok",
        engineName=ENGINE_NAME,
        engineVersion=ENGINE_VERSION,
        contractVersion=CONTRACT_VERSION,
        computationSupported=True,
    )


@app.post("/compute/exposure")
def compute_exposure(request: ExposureComputationServiceRequest) -> ExposureComputationServiceResponse:
    # CRS policy (explicit EPSG:4326 only; no transform)
    crs = request.crsPolicy
    if not (
        crs.sourceType == "Explicit"
        and crs.targetType == "Explicit"
        and crs.sourceEpsg == 4326
        and crs.targetEpsg == 4326
        and crs.failOnAmbiguity is True
    ):
        return _fail(
            error_code="CrsUnsupported",
            error_message="Only explicit EPSG:4326 CRS is supported for facility exposure computation.",
            message="CRS policy rejected for facility exposure computation.",
            barangay_count=len(request.barangays),
            critical_facility_count=len(request.criticalFacilities),
        )

    # Geometry policy (fail-fast/no-repair)
    geo_policy = request.geometryPolicy
    if not (
        geo_policy.failOnInvalidGeoJson is True
        and geo_policy.failOnUnsupportedGeometryTypes is True
        and geo_policy.failOnEmptyGeometry is True
        and geo_policy.repairStrategy == "None"
    ):
        return _fail(
            error_code="ValidationFailed",
            error_message="Unsupported geometry policy for facility exposure computation.",
            message="Geometry policy rejected for facility exposure computation.",
            barangay_count=len(request.barangays),
            critical_facility_count=len(request.criticalFacilities),
            crs_description="EPSG:4326 (Explicit)",
        )

    # Parse hazard features (must be a valid GeoJSON FeatureCollection)
    hazard_features = request.hazardFeatures
    if not isinstance(hazard_features, dict) or hazard_features.get("type") != "FeatureCollection":
        return _fail(
            error_code="InvalidGeoJson",
            error_message="Hazard features must be a valid GeoJSON FeatureCollection.",
            message="HazardFeatures GeoJSON was invalid.",
            barangay_count=len(request.barangays),
            critical_facility_count=len(request.criticalFacilities),
            crs_description="EPSG:4326 (Explicit)",
        )

    features = hazard_features.get("features")
    if not isinstance(features, list):
        return _fail(
            error_code="InvalidGeoJson",
            error_message="Hazard features must be a valid GeoJSON FeatureCollection.",
            message="HazardFeatures GeoJSON was invalid.",
            barangay_count=len(request.barangays),
            critical_facility_count=len(request.criticalFacilities),
            crs_description="EPSG:4326 (Explicit)",
        )

    geometry_feature_count = len(features)
    hazard_geoms: List[tuple[Optional[str], Any]] = []

    for feature in features:
        if not isinstance(feature, dict):
            return _fail(
                error_code="InvalidGeoJson",
                error_message="Hazard feature contains invalid GeoJSON geometry.",
                message="Hazard feature was not a valid GeoJSON Feature object.",
                geometry_feature_count=geometry_feature_count,
                barangay_count=len(request.barangays),
                critical_facility_count=len(request.criticalFacilities),
                crs_description="EPSG:4326 (Explicit)",
            )

        geom_obj = feature.get("geometry")
        if not isinstance(geom_obj, dict):
            return _fail(
                error_code="InvalidGeoJson",
                error_message="Hazard feature contains invalid GeoJSON geometry.",
                message="Hazard feature geometry was missing or invalid.",
                geometry_feature_count=geometry_feature_count,
                barangay_count=len(request.barangays),
                critical_facility_count=len(request.criticalFacilities),
                crs_description="EPSG:4326 (Explicit)",
            )

        geom_type = geom_obj.get("type")
        if geom_type not in ("Polygon", "MultiPolygon"):
            return _fail(
                error_code="UnsupportedGeometry",
                error_message="Only Polygon and MultiPolygon hazard geometries are supported.",
                message=f"Unsupported hazard geometry type: {geom_type}.",
                geometry_feature_count=geometry_feature_count,
                barangay_count=len(request.barangays),
                critical_facility_count=len(request.criticalFacilities),
                crs_description="EPSG:4326 (Explicit)",
            )

        try:
            hazard_geom = shape(geom_obj)
        except Exception:
            return _fail(
                error_code="InvalidGeoJson",
                error_message="Hazard feature contains invalid GeoJSON geometry.",
                message="Hazard feature geometry could not be parsed.",
                geometry_feature_count=geometry_feature_count,
                barangay_count=len(request.barangays),
                critical_facility_count=len(request.criticalFacilities),
                crs_description="EPSG:4326 (Explicit)",
            )

        if geo_policy.failOnEmptyGeometry and hazard_geom.is_empty:
            return _fail(
                error_code="ValidationFailed",
                error_message="Hazard feature geometry is empty.",
                message="Hazard feature geometry was empty.",
                geometry_feature_count=geometry_feature_count,
                barangay_count=len(request.barangays),
                critical_facility_count=len(request.criticalFacilities),
                crs_description="EPSG:4326 (Explicit)",
            )

        # Fail-fast invalid geometry; do not repair/buffer
        if geo_policy.failOnInvalidGeoJson and not hazard_geom.is_valid:
            return _fail(
                error_code="InvalidGeoJson",
                error_message="Hazard feature contains invalid GeoJSON geometry.",
                message="Hazard feature geometry was invalid (not repaired).",
                geometry_feature_count=geometry_feature_count,
                barangay_count=len(request.barangays),
                critical_facility_count=len(request.criticalFacilities),
                crs_description="EPSG:4326 (Explicit)",
            )

        feature_id = feature.get("id")
        hazard_geoms.append((str(feature_id) if feature_id is not None else None, hazard_geom))

    # Point-in-polygon: boundary-inclusive (covers)
    results: List[ExposureComputationServiceResultRow] = []
    warnings: List[str] = []

    for facility in request.criticalFacilities:
        if facility.latitude is None or facility.longitude is None:
            warnings.append(f"Skipped critical facility {facility.id} due to missing latitude/longitude.")
            continue

        point = Point(float(facility.longitude), float(facility.latitude))

        matched_ids: List[str] = []
        for hazard_feature_id, hazard_geom in hazard_geoms:
            if hazard_geom.covers(point):
                if hazard_feature_id is not None:
                    matched_ids.append(hazard_feature_id)

        if matched_ids:
            results.append(
                ExposureComputationServiceResultRow(
                    barangayId=facility.barangayId,
                    criticalFacilityId=facility.id,
                    hazardLayerId=request.hazardLayer.id,
                    hazardType=request.hazardLayer.hazardType,
                    severity=request.hazardLayer.severity,
                    exposedAreaHectares=None,
                    exposedFacilityCount=1,
                    exposedPopulation=None,
                    riskScore=None,
                    summaryJson={
                        "mode": "FacilityOnlyPointInPolygon",
                        "boundaryPolicy": "BoundaryInclusive",
                        "matchedHazardFeatureIds": matched_ids,
                    },
                )
            )

    if len(results) == 0:
        return _make_response(
            success=True,
            error_code=None,
            error_message=None,
            diagnostics=_make_diagnostics(
                message="Facility-only point-in-polygon computation completed. No facilities were exposed.",
                warnings=warnings,
                validation_notes=[
                    "Supported hazard geometry types: Polygon, MultiPolygon.",
                    "exposedAreaHectares, exposedPopulation, and riskScore are deferred.",
                ],
                geometry_feature_count=geometry_feature_count,
                barangay_count=len(request.barangays),
                critical_facility_count=len(request.criticalFacilities),
                crs_description="EPSG:4326 (Explicit)",
            ),
            results=[],
        )

    return _make_response(
        success=True,
        error_code=None,
        error_message=None,
        diagnostics=_make_diagnostics(
            message="Facility-only point-in-polygon computation completed.",
            warnings=warnings,
            validation_notes=[
                "Supported hazard geometry types: Polygon, MultiPolygon.",
                "exposedAreaHectares, exposedPopulation, and riskScore are deferred.",
            ],
            geometry_feature_count=geometry_feature_count,
            barangay_count=len(request.barangays),
            critical_facility_count=len(request.criticalFacilities),
            crs_description="EPSG:4326 (Explicit)",
        ),
        results=results,
    )

