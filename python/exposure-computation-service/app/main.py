from typing import Literal

from fastapi import FastAPI
from pydantic import BaseModel

CONTRACT_VERSION = "2026-05-06.v1"


class HealthResponse(BaseModel):
    status: Literal["ok"]
    engineName: Literal["ExposureComputationScaffold"]
    engineVersion: Literal["scaffold"]
    contractVersion: str
    computationSupported: Literal[False]


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

