"use client";

import { RefreshCw } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { PlanActivityFeed } from "@/components/plans/plan-activity-feed";
import type { FundingCurrencyTotal, PlanOperationalDashboard } from "@/types/plans";

export type OperationalDashboardPanelState =
  | { status: "idle" }
  | { status: "loading" }
  | { status: "ready"; data: PlanOperationalDashboard }
  | { status: "error"; message: string; retryable: boolean };

interface PlanOperationalDashboardProps {
  readonly panel: OperationalDashboardPanelState;
  readonly onRetry: () => void;
}

function fmt(n: number): string {
  return n.toLocaleString();
}

function YesNo(value: boolean): string {
  return value ? "Yes" : "No";
}

function formatWhen(iso: string | null | undefined): string {
  if (!iso?.trim()) {
    return "—";
  }
  try {
    return new Intl.DateTimeFormat(undefined, { dateStyle: "medium", timeStyle: "short" }).format(new Date(iso));
  } catch {
    return iso;
  }
}

function StatGrid(props: { rows: readonly { label: string; value: string | number }[] }) {
  return (
    <dl className="grid gap-1.5">
      {props.rows.map((r) => (
        <div key={r.label} className="flex justify-between gap-3 text-sm">
          <dt className="text-muted-foreground">{r.label}</dt>
          <dd className="font-medium tabular-nums text-slate-900">{r.value}</dd>
        </div>
      ))}
    </dl>
  );
}

export function PlanOperationalDashboard({ panel, onRetry }: PlanOperationalDashboardProps) {
  if (panel.status === "idle" || panel.status === "loading") {
    return (
      <Card>
        <CardHeader className="pb-2">
          <CardTitle className="text-base">Operational overview</CardTitle>
          <CardDescription>Evidence, actions, monitoring, funding, reviews, and recent activity.</CardDescription>
        </CardHeader>
        <CardContent className="flex items-center gap-3 py-8 text-sm text-muted-foreground">
          <RefreshCw
            className={`h-5 w-5 shrink-0 text-emerald-700 ${panel.status === "loading" ? "animate-spin" : ""}`}
            aria-hidden
          />
          {panel.status === "loading" ? "Loading operational dashboard…" : "Preparing operational dashboard…"}
        </CardContent>
      </Card>
    );
  }

  if (panel.status === "error") {
    return (
      <Card className="border-amber-200 bg-amber-50/50">
        <CardHeader className="pb-2">
          <CardTitle className="text-base text-amber-950">Operational overview unavailable</CardTitle>
          <CardDescription className="text-amber-950/85">{panel.message}</CardDescription>
        </CardHeader>
        <CardContent className="flex flex-wrap gap-2">
          {panel.retryable ? (
            <Button type="button" variant="outline" size="sm" className="gap-2" onClick={() => onRetry()}>
              <RefreshCw className="h-4 w-4" aria-hidden />
              Retry dashboard
            </Button>
          ) : null}
        </CardContent>
      </Card>
    );
  }

  const d = panel.data;
  const period = `${d.planningPeriodStart}–${d.planningPeriodEnd}`;

  return (
    <Card>
      <CardHeader className="flex flex-row flex-wrap items-start justify-between gap-3 pb-2">
        <div className="space-y-1">
          <CardTitle className="text-base">Operational overview</CardTitle>
          <CardDescription>
            {d.planTitle} · Planning period {period} · Generated {(() => {
              try {
                return new Intl.DateTimeFormat(undefined, {
                  dateStyle: "medium",
                  timeStyle: "short"
                }).format(new Date(d.generatedAtUtc));
              } catch {
                return d.generatedAtUtc;
              }
            })()}
          </CardDescription>
        </div>
        <Button type="button" variant="outline" size="sm" className="shrink-0 gap-2" onClick={() => onRetry()}>
          <RefreshCw className="h-4 w-4" aria-hidden />
          Refresh
        </Button>
      </CardHeader>
      <CardContent className="space-y-6">
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          <div className="rounded-lg border border-border bg-muted/20 p-3">
            <h3 className="text-xs font-semibold uppercase tracking-wide text-slate-600">Evidence</h3>
            <StatGrid
              rows={[
                { label: "Total documents", value: fmt(d.evidence.totalDocuments) },
                { label: "Official", value: fmt(d.evidence.officialEvidenceCount) },
                { label: "Public", value: fmt(d.evidence.publicEvidenceCount) },
                { label: "Linked to sections", value: fmt(d.evidence.linkedToSectionCount) },
                { label: "Linked to actions", value: fmt(d.evidence.linkedToActionCount) }
              ]}
            />
          </div>
          <div className="rounded-lg border border-border bg-muted/20 p-3">
            <h3 className="text-xs font-semibold uppercase tracking-wide text-slate-600">Actions</h3>
            <StatGrid
              rows={[
                { label: "Total", value: fmt(d.actions.totalActions) },
                { label: "In progress", value: fmt(d.actions.inProgressCount) },
                { label: "On track", value: fmt(d.actions.onTrackCount) },
                { label: "Delayed", value: fmt(d.actions.delayedCount) },
                { label: "Missing funding source", value: fmt(d.actions.missingFundingSourceCount) }
              ]}
            />
          </div>
          <div className="rounded-lg border border-border bg-muted/20 p-3">
            <h3 className="text-xs font-semibold uppercase tracking-wide text-slate-600">Monitoring</h3>
            <StatGrid
              rows={[
                { label: "Indicators", value: fmt(d.monitoring.totalIndicators) },
                { label: "Updates", value: fmt(d.monitoring.totalMonitoringUpdates) },
                { label: "Latest update", value: formatWhen(d.monitoring.latestMonitoringUpdateAtUtc) },
                { label: "With updates", value: fmt(d.monitoring.indicatorsWithUpdatesCount) }
              ]}
            />
          </div>
          <div className="rounded-lg border border-border bg-muted/20 p-3">
            <h3 className="text-xs font-semibold uppercase tracking-wide text-slate-600">Review comments</h3>
            <StatGrid
              rows={[
                { label: "Total", value: fmt(d.review.totalComments) },
                { label: "Unresolved", value: fmt(d.review.unresolvedComments) },
                { label: "Data gaps", value: fmt(d.review.dataGapComments) },
                { label: "Validation", value: fmt(d.review.validationComments) },
                { label: "Revision requests", value: fmt(d.review.revisionRequestComments) }
              ]}
            />
          </div>
          <div className="rounded-lg border border-border bg-muted/20 p-3">
            <h3 className="text-xs font-semibold uppercase tracking-wide text-slate-600">Funding allocations</h3>
            <StatGrid
              rows={[
                { label: "Total rows", value: fmt(d.funding.totalAllocations) },
                { label: "CCET tagged", value: fmt(d.funding.ccetTaggedAllocations) },
                { label: "Untagged", value: fmt(d.funding.untaggedAllocations) },
                ...d.funding.allocationTotalsByCurrency.map((t: FundingCurrencyTotal) => ({
                  label: `Total ${t.currencyCode}`,
                  value: t.totalAllocatedAmount.toLocaleString(undefined, {
                    minimumFractionDigits: 0,
                    maximumFractionDigits: 2
                  })
                }))
              ]}
            />
          </div>
          <div className="rounded-lg border border-border bg-muted/20 p-3">
            <h3 className="text-xs font-semibold uppercase tracking-wide text-slate-600">Export readiness</h3>
            <StatGrid
              rows={[
                { label: "Official evidence", value: YesNo(d.exportReadiness.hasOfficialEvidence) },
                { label: "Actions recorded", value: YesNo(d.exportReadiness.hasActions) },
                { label: "Monitoring", value: YesNo(d.exportReadiness.hasMonitoring) },
                { label: "Funding rows", value: YesNo(d.exportReadiness.hasFundingAllocations) },
                { label: "Open comments", value: YesNo(d.exportReadiness.hasUnresolvedComments) }
              ]}
            />
          </div>
        </div>

        {d.exportReadiness.suggestedNextSteps.length > 0 ? (
          <div className="rounded-lg border border-dashed border-emerald-800/30 bg-emerald-50/40 p-3">
            <h3 className="text-sm font-semibold text-emerald-950">Suggested next steps</h3>
            <ul className="mt-2 list-inside list-disc space-y-1 text-sm text-emerald-950/90">
              {d.exportReadiness.suggestedNextSteps.map((s: string) => (
                <li key={s}>{s}</li>
              ))}
            </ul>
          </div>
        ) : null}

        <div>
          <h3 className="text-sm font-semibold text-slate-900">Recent activity</h3>
          <p className="mb-2 text-xs text-muted-foreground">Latest changes in this plan workspace (tenant-scoped).</p>
          <PlanActivityFeed items={d.recentActivity} />
        </div>
      </CardContent>
    </Card>
  );
}
