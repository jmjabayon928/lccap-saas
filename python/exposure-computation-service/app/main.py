from datetime import datetime, timezone
from typing import Any, Dict, List, Literal, Optional

from fastapi import FastAPI
from pydantic import BaseModel, Field

CONTRACT_VERSION = "2026-05-06.v1"


class HealthResponse(BaseModel):
    status: Literal["ok"]
    engineName: Literal["ExposureComputationScaffold"]
    engineVersion: Literal["scaffold"]
    contractVersion: str
    computationSupported: Literal[False]


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
    version="scaffold",
)


@app.get("/health")
def health() -> HealthResponse:
    return HealthResponse(
        status="ok",
        engineName="ExposureComputationScaffold",
        engineVersion="scaffold",
        contractVersion=CONTRACT_VERSION,
        computationSupported=False,
    )


@app.post("/compute/exposure")
def compute_exposure(request: ExposureComputationServiceRequest) -> ExposureComputationServiceResponse:
    # Scaffold-only stub: we never compute, never access the DB, and never fabricate result rows.
    return ExposureComputationServiceResponse(
        success=False,
        engineName="ExposureComputationScaffold",
        engineVersion="scaffold",
        computationRunId=None,
        completedAtUtc=datetime.now(timezone.utc),
        errorCode="EngineUnavailable",
        errorMessage="Exposure computation engine is not configured.",
        diagnostics=ExposureComputationDiagnostics(
            message="Computation endpoint is scaffolded only.",
            warnings=[],
            validationNotes=[],
            geometryFeatureCount=None,
            barangayCount=None,
            criticalFacilityCount=None,
            crsDescription=None,
        ),
        results=[],
    )

