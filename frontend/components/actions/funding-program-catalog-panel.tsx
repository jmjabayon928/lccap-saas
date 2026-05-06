"use client";

import { RefreshCw } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import type { FundingProgramsResult } from "@/types/actions";

function formatShortRange(opensIso: string | null, closesIso: string | null): string | null {
  const o = opensIso?.trim().length ? Date.parse(opensIso) : NaN;
  const c = closesIso?.trim().length ? Date.parse(closesIso) : NaN;
  const fmt = (ms: number) =>
    new Intl.DateTimeFormat(undefined, {
      dateStyle: "medium",
      timeZone: "UTC"
    }).format(new Date(ms));
  const parts: string[] = [];
  if (!Number.isNaN(o)) {
    parts.push(`Opens ${fmt(o)} (UTC)`);
  }
  if (!Number.isNaN(c)) {
    parts.push(`Closes ${fmt(c)} (UTC)`);
  }
  return parts.length ? parts.join(" · ") : null;
}

export interface FundingProgramCatalogPanelProps {
  readonly status: "idle" | "loading" | "ready" | "error";
  readonly result?: FundingProgramsResult;
  readonly errorMessage?: string;
  readonly onRetry?: () => void;
}

export function FundingProgramCatalogPanel({ status, result, errorMessage, onRetry }: FundingProgramCatalogPanelProps) {
  const busy = status === "idle" || status === "loading";
  const failed = status === "error";

  if (busy) {
    return (
      <Card className="border-border shadow-sm">
        <CardHeader className="pb-3">
          <CardTitle className="text-base">Funding programs</CardTitle>
          <CardDescription>Active programs visible by default.</CardDescription>
        </CardHeader>
        <CardContent className="flex items-center gap-3 py-6 text-sm text-muted-foreground">
          <RefreshCw className="h-5 w-5 shrink-0 animate-spin text-emerald-700" aria-hidden />
          Loading funding programs…
        </CardContent>
      </Card>
    );
  }

  if (failed) {
    return (
      <Card className="border-amber-200 bg-amber-50/50 shadow-sm">
        <CardHeader className="pb-2">
          <CardTitle className="text-base text-amber-950">Funding programs could not be loaded</CardTitle>
          <CardDescription className="text-amber-950/85">
            {errorMessage ?? "Something went wrong while loading funding programs."}
          </CardDescription>
        </CardHeader>
        {onRetry ? (
          <CardContent className="pt-0">
            <Button type="button" variant="outline" size="sm" className="gap-2" onClick={() => void onRetry()}>
              <RefreshCw className="h-4 w-4" aria-hidden />
              Retry funding programs
            </Button>
          </CardContent>
        ) : null}
      </Card>
    );
  }

  if (!result) {
    return null;
  }

  return (
    <Card className="border-border shadow-sm">
      <CardHeader className="pb-3">
        <div className="flex flex-wrap items-start justify-between gap-2">
          <div className="min-w-0">
            <CardTitle className="text-base">Funding programs</CardTitle>
            <CardDescription>Active programs in your tenant workspace.</CardDescription>
          </div>
          <Badge variant="secondary" className="shrink-0">
            {result.totalCount} active
          </Badge>
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        {result.items.length === 0 ? (
          <p className="rounded-md border border-dashed border-border bg-slate-50/80 px-3 py-8 text-center text-sm text-muted-foreground">
            No active funding programs are available yet.
          </p>
        ) : (
          <ul className="space-y-2 text-sm">
            {result.items.map((p) => {
              const rangeLabel = formatShortRange(p.opensAtUtc, p.closesAtUtc);
              return (
                <li key={p.id} className="rounded-md border border-border bg-white px-3 py-2 shadow-sm">
                  <div className="flex flex-wrap items-baseline gap-x-2 gap-y-1">
                    <span className="font-medium text-slate-900">{p.name}</span>
                    <Badge variant="outline" className="font-normal capitalize">
                      {p.status}
                    </Badge>
                  </div>
                  <p className="mt-1 text-xs text-muted-foreground">
                    Source{" "}
                    <span className="font-medium text-slate-800">{p.fundingSourceName}</span>
                    {p.programCode?.trim() ? (
                      <>
                        {" "}
                        · code <span className="font-mono text-slate-700">{p.programCode.trim()}</span>
                      </>
                    ) : null}
                  </p>
                  {typeof p.maxAwardAmount === "number" && Number.isFinite(p.maxAwardAmount) ? (
                    <p className="mt-1 text-xs text-muted-foreground">
                      Max award:{" "}
                      <span className="tabular-nums font-medium text-slate-800">{p.maxAwardAmount.toFixed(2)}</span>{" "}
                      <span className="font-mono uppercase">{p.currencyCode}</span>
                    </p>
                  ) : null}
                  {rangeLabel ? (
                    <p className="mt-1 text-[11px] text-muted-foreground">{rangeLabel}</p>
                  ) : null}
                </li>
              );
            })}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}
