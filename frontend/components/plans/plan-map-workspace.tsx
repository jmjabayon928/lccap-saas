"use client";

import type { ReactElement } from "react";
import { useCallback, useEffect, useMemo, useState } from "react";
import { RefreshCw } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { BarangayFacilityList } from "@/components/plans/barangay-facility-list";
import { ExposureReadinessPanel } from "@/components/plans/exposure-readiness-panel";
import { GeoJsonLayerForm } from "@/components/plans/geojson-layer-form";
import { MapFeatureList } from "@/components/plans/map-feature-list";
import { MapLayerList } from "@/components/plans/map-layer-list";
import { isApiError } from "@/lib/api/api-error";
import { planClient } from "@/lib/plans/plan-client";
import type { ExposureAnalysisJobSummary, HazardLayerSummary, MapAssetSummary, PlanMapWorkspaceResult } from "@/types/plans";

interface PlanMapWorkspaceProps {
  readonly planId: string;
}

type PanelState =
  | { status: "idle" }
  | { status: "loading" }
  | { status: "ready"; data: PlanMapWorkspaceResult }
  | { status: "error"; message: string; retryable: boolean };

export function PlanMapWorkspace({ planId }: PlanMapWorkspaceProps): ReactElement {
  const [panel, setPanel] = useState<PanelState>({ status: "idle" });
  const [selectedLayerId, setSelectedLayerId] = useState<string | null>(null);
  const [archiveBusyId, setArchiveBusyId] = useState<string | null>(null);
  const [hazardLayers, setHazardLayers] = useState<readonly HazardLayerSummary[]>([]);
  const [exposureJobs, setExposureJobs] = useState<readonly ExposureAnalysisJobSummary[]>([]);
  const [isLoadingHazardLayers, setIsLoadingHazardLayers] = useState(false);
  const [isLoadingExposureJobs, setIsLoadingExposureJobs] = useState(false);
  const [isRegisteringHazardLayer, setIsRegisteringHazardLayer] = useState(false);
  const [isCreatingExposureJob, setIsCreatingExposureJob] = useState(false);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);

  const loadHazardLayers = useCallback(async () => {
    if (!planId) return;

    setIsLoadingHazardLayers(true);
    try {
      const items = await planClient.getHazardLayers(planId);
      setHazardLayers(items);
    } catch (err: unknown) {
      const message = isApiError(err) ? err.message : "Unable to load hazard layers.";
      setStatusMessage(message);
    } finally {
      setIsLoadingHazardLayers(false);
    }
  }, [planId]);

  const loadExposureJobs = useCallback(async () => {
    if (!planId) return;

    setIsLoadingExposureJobs(true);
    try {
      const items = await planClient.getExposureAnalysisJobs(planId);
      setExposureJobs(items);
    } catch (err: unknown) {
      const message = isApiError(err) ? err.message : "Unable to load exposure analysis jobs.";
      setStatusMessage(message);
    } finally {
      setIsLoadingExposureJobs(false);
    }
  }, [planId]);

  const load = useCallback(async () => {
    if (!planId) {
      setPanel({ status: "error", message: "Missing plan identifier.", retryable: false });
      return;
    }

    setPanel({ status: "loading" });
    setStatusMessage(null);

    try {
      const data = await planClient.getPlanMapWorkspace(planId);
      setPanel({ status: "ready", data });
      setSelectedLayerId((prev) => {
        const geoLayers = data.mapAssets.filter((m) => m.mapFormat === "GeoJson").map((m) => m.id);
        if (prev !== null && geoLayers.includes(prev)) {
          return prev;
        }

        const firstHazard = data.hazardLayerMapAssetIds.find((id) => geoLayers.includes(id));
        return firstHazard ?? geoLayers[0] ?? null;
      });

      void loadHazardLayers();
      void loadExposureJobs();
    } catch (err: unknown) {
      if (isApiError(err)) {
        const notFound = err.status === 404;
        const forbid = err.status === 403;
        setPanel({
          status: "error",
          message:
            notFound || forbid
              ? "This map workspace could not be loaded or is not permitted for your tenant session."
              : err.message,
          retryable: !(notFound || forbid)
        });
      } else {
        setPanel({
          status: "error",
          message: "Something went wrong while loading the map workspace.",
          retryable: true
        });
      }
    }
  }, [planId, loadHazardLayers, loadExposureJobs]);

  useEffect(() => {
    void load();
  }, [load]);

  const layers = useMemo(() => (panel.status === "ready" ? panel.data.mapAssets : []), [panel]);

  async function handleArchive(mapAssetId: string): Promise<void> {
    if (!window.confirm("Archive this map layer? Related GeoJSON features and annotations will be hidden.")) {
      return;
    }

    setArchiveBusyId(mapAssetId);

    try {
      await planClient.archiveMapAsset(mapAssetId);

      setSelectedLayerId((curr) => (curr === mapAssetId ? null : curr));
      await load();
    } catch (err: unknown) {
      const message = isApiError(err) ? err.message : "Could not archive this map layer.";
      window.alert(message);
    } finally {
      setArchiveBusyId(null);
    }
  }

  async function handleRegisterHazardLayer(mapAsset: MapAssetSummary): Promise<void> {
    setIsRegisteringHazardLayer(true);
    setStatusMessage(null);

    try {
      const payload = {
        mapAssetId: mapAsset.id,
        name: mapAsset.name,
        hazardType: mapAsset.mapType === "Hazard" ? "Hazard" : mapAsset.mapType,
        severity: "Moderate",
        source: mapAsset.originalFileName,
        description: mapAsset.description
      } as const;

      await planClient.registerHazardLayer(planId, payload);
      await loadHazardLayers();
      setStatusMessage("Hazard layer registered.");
    } catch (err: unknown) {
      if (isApiError(err) && err.status === 409) {
        await loadHazardLayers();
        setStatusMessage("Hazard layer is already registered.");
      } else {
        setStatusMessage("Unable to register hazard layer.");
      }
    } finally {
      setIsRegisteringHazardLayer(false);
    }
  }

  async function handleCreateExposureJob(hazardLayer: HazardLayerSummary): Promise<void> {
    setIsCreatingExposureJob(true);
    setStatusMessage(null);

    try {
      await planClient.createExposureAnalysisJob(planId, {
        hazardLayerId: hazardLayer.id
      });
      await loadExposureJobs();
      setStatusMessage("Exposure analysis job queued.");
    } catch (err: unknown) {
      if (isApiError(err) && err.status === 409) {
        await loadExposureJobs();
        setStatusMessage("A queued or running exposure job already exists for this hazard layer.");
      } else {
        setStatusMessage("Unable to queue exposure analysis job.");
      }
    } finally {
      setIsCreatingExposureJob(false);
    }
  }

  return (
    <section className="space-y-4" aria-labelledby="plan-map-workspace-heading">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 id="plan-map-workspace-heading" className="text-lg font-semibold tracking-tight text-slate-900">
            Map workspace
          </h2>
          <p className="mt-1 max-w-3xl text-sm text-muted-foreground">
            This map workspace organizes reference layers only. Exposure analysis will be added in a later phase.
          </p>
        </div>
        <Button type="button" variant="outline" size="sm" className="gap-2" onClick={() => void load()}>
          <RefreshCw className="h-4 w-4" aria-hidden />
          Retry workspace
        </Button>
      </div>

      {panel.status === "loading" || panel.status === "idle" ? (
        <Card>
          <CardContent className="flex items-center gap-3 py-10 text-sm text-muted-foreground">
            <RefreshCw className="h-5 w-5 shrink-0 animate-spin text-emerald-700" aria-hidden />
            Loading map workspace…
          </CardContent>
        </Card>
      ) : null}

      {panel.status === "error" ? (
        <Card className="border-amber-200 bg-amber-50/50">
          <CardHeader className="pb-2">
            <CardTitle className="text-base text-amber-950">Map workspace unavailable</CardTitle>
            <CardDescription className="text-amber-950/85">{panel.message}</CardDescription>
          </CardHeader>
          <CardContent className="flex flex-wrap gap-2">
            {panel.retryable ? (
              <Button type="button" variant="outline" size="sm" className="gap-2" onClick={() => void load()}>
                <RefreshCw className="h-4 w-4" aria-hidden />
                Retry
              </Button>
            ) : null}
          </CardContent>
        </Card>
      ) : null}

      {panel.status === "ready" ? (
        <>
          <div className="flex flex-wrap gap-2">
            <Badge variant="secondary">Map assets · {panel.data.counts.mapAssets}</Badge>
            <Badge variant="secondary">GeoJSON layers · {panel.data.counts.geoJsonLayers}</Badge>
            <Badge variant="secondary">Barangays · {panel.data.counts.barangays}</Badge>
            <Badge variant="secondary">Facilities · {panel.data.counts.criticalFacilities}</Badge>
            <Badge variant="outline">Evacuation sites · {panel.data.counts.evacuationSites}</Badge>
          </div>

          <div className="grid gap-6 xl:grid-cols-2 xl:items-start">
            <div className="space-y-4">
              <MapLayerList
                layers={layers}
                selectedId={selectedLayerId}
                onSelect={setSelectedLayerId}
                onArchive={(id) => void handleArchive(id)}
                busyId={archiveBusyId}
              />

              <MapFeatureList mapAssetId={selectedLayerId} />
            </div>

            <div className="space-y-6">
              <GeoJsonLayerForm planId={planId} onCreated={() => load()} />

              <ExposureReadinessPanel
                planId={planId}
                selectedMapAssetId={selectedLayerId}
                mapAssets={panel.data.mapAssets}
                hazardLayerMapAssetIds={panel.data.hazardLayerMapAssetIds}
                hazardLayers={hazardLayers}
                exposureJobs={exposureJobs}
                barangayCount={panel.data.counts.barangays}
                criticalFacilityCount={panel.data.counts.criticalFacilities}
                evacuationSiteCount={panel.data.counts.evacuationSites}
                isLoadingHazardLayers={isLoadingHazardLayers}
                isLoadingExposureJobs={isLoadingExposureJobs}
                isRegisteringHazardLayer={isRegisteringHazardLayer}
                isCreatingExposureJob={isCreatingExposureJob}
                onRegisterHazardLayer={(mapAsset) => handleRegisterHazardLayer(mapAsset)}
                onCreateExposureJob={(hazardLayer) => handleCreateExposureJob(hazardLayer)}
                statusMessage={statusMessage}
              />

              <BarangayFacilityList barangays={panel.data.barangays} facilities={panel.data.criticalFacilities} />
            </div>
          </div>
        </>
      ) : null}
    </section>
  );
}
