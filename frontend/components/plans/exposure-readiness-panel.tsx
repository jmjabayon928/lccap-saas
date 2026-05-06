import type { ReactElement } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import type { MapAssetSummary } from "@/types/plans";

interface ExposureReadinessPanelProps {
  readonly selectedMapAssetId: string | null;
  readonly mapAssets: readonly MapAssetSummary[];
  readonly hazardLayerMapAssetIds: readonly string[];
  readonly barangayCount: number;
  readonly criticalFacilityCount: number;
  readonly evacuationSiteCount: number;
}

function computeDisplayedHazardLayer(
  selectedMapAssetId: string | null,
  mapAssets: readonly MapAssetSummary[],
  hazardLayerMapAssetIds: readonly string[]
): MapAssetSummary | null {
  const selectedLayer = selectedMapAssetId
    ? mapAssets.find((layer) => layer.id === selectedMapAssetId) ?? null
    : null;

  const selectedHazardLayer =
    selectedLayer && hazardLayerMapAssetIds.includes(selectedLayer.id) ? selectedLayer : null;

  if (selectedHazardLayer) {
    return selectedHazardLayer;
  }

  return mapAssets.find((layer) => hazardLayerMapAssetIds.includes(layer.id)) ?? null;
}

export function ExposureReadinessPanel({
  selectedMapAssetId,
  mapAssets,
  hazardLayerMapAssetIds,
  barangayCount,
  criticalFacilityCount,
  evacuationSiteCount
}: ExposureReadinessPanelProps): ReactElement {
  const displayedHazardLayer = computeDisplayedHazardLayer(selectedMapAssetId, mapAssets, hazardLayerMapAssetIds);
  const hazardExists = displayedHazardLayer !== null;

  const hazardStatus = hazardExists ? "Ready" : "Missing";
  const hazardLayerName = hazardExists ? displayedHazardLayer.name : "No hazard layer selected";
  const hazardFeatureCount = hazardExists ? displayedHazardLayer.featureCount : 0;

  let readinessMessage: string;
  if (!hazardExists) {
    readinessMessage = "Upload or select a GeoJSON hazard layer before running exposure analysis.";
  } else if (barangayCount === 0) {
    readinessMessage = "Add barangay reference data before running exposure analysis.";
  } else {
    readinessMessage = "Reference data is ready for a future exposure analysis run.";
  }

  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-base">Exposure readiness</CardTitle>
        <CardDescription>Read-only placeholder for future exposure analysis setup.</CardDescription>
      </CardHeader>
      <CardContent className="space-y-3">
        <div className="space-y-1">
          <div className="text-sm font-medium text-slate-900">Hazard layer status: {hazardStatus}</div>
          <div className="text-sm text-muted-foreground">Hazard layer name: {hazardLayerName}</div>
          <div className="text-sm text-muted-foreground">Hazard feature count: {hazardFeatureCount}</div>
        </div>

        <div className="space-y-1">
          <div className="text-sm text-muted-foreground">Barangay references: {barangayCount}</div>
          <div className="text-sm text-muted-foreground">Critical facilities: {criticalFacilityCount}</div>
          <div className="text-sm text-muted-foreground">Evacuation sites: {evacuationSiteCount}</div>
        </div>

        <p className="text-sm">{readinessMessage}</p>

        <div className="space-y-1">
          <Button type="button" disabled>
            Run exposure analysis
          </Button>
          <p className="text-xs text-muted-foreground">Exposure analysis will be enabled in a later Phase 3 slice.</p>
        </div>
      </CardContent>
    </Card>
  );
}

