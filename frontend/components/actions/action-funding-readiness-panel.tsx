"use client";

import { useMemo } from "react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ActionBudget } from "@/components/actions/action-budget";
import { ActionTypeBadge } from "@/components/actions/action-type-badge";
import type {
  ActionFundingAllocationSummary,
  ActionItemSummary,
  ActionType,
  ClimateExpenditureTagCategory,
  ClimateExpenditureTagSummary,
  FundingProgramsLoadState,
  FundingSourcesLoadState
} from "@/types/actions";

function suggestedCcetCategories(actionType: ActionType): ClimateExpenditureTagCategory[] {
  if (actionType === "Adaptation") {
    return ["Adaptation", "CrossCutting", "DisasterRiskReduction", "CapacityDevelopment", "Other"];
  }
  return ["Mitigation", "CrossCutting", "CapacityDevelopment", "Other"];
}

function formatCategoryLabel(category: ClimateExpenditureTagCategory): string {
  switch (category) {
    case "CrossCutting":
      return "Cross-cutting";
    case "DisasterRiskReduction":
      return "Disaster risk reduction";
    case "CapacityDevelopment":
      return "Capacity development";
    default:
      return category;
  }
}

export interface ActionFundingReadinessPanelProps {
  readonly actions: readonly ActionItemSummary[];
  readonly tags: readonly ClimateExpenditureTagSummary[];
  readonly planAllocations?: readonly ActionFundingAllocationSummary[] | null;
  readonly actionsLoading: boolean;
  readonly actionsError: string | null;
  readonly ccetLoading: boolean;
  readonly ccetError: string | null;
  readonly fundingSourcesState: FundingSourcesLoadState;
  readonly fundingProgramsState: FundingProgramsLoadState;
}

export function ActionFundingReadinessPanel({
  actions,
  tags,
  planAllocations,
  actionsLoading,
  actionsError,
  ccetLoading,
  ccetError,
  fundingSourcesState,
  fundingProgramsState
}: ActionFundingReadinessPanelProps) {
  const withBudget = actions.filter((a) => Number.isFinite(a.budgetAmount) && a.budgetAmount > 0).length;

  const withFundingSource = actions.filter((a) => {
    const fundingSource = a.fundingSource?.trim() ?? "";
    return fundingSource.length > 0;
  }).length;

  const missingFundingSource = actions.filter((a) => {
    const fundingSource = a.fundingSource?.trim() ?? "";
    return fundingSource.length === 0;
  }).length;

  const activeTagCount = tags.filter((t) => t.isActive).length;

  const allocList = useMemo(
    () => planAllocations ?? [],
    [planAllocations]
  );

  const allocationTotalsByCurrency = useMemo(() => {
    const totals = new Map<string, number>();

    for (const allocation of allocList) {
      const currencyCode = allocation.currencyCode.trim().toUpperCase();
      const currentTotal = totals.get(currencyCode) ?? 0;

      totals.set(currencyCode, currentTotal + allocation.allocatedAmount);
    }

    return [...totals.entries()].sort(([leftCurrency], [rightCurrency]) =>
      leftCurrency.localeCompare(rightCurrency)
    );
  }, [allocList]);

  const allocationTotalsByFundingSource = useMemo(() => {
    const rows = new Map<string, { readonly name: string; readonly byCurrency: Map<string, number> }>();

    for (const allocation of allocList) {
      const fundingSourceName = allocation.fundingSourceName.trim() || "Unknown source";
      let row = rows.get(fundingSourceName);

      if (row === undefined) {
        row = {
          name: fundingSourceName,
          byCurrency: new Map<string, number>()
        };

        rows.set(fundingSourceName, row);
      }

      const currencyCode = allocation.currencyCode.trim().toUpperCase();
      const currentTotal = row.byCurrency.get(currencyCode) ?? 0;

      row.byCurrency.set(currencyCode, currentTotal + allocation.allocatedAmount);
    }

    return [...rows.values()].sort((left, right) => left.name.localeCompare(right.name));
  }, [allocList]);

  const fundingSourcesCatalogLoading =
    fundingSourcesState.status === "idle" || fundingSourcesState.status === "loading";

  const fundingProgramsCatalogLoading =
    fundingProgramsState.status === "idle" || fundingProgramsState.status === "loading";

  return (
    <Card className="border-border shadow-sm">
      <CardHeader className="pb-3">
        <CardTitle className="text-base">Funding readiness</CardTitle>
        <CardDescription>
          Quick checklist for budgeting and tagging prep—helps LGU teams see gaps before linkage and allocation slices ship.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4 text-sm">
        <p className="rounded-md border border-slate-200 bg-slate-50/90 px-3 py-2 text-xs text-muted-foreground leading-snug">
          CCET tags are available for planning reference. Catalog funding sources/programs remain read-only; allocations below
          link planned amounts to IDs from those catalogs.
        </p>

        <div className="grid gap-2 sm:grid-cols-2">
          <div className="rounded-md border border-border px-3 py-2">
            <p className="text-xs font-medium text-muted-foreground">Funding sources (catalog)</p>
            <div className="mt-1 tabular-nums text-lg font-semibold text-slate-900">
              {fundingSourcesCatalogLoading ? (
                <span className="text-sm font-normal text-muted-foreground">Loading…</span>
              ) : fundingSourcesState.status === "error" ? (
                <span className="text-sm font-normal text-amber-800">Unavailable</span>
              ) : fundingSourcesState.status === "ready" ? (
                fundingSourcesState.data.totalCount
              ) : (
                <span className="text-sm font-normal text-muted-foreground">—</span>
              )}
            </div>
            {fundingSourcesState.status === "error" ? (
              <p className="mt-1 text-[11px] text-amber-900/90">{fundingSourcesState.message}</p>
            ) : null}
          </div>
          <div className="rounded-md border border-border px-3 py-2">
            <p className="text-xs font-medium text-muted-foreground">Active funding programs (catalog)</p>
            <div className="mt-1 tabular-nums text-lg font-semibold text-slate-900">
              {fundingProgramsCatalogLoading ? (
                <span className="text-sm font-normal text-muted-foreground">Loading…</span>
              ) : fundingProgramsState.status === "error" ? (
                <span className="text-sm font-normal text-amber-800">Unavailable</span>
              ) : fundingProgramsState.status === "ready" ? (
                fundingProgramsState.data.totalCount
              ) : (
                <span className="text-sm font-normal text-muted-foreground">—</span>
              )}
            </div>
            {fundingProgramsState.status === "error" ? (
              <p className="mt-1 text-[11px] text-amber-900/90">{fundingProgramsState.message}</p>
            ) : null}
          </div>
        </div>

        {actionsLoading ? (
          <p className="text-muted-foreground">Loading actions for this summary…</p>
        ) : actionsError ? (
          <p className="rounded-md border border-amber-200 bg-amber-50/50 px-3 py-2 text-amber-950/90">
            Actions could not be loaded ({actionsError}). Retry actions above to see funding completeness.
          </p>
        ) : actions.length === 0 ? (
          <p className="text-muted-foreground">No actions yet. Add actions to review funding completeness.</p>
        ) : (
          <dl className="grid gap-2 sm:grid-cols-2">
            <div className="rounded-md border border-border px-3 py-2">
              <dt className="text-xs text-muted-foreground">Total actions</dt>
              <dd className="text-lg font-semibold tabular-nums text-slate-900">{actions.length}</dd>
            </div>
            <div className="rounded-md border border-border px-3 py-2">
              <dt className="text-xs text-muted-foreground">With budget (&gt; 0)</dt>
              <dd className="text-lg font-semibold tabular-nums text-slate-900">{withBudget}</dd>
            </div>
            <div className="rounded-md border border-border px-3 py-2">
              <dt className="text-xs text-muted-foreground">Funding source recorded</dt>
              <dd className="text-lg font-semibold tabular-nums text-slate-900">{withFundingSource}</dd>
            </div>
            <div className="rounded-md border border-border px-3 py-2">
              <dt className="text-xs text-muted-foreground">Missing funding source</dt>
              <dd className="text-lg font-semibold tabular-nums text-slate-900">{missingFundingSource}</dd>
            </div>
          </dl>
        )}

        {allocList.length > 0 ? (
          <div className="rounded-md border border-emerald-200 bg-emerald-50/40 px-3 py-2">
            <p className="text-xs font-medium text-emerald-900/90">Planned allocations on this plan</p>
            <p className="mt-1 text-sm font-semibold tabular-nums text-emerald-950">
              {allocList.length} record{allocList.length === 1 ? "" : "s"}
            </p>
            {allocationTotalsByCurrency.length ? (
              <ul className="mt-2 space-y-0.5 text-xs text-emerald-900/85">
                {allocationTotalsByCurrency.map(([cur, sum]) => (
                  <li key={cur}>
                    <span className="font-mono">{cur}</span> total allocated:{" "}
                    <span className="tabular-nums font-medium">{sum.toFixed(2)}</span>
                  </li>
                ))}
              </ul>
            ) : null}
            {allocationTotalsByFundingSource.length ? (
              <div className="mt-3 border-t border-emerald-200/80 pt-2">
                <p className="text-[11px] font-medium text-emerald-900/90">By funding source</p>
                <ul className="mt-1 space-y-1 text-[11px] text-emerald-900/85">
                  {allocationTotalsByFundingSource.map((row) => (
                    <li key={row.name}>
                      <span className="font-medium text-emerald-950">{row.name}</span>
                      <ul className="ml-3 mt-0.5 list-disc space-y-0.5">
                        {[...row.byCurrency.entries()]
                          .sort(([a], [b]) => a.localeCompare(b))
                          .map(([cur, sum]) => (
                            <li key={`${row.name}-${cur}`}>
                              <span className="font-mono">{cur}</span>{" "}
                              <span className="tabular-nums font-medium">{sum.toFixed(2)}</span>
                            </li>
                          ))}
                      </ul>
                    </li>
                  ))}
                </ul>
              </div>
            ) : null}
          </div>
        ) : null}

        <div className="rounded-md border border-border px-3 py-2">
          <p className="text-xs font-medium text-muted-foreground">Available CCET tags (active)</p>
          <div className="mt-1 tabular-nums text-slate-900">
            {ccetLoading ? (
              <span className="text-muted-foreground">Loading…</span>
            ) : ccetError ? (
              <span className="text-amber-800">Unavailable ({ccetError})</span>
            ) : (
              <span className="text-lg font-semibold">{activeTagCount}</span>
            )}
          </div>
        </div>

        {!actionsLoading && actions.length > 0 ? (
          <div className="space-y-2">
            <h3 className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Per action</h3>
            <ul className="max-h-[320px] space-y-2 overflow-y-auto pr-1 text-xs">
              {actions.map((a) => (
                <li key={a.id} className="rounded-md border border-border bg-white px-3 py-2 shadow-sm">
                  <div className="flex flex-wrap items-start justify-between gap-2">
                    <p className="min-w-0 flex-1 font-medium text-slate-900">{a.title}</p>
                    <ActionTypeBadge actionType={a.actionType} />
                  </div>
                  <div className="mt-1 flex flex-wrap items-center gap-x-3 gap-y-1 text-muted-foreground">
                    <span>
                      Budget: <ActionBudget budgetAmount={a.budgetAmount} />
                    </span>
                    <span className="text-slate-700">
                      {a.fundingSource?.trim()
                        ? `Source: ${a.fundingSource.trim()}`
                        : "Source: No funding source"}
                    </span>
                  </div>
                  <p className="mt-2 text-[11px] leading-snug text-muted-foreground">
                    <span className="font-medium text-slate-600">Suggested CCET categories:</span>{" "}
                    {suggestedCcetCategories(a.actionType).map(formatCategoryLabel).join(", ")}
                  </p>
                </li>
              ))}
            </ul>
          </div>
        ) : null}
      </CardContent>
    </Card>
  );
}
