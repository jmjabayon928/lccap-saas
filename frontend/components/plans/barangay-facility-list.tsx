import type { ReactElement } from "react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import type { BarangaySummary, CriticalFacilitySummary } from "@/types/plans";

interface BarangayFacilityListProps {
  readonly barangays: readonly BarangaySummary[];
  readonly facilities: readonly CriticalFacilitySummary[];
}

export function BarangayFacilityList({ barangays, facilities }: BarangayFacilityListProps): ReactElement {
  return (
    <div className="grid gap-4 lg:grid-cols-2">
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base">Barangays</CardTitle>
          <CardDescription>Account-scoped reference barangays (non-deleted).</CardDescription>
        </CardHeader>
        <CardContent className="max-h-[360px] space-y-2 overflow-auto text-sm">
          {barangays.length === 0 ? (
            <p className="text-muted-foreground">No barangays configured for this tenant.</p>
          ) : (
            <ul className="space-y-2">
              {barangays.map((b) => (
                <li key={b.id} className="rounded-md border border-border/80 bg-muted/30 px-3 py-2">
                  <div className="font-medium text-slate-900">{b.name}</div>
                  <div className="mt-1 space-y-0.5 font-mono text-xs text-muted-foreground">
                    {b.code ? <div>Code · {b.code}</div> : null}
                    {b.latitude != null && b.longitude != null ? (
                      <div>
                        Location · {b.latitude}, {b.longitude}
                      </div>
                    ) : null}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base">Critical facilities</CardTitle>
          <CardDescription>Plan-scoped facilities (non-deleted).</CardDescription>
        </CardHeader>
        <CardContent className="max-h-[360px] space-y-2 overflow-auto text-sm">
          {facilities.length === 0 ? (
            <p className="text-muted-foreground">No critical facilities for this plan.</p>
          ) : (
            <ul className="space-y-2">
              {facilities.map((f) => (
                <li key={f.id} className="rounded-md border border-border/80 bg-muted/30 px-3 py-2">
                  <div className="font-medium text-slate-900">{f.name}</div>
                  <div className="mt-1 flex flex-wrap gap-x-3 gap-y-1 text-xs text-muted-foreground">
                    <span>{f.facilityType}</span>
                    {f.barangayName ? <span>Barangay · {f.barangayName}</span> : null}
                    {f.isEvacuationSite ? <span className="text-emerald-800">Evacuation site</span> : null}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
