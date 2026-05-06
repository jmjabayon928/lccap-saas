import type { ReactElement } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import type { MapAssetSummary } from "@/types/plans";

interface MapLayerListProps {
  readonly layers: readonly MapAssetSummary[];
  readonly selectedId: string | null;
  readonly onSelect: (id: string) => void;
  readonly onArchive: (id: string) => void;
  readonly busyId: string | null;
}

export function MapLayerList({
  layers,
  selectedId,
  onSelect,
  onArchive,
  busyId
}: MapLayerListProps): ReactElement {
  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-base">Map layers</CardTitle>
        <CardDescription>GeoJSON-backed layers highlight how many persisted features exist for each asset.</CardDescription>
      </CardHeader>
      <CardContent className="space-y-2">
        {layers.length === 0 ? (
          <p className="text-sm text-muted-foreground">No map assets yet.</p>
        ) : (
          <ul className="space-y-2">
            {layers.map((m) => (
              <li
                key={m.id}
                className={`flex flex-col gap-2 rounded-md border px-3 py-2 md:flex-row md:items-center md:justify-between ${
                  selectedId === m.id ? "border-emerald-600/60 bg-emerald-50/50" : "border-border/80 bg-white"
                }`}
              >
                <div className="min-w-0">
                  <div className="truncate font-medium text-slate-900">{m.name}</div>
                  <div className="mt-1 flex flex-wrap gap-x-2 gap-y-1 text-xs text-muted-foreground">
                    <span>{m.mapFormat}</span>
                    <span>·</span>
                    <span>{m.mapType}</span>
                    <span>·</span>
                    <span>{m.featureCount} features</span>
                  </div>
                  <div className="mt-1 truncate font-mono text-[11px] text-muted-foreground">
                    Source file · {m.originalFileName}
                  </div>
                </div>
                <div className="flex shrink-0 flex-wrap gap-2">
                  <Button
                    type="button"
                    size="sm"
                    variant={selectedId === m.id ? "default" : "outline"}
                    onClick={() => onSelect(m.id)}
                    disabled={m.mapFormat !== "GeoJson"}
                    title={m.mapFormat !== "GeoJson" ? "Feature listing applies to GeoJSON-based map assets." : undefined}
                  >
                    View metadata
                  </Button>
                  <Button
                    type="button"
                    size="sm"
                    variant="ghost"
                    className="text-red-900 hover:bg-red-50"
                    disabled={busyId === m.id}
                    onClick={() => onArchive(m.id)}
                  >
                    Archive
                  </Button>
                </div>
              </li>
            ))}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}
