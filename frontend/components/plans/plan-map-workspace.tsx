"use client";

import type { ReactElement } from "react";
import { useCallback, useEffect, useMemo, useState } from "react";
import { RefreshCw } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { BarangayFacilityList } from "@/components/plans/barangay-facility-list";
import { GeoJsonLayerForm } from "@/components/plans/geojson-layer-form";
import { MapFeatureList } from "@/components/plans/map-feature-list";
import { MapLayerList } from "@/components/plans/map-layer-list";
import { isApiError } from "@/lib/api/api-error";
import { planClient } from "@/lib/plans/plan-client";
import type { PlanMapWorkspaceResult } from "@/types/plans";

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

  const load = useCallback(async () => {
    if (!planId) {
      setPanel({ status: "error", message: "Missing plan identifier.", retryable: false });
      return;
    }

    setPanel({ status: "loading" });

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
  }, [planId]);

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

              <BarangayFacilityList barangays={panel.data.barangays} facilities={panel.data.criticalFacilities} />
            </div>
          </div>
        </>
      ) : null}
    </section>
  );
}
