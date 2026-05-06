"use client";

import type { ReactElement } from "react";
import { useEffect, useState } from "react";
import { RefreshCw } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { planClient } from "@/lib/plans/plan-client";
import type { GeoJsonLayerFeatureSummary } from "@/types/plans";

interface MapFeatureListProps {
  readonly mapAssetId: string | null;
}

export function MapFeatureList({ mapAssetId }: MapFeatureListProps): ReactElement {
  const [state, setState] =
    useState<
      | { status: "idle" }
      | { status: "loading" }
      | { status: "ready"; items: readonly GeoJsonLayerFeatureSummary[] }
      | { status: "error"; message: string }
    >({ status: "idle" });

  useEffect(() => {
    let cancelled = false;
    async function load(): Promise<void> {
      if (!mapAssetId) {
        setState({ status: "idle" });
        return;
      }
      setState({ status: "loading" });
      try {
        const items = await planClient.getGeoJsonLayerFeatures(mapAssetId, { limit: 500 });
        if (!cancelled) {
          setState({ status: "ready", items });
        }
      } catch {
        if (!cancelled) {
          setState({ status: "error", message: "Could not load GeoJSON feature rows for this layer." });
        }
      }
    }

    void load();
    return () => {
      cancelled = true;
    };
  }, [mapAssetId]);

  function previewPayload(value: unknown): string {
    try {
      return JSON.stringify(value, null, 2);
    } catch {
      return String(value);
    }
  }

  return (
    <Card className="min-h-[200px]">
      <CardHeader className="pb-3">
        <CardTitle className="text-base">GeoJSON feature metadata</CardTitle>
        <CardDescription>Structured previews (no rendered basemap).</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {!mapAssetId ? (
          <p className="text-sm text-muted-foreground">Choose a GeoJSON map layer above to inspect features.</p>
        ) : null}

        {mapAssetId && state.status === "loading" ? (
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <RefreshCw className="h-4 w-4 animate-spin text-emerald-700" aria-hidden />
            Loading features…
          </div>
        ) : null}

        {state.status === "error" ? (
          <div className="rounded-md border border-amber-200 bg-amber-50/60 px-3 py-2 text-sm text-amber-950">
            <p>{state.message}</p>
            <Button
              type="button"
              variant="outline"
              size="sm"
              className="mt-2 gap-2"
              onClick={() =>
                mapAssetId
                  ? void (async () => {
                      setState({ status: "loading" });
                      try {
                        const items = await planClient.getGeoJsonLayerFeatures(mapAssetId, { limit: 500 });
                        setState({ status: "ready", items });
                      } catch {
                        setState({
                          status: "error",
                          message: "Could not load GeoJSON feature rows for this layer."
                        });
                      }
                    })()
                  : undefined
              }
            >
              <RefreshCw className="h-3.5 w-3.5" aria-hidden />
              Retry features
            </Button>
          </div>
        ) : null}

        {state.status === "ready" && mapAssetId ? (
          <div className="max-h-[480px] space-y-3 overflow-auto pr-1">
            {state.items.length === 0 ? (
              <p className="text-sm text-muted-foreground">This layer reports zero features.</p>
            ) : (
              state.items.map((f) => (
                <div key={f.id} className="rounded-md border border-border/70 bg-muted/20 p-3 text-sm">
                  <div className="flex flex-wrap items-baseline gap-2 font-medium text-slate-900">
                    <span>{f.displayName ?? f.featureId ?? f.id.slice(0, 8)}</span>
                    <span className="text-xs font-normal text-muted-foreground">
                      ({f.featureType ?? "geometry"})
                    </span>
                  </div>
                  <div className="mt-3 space-y-2">
                    <div>
                      <div className="text-xs uppercase tracking-wide text-muted-foreground">Geometry</div>
                      <pre className="max-h-40 overflow-auto rounded-md bg-slate-950/95 p-2 font-mono text-[11px] text-slate-100">
                        {previewPayload(f.geometryJson).slice(0, 2400)}
                        {previewPayload(f.geometryJson).length > 2400 ? "…" : ""}
                      </pre>
                    </div>
                    <div>
                      <div className="text-xs uppercase tracking-wide text-muted-foreground">Properties</div>
                      <pre className="max-h-32 overflow-auto rounded-md bg-muted/60 p-2 font-mono text-[11px]">
                        {previewPayload(f.propertiesJson).slice(0, 1800)}
                        {previewPayload(f.propertiesJson).length > 1800 ? "…" : ""}
                      </pre>
                    </div>
                  </div>
                </div>
              ))
            )}
          </div>
        ) : null}
      </CardContent>
    </Card>
  );
}
