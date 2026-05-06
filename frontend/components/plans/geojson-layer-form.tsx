"use client";

import type { FormEvent, ReactElement } from "react";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { isApiError } from "@/lib/api/api-error";
import { planClient } from "@/lib/plans/plan-client";
import type { GeoJsonFeatureCollectionPayload, MapType } from "@/types/plans";

const MAP_TYPES: readonly MapType[] = [
  "Flood",
  "Landslide",
  "StormSurge",
  "Boundary",
  "LandUse",
  "Hazard",
  "Other"
];

interface GeoJsonLayerFormProps {
  readonly planId: string;
  readonly onCreated?: () => Promise<void> | void;
}

export function GeoJsonLayerForm({ planId, onCreated }: GeoJsonLayerFormProps): ReactElement {
  const [fileAssetId, setFileAssetId] = useState("");
  const [name, setName] = useState("");
  const [mapType, setMapType] = useState<MapType>("Flood");
  const [textarea, setTextarea] = useState("{}");
  const [status, setStatus] =
    useState<
      | { kind: "idle" }
      | { kind: "saving" }
      | { kind: "error"; message: string }
      | { kind: "ok"; message: string }
    >({
      kind: "idle"
    });

  async function submit(ev: FormEvent<HTMLFormElement>): Promise<void> {
    ev.preventDefault();
    setStatus({ kind: "saving" });

    let parsedUnknown: unknown;
    try {
      const trimmed = textarea.trim().length === 0 ? "{}" : textarea.trim();
      parsedUnknown = JSON.parse(trimmed) as unknown;
    } catch {
      setStatus({ kind: "error", message: "Paste valid JSON representing a GeoJSON FeatureCollection." });
      return;
    }

    const featureCollection = asFeatureCollection(parsedUnknown);
    if (featureCollection === null) {
      setStatus({
        kind: "error",
        message:
          'GeoJSON must be an object with "type":"FeatureCollection" and a features array.'
      });
      return;
    }

    if (!fileAssetIdTrimmedValid(fileAssetId)) {
      setStatus({
        kind: "error",
        message: "File asset ID must be the UUID for an uploaded file in this tenant."
      });
      return;
    }

    const layerName = name.trim();
    if (layerName.length === 0 || layerName.length > 250) {
      setStatus({ kind: "error", message: "Name is required (max 250 characters)." });
      return;
    }

    try {
      const created = await planClient.createGeoJsonLayer(planId, {
        fileAssetId: fileAssetId.trim(),
        name: layerName,
        mapType,
        description: null,
        geoJson: featureCollection,
      });

      setStatus({ kind: "ok", message: `Registered ${created.name} (${created.featureCount} features).` });
      if (onCreated) {
        await onCreated();
      }
    } catch (err: unknown) {
      if (isApiError(err)) {
        const message =
          err.status === 404 ? "Plan or referenced file asset was not found." : err.message;
        setStatus({ kind: "error", message });
      } else {
        setStatus({ kind: "error", message: "Could not register the GeoJSON layer." });
      }
    }
  }

  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-base">Register GeoJSON map layer</CardTitle>
        <CardDescription>
          Provide an uploaded GeoJSON-aligned file asset UUID and paste a bounded FeatureCollection. The API validates shape
          and persists feature rows.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form className="space-y-4" onSubmit={(e) => void submit(e)}>
          <div className="grid gap-3 md:grid-cols-2">
            <div className="space-y-1">
              <label className="text-sm font-medium text-slate-800" htmlFor="file-asset-id">
                File asset ID
              </label>
              <Input
                id="file-asset-id"
                placeholder="Uploaded file GUID"
                value={fileAssetId}
                onChange={(e) => setFileAssetId(e.target.value)}
                autoComplete="off"
              />
            </div>
            <div className="space-y-1">
              <label className="text-sm font-medium text-slate-800" htmlFor="layer-name">
                Layer name
              </label>
              <Input
                id="layer-name"
                placeholder="Flood inundation sketch"
                value={name}
                onChange={(e) => setName(e.target.value)}
              />
            </div>
          </div>

          <div className="space-y-1">
            <label className="text-sm font-medium text-slate-800" htmlFor="map-type-select">
              Map Type
            </label>
            <Select
              id="map-type-select"
              value={mapType}
              onChange={(e) => setMapType(e.target.value as MapType)}
            >
              {MAP_TYPES.map((t) => (
                <option key={t} value={t}>
                  {t}
                </option>
              ))}
            </Select>
          </div>

          <div className="space-y-1">
            <label className="text-sm font-medium text-slate-800" htmlFor="geojson-textarea">
              GeoJSON (FeatureCollection)
            </label>
            <textarea
              id="geojson-textarea"
              className="min-h-[200px] w-full rounded-md border border-border bg-white px-3 py-2 font-mono text-xs text-slate-900 shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              spellCheck={false}
              value={textarea}
              onChange={(e) => setTextarea(e.target.value)}
              aria-required
            />
          </div>

          {status.kind === "error" ? (
            <p className="text-sm text-red-800" role="alert">
              {status.message}
            </p>
          ) : null}
          {status.kind === "ok" ? (
            <p className="text-sm text-emerald-900" role="status">
              {status.message}
            </p>
          ) : null}

          <Button type="submit" disabled={status.kind === "saving"} className="gap-2">
            {status.kind === "saving" ? "Saving…" : "Register GeoJSON layer"}
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}

function fileAssetIdTrimmedValid(raw: string): boolean {
  const v = raw.trim();
  return /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/.test(v);
}

function asFeatureCollection(value: unknown): GeoJsonFeatureCollectionPayload | null {
  if (!value || typeof value !== "object") {
    return null;
  }
  const obj = value as Record<string, unknown>;
  const typeRaw = obj.type;
  const featuresUnknown = obj.features;
  if (typeRaw !== "FeatureCollection" || !Array.isArray(featuresUnknown)) {
    return null;
  }

  return value as GeoJsonFeatureCollectionPayload;
}
