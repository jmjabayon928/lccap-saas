import type { ReactElement } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import type { ExposureAnalysisJobSummary, HazardLayerSummary, MapAssetSummary, ExposureSummary } from "@/types/plans";

interface ExposureReadinessPanelProps {
  readonly planId: string;
  readonly selectedMapAssetId: string | null;
  readonly mapAssets: readonly MapAssetSummary[];
  readonly hazardLayerMapAssetIds: readonly string[];
  readonly hazardLayers: readonly HazardLayerSummary[];
  readonly exposureJobs: readonly ExposureAnalysisJobSummary[];
  readonly exposureSummaries: readonly ExposureSummary[];
  readonly barangayCount: number;
  readonly criticalFacilityCount: number;
  readonly evacuationSiteCount: number;
  readonly isLoadingHazardLayers: boolean;
  readonly isLoadingExposureJobs: boolean;
  readonly isLoadingExposureSummaries: boolean;
  readonly isRegisteringHazardLayer: boolean;
  readonly isCreatingExposureJob: boolean;
  readonly isProcessingExposureJob: boolean;
  readonly processingExposureJobId: string | null;
  readonly onRegisterHazardLayer: (mapAsset: MapAssetSummary) => Promise<void>;
  readonly onCreateExposureJob: (hazardLayer: HazardLayerSummary) => Promise<void>;
  readonly onProcessExposureJob: (job: ExposureAnalysisJobSummary) => Promise<void>;
  readonly statusMessage?: string | null;
}

function formatIsoUtc(iso: string): string {
  const ms = Date.parse(iso);
  if (Number.isNaN(ms)) return iso;

  const d = new Date(ms);
  return d.toLocaleString(undefined, {
    year: "numeric",
    month: "short",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit"
  });
}

function shortGuid(value: string | null): string {
  if (!value) return "—";
  return value.length > 10 ? `${value.slice(0, 8)}…` : value;
}

function selectDisplayedHazardMapAsset(
  selectedMapAssetId: string | null,
  mapAssets: readonly MapAssetSummary[],
  hazardLayerMapAssetIds: readonly string[]
): MapAssetSummary | null {
  const selectedLayer = selectedMapAssetId ? mapAssets.find((layer) => layer.id === selectedMapAssetId) ?? null : null;
  const selectedHazardMapAsset =
    selectedLayer && hazardLayerMapAssetIds.includes(selectedLayer.id) ? selectedLayer : null;
  const fallbackHazardMapAsset = mapAssets.find((layer) => hazardLayerMapAssetIds.includes(layer.id)) ?? null;
  return selectedHazardMapAsset ?? fallbackHazardMapAsset;
}

export function ExposureReadinessPanel({
  selectedMapAssetId,
  mapAssets,
  hazardLayerMapAssetIds,
  hazardLayers,
  exposureJobs,
  exposureSummaries,
  barangayCount,
  criticalFacilityCount,
  evacuationSiteCount,
  isLoadingHazardLayers,
  isLoadingExposureJobs,
  isLoadingExposureSummaries,
  isRegisteringHazardLayer,
  isCreatingExposureJob,
  isProcessingExposureJob,
  processingExposureJobId,
  onRegisterHazardLayer,
  onCreateExposureJob,
  onProcessExposureJob,
  statusMessage
}: ExposureReadinessPanelProps): ReactElement {
  const displayedHazardMapAsset = selectDisplayedHazardMapAsset(selectedMapAssetId, mapAssets, hazardLayerMapAssetIds);

  const registeredHazardLayer =
    displayedHazardMapAsset === null
      ? null
      : hazardLayers.find((h) => h.isActive && h.mapAssetId === displayedHazardMapAsset.id) ?? null;

  const activeHazardLayerReady = registeredHazardLayer !== null;
  const canRegisterHazardLayer = displayedHazardMapAsset !== null && !activeHazardLayerReady;
  const canCreateExposureJob = activeHazardLayerReady && barangayCount > 0;

  const hazardLayerStatus = isLoadingHazardLayers
    ? "Loading…"
    : displayedHazardMapAsset
      ? activeHazardLayerReady
        ? "Ready"
        : "Not registered"
      : "Missing";

  const hazardLayerName = displayedHazardMapAsset ? displayedHazardMapAsset.name : "No hazard layer selected";
  const hazardFeatureCount = displayedHazardMapAsset ? displayedHazardMapAsset.featureCount : 0;

  const createDisabledReason =
    displayedHazardMapAsset === null
      ? "Select a hazard GeoJSON layer to enable exposure analysis."
      : !activeHazardLayerReady
        ? "Register the hazard layer before running exposure analysis."
        : barangayCount === 0
          ? "Add barangay reference data before running exposure analysis."
          : null;

  const recentJobs = exposureJobs.slice(0, 5);

  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-base">Exposure readiness</CardTitle>
        <CardDescription>Queue exposure analysis jobs from registered hazard layers.</CardDescription>
      </CardHeader>

      <CardContent className="space-y-4">
        {statusMessage ? <p className="text-sm">{statusMessage}</p> : null}

        <div className="space-y-1">
          <div className="text-sm font-medium text-slate-900">Hazard layer status: {hazardLayerStatus}</div>
          <div className="text-sm text-muted-foreground">Hazard layer name: {hazardLayerName}</div>
          <div className="text-sm text-muted-foreground">Hazard feature count: {hazardFeatureCount}</div>
        </div>

        <div className="space-y-1">
          <div className="text-sm text-muted-foreground">Barangay references: {barangayCount}</div>
          <div className="text-sm text-muted-foreground">Critical facilities: {criticalFacilityCount}</div>
          <div className="text-sm text-muted-foreground">Evacuation sites: {evacuationSiteCount}</div>
        </div>

        {isLoadingHazardLayers ? (
          <p className="text-sm text-muted-foreground">Loading hazard layer registrations…</p>
        ) : null}

        {canRegisterHazardLayer && displayedHazardMapAsset ? (
          <div className="space-y-1">
            <Button
              type="button"
              onClick={() => void onRegisterHazardLayer(displayedHazardMapAsset)}
              disabled={isRegisteringHazardLayer || isLoadingHazardLayers}
            >
              {isRegisteringHazardLayer ? "Registering…" : "Register hazard layer"}
            </Button>
          </div>
        ) : null}

        <div className="space-y-1">
          <Button
            type="button"
            onClick={() => {
              if (registeredHazardLayer) {
                void onCreateExposureJob(registeredHazardLayer);
              }
            }}
            disabled={!canCreateExposureJob || isCreatingExposureJob}
          >
            {isCreatingExposureJob ? "Creating job…" : "Run exposure analysis"}
          </Button>
          {createDisabledReason ? <p className="text-xs text-muted-foreground">{createDisabledReason}</p> : null}
        </div>

        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <div className="text-sm font-medium">Recent exposure jobs</div>
            {isLoadingExposureJobs ? <div className="text-xs text-muted-foreground">Loading…</div> : null}
          </div>

          {recentJobs.length === 0 ? (
            <p className="text-sm text-muted-foreground">No exposure jobs have been queued yet.</p>
          ) : (
            <div className="space-y-2">
              {recentJobs.map((job) => (
                <div key={job.id} className="rounded-md border px-3 py-2">
                  <div className="flex items-baseline justify-between gap-3">
                    <div className="text-sm font-medium">{job.status}</div>
                    <div className="text-xs text-muted-foreground">{formatIsoUtc(job.createdAtUtc)}</div>
                  </div>
                  <div className="mt-1 text-xs text-muted-foreground">
                    Hazard layer: {shortGuid(job.hazardLayerId)}
                  </div>
                  {job.errorMessage ? (
                    <div className="mt-1 text-xs text-rose-700">
                      Error: {job.errorMessage.length > 120 ? `${job.errorMessage.slice(0, 120)}…` : job.errorMessage}
                    </div>
                  ) : null}

                  {job.status === "Queued" ? (
                    <div className="mt-2">
                      <Button
                        type="button"
                        onClick={() => void onProcessExposureJob(job)}
                        disabled={isProcessingExposureJob}
                      >
                        {isProcessingExposureJob && processingExposureJobId === job.id
                          ? "Processing…"
                          : "Process job"}
                      </Button>
                    </div>
                  ) : null}
                </div>
              ))}
            </div>
          )}
        </div>

        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <div className="text-sm font-medium">Exposure summaries</div>
            {isLoadingExposureSummaries ? <div className="text-xs text-muted-foreground">Loading…</div> : null}
          </div>

          {isLoadingExposureSummaries ? (
            <p className="text-sm text-muted-foreground">Loading exposure summaries…</p>
          ) : null}

          {!isLoadingExposureSummaries && exposureSummaries.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              No exposure summaries are available yet. Queued jobs will show stored results here after a future computation slice runs.
            </p>
          ) : null}

          {!isLoadingExposureSummaries && exposureSummaries.length > 0 ? (
            <div className="space-y-2">
              {exposureSummaries.slice(0, 5).map((s) => (
                <div key={s.id} className="rounded-md border px-3 py-2">
                  <div className="flex items-baseline justify-between gap-3">
                    <div className="text-sm font-medium">{s.hazardType}</div>
                    <div className="text-xs text-muted-foreground">{formatIsoUtc(s.createdAtUtc)}</div>
                  </div>
                  {s.severity ? (
                    <div className="mt-1 text-xs text-muted-foreground">Severity: {s.severity}</div>
                  ) : null}
                  <div className="mt-1 text-xs text-muted-foreground">Facilities: {s.exposedFacilityCount}</div>
                  {s.exposedPopulation != null ? (
                    <div className="mt-1 text-xs text-muted-foreground">Population: {s.exposedPopulation}</div>
                  ) : null}
                  {s.exposedAreaHectares != null ? (
                    <div className="mt-1 text-xs text-muted-foreground">Area: {s.exposedAreaHectares} ha</div>
                  ) : null}
                  {s.riskScore != null ? (
                    <div className="mt-1 text-xs text-muted-foreground">Risk score: {s.riskScore}</div>
                  ) : null}
                </div>
              ))}
            </div>
          ) : null}
        </div>
      </CardContent>
    </Card>
  );
}

