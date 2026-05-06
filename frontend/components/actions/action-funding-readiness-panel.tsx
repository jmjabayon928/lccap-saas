"use client";

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ActionBudget } from "@/components/actions/action-budget";
import { ActionTypeBadge } from "@/components/actions/action-type-badge";
import type {
  ActionItemSummary,
  ActionType,
  ClimateExpenditureTagCategory,
  ClimateExpenditureTagSummary
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
  readonly actionsLoading: boolean;
  readonly actionsError: string | null;
  readonly ccetLoading: boolean;
  readonly ccetError: string | null;
}

export function ActionFundingReadinessPanel({
  actions,
  tags,
  actionsLoading,
  actionsError,
  ccetLoading,
  ccetError
}: ActionFundingReadinessPanelProps) {
  const withBudget = actions.filter((a) => Number.isFinite(a.budgetAmount) && a.budgetAmount > 0).length;
  const withFundingSource = actions.filter((a) => (a.fundingSource?.trim() ?? "").length > 0).length;
  const missingFundingSource = actions.filter((a) => !(a.fundingSource?.trim() ?? "").length).length;

  const activeTagCount = tags.filter((t) => t.isActive).length;

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
          CCET tags are available for planning reference. Tag persistence and allocation tracking will be added in a later slice.
        </p>

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
