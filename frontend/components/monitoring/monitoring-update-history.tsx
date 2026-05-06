"use client";

import { useMemo } from "react";
import { AlertCircle, History } from "lucide-react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import type { MonitoringUpdateSummary } from "@/types/monitoring";

export interface MonitoringUpdateHistoryProps {
  readonly updates: readonly MonitoringUpdateSummary[];
  readonly loading?: boolean;
  readonly error?: string | null;
}

function formatNumberOrDash(n: number | null): string {
  if (n === null || n === undefined) {
    return "—";
  }
  return String(n);
}

export function MonitoringUpdateHistory({ updates, loading, error }: MonitoringUpdateHistoryProps) {
  const sorted = useMemo(() => {
    const list = [...updates];
    list.sort((a, b) => Date.parse(b.reportedAtUtc) - Date.parse(a.reportedAtUtc));
    return list;
  }, [updates]);

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <History className="h-5 w-5" />
          Update history
        </CardTitle>
        <CardDescription>Newest updates first.</CardDescription>
      </CardHeader>
      <CardContent className="space-y-3">
        {error ? (
          <Alert variant="destructive">
            <AlertCircle className="h-4 w-4" />
            <AlertTitle>Could not load updates</AlertTitle>
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        ) : null}

        {loading ? <div className="text-sm text-muted-foreground">Loading…</div> : null}

        {!loading && !error && sorted.length === 0 ? (
          <div className="text-sm text-muted-foreground">No updates yet.</div>
        ) : null}

        <div className="space-y-2">
          {sorted.map((u) => (
            <div key={u.id} className="rounded-md border p-3">
              <div className="flex flex-wrap items-baseline justify-between gap-2">
                <div className="font-medium">{u.periodLabel}</div>
                <div className="text-xs text-muted-foreground">
                  {new Date(u.reportedAtUtc).toLocaleString()}
                </div>
              </div>

              <div className="mt-1 text-sm">
                <div className="flex flex-wrap gap-x-6 gap-y-1">
                  <div>
                    <span className="text-muted-foreground">Status:</span> {u.status}
                  </div>
                  <div>
                    <span className="text-muted-foreground">Actual:</span>{" "}
                    {formatNumberOrDash(u.actualValue)}
                  </div>
                  <div>
                    <span className="text-muted-foreground">Progress:</span>{" "}
                    {u.progressPercent === null ? "—" : `${u.progressPercent}%`}
                  </div>
                </div>
              </div>

              {u.notes ? <div className="mt-2 whitespace-pre-wrap text-sm">{u.notes}</div> : null}
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}

