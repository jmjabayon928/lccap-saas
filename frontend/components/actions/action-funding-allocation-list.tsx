"use client";

import { useState } from "react";
import { RefreshCw } from "lucide-react";
import { Button } from "@/components/ui/button";
import type { ActionFundingAllocationSummary } from "@/types/actions";

export interface ActionFundingAllocationListProps {
  readonly allocations: readonly ActionFundingAllocationSummary[];
  readonly subsetFilterActive?: boolean;
  readonly loading: boolean;
  readonly error: string | null;
  readonly onArchive: (allocationId: string) => Promise<void>;
  readonly onRetry?: () => void;
  readonly highlightActionItemId?: string | null;
}

function formatAllocated(amount: number, currencyCode: string): string {
  try {
    return new Intl.NumberFormat(undefined, {
      style: "currency",
      currency: currencyCode,
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(amount);
  } catch {
    return `${amount.toFixed(2)} ${currencyCode}`;
  }
}

function formatCreated(iso: string | null): string {
  if (!iso?.trim()) {
    return "—";
  }
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) {
    return iso;
  }
  return new Intl.DateTimeFormat(undefined, { dateStyle: "medium", timeStyle: "short" }).format(d);
}

export function ActionFundingAllocationList({
  allocations,
  subsetFilterActive,
  loading,
  error,
  onArchive,
  onRetry,
  highlightActionItemId
}: ActionFundingAllocationListProps) {
  const [archivingId, setArchivingId] = useState<string | null>(null);

  if (loading) {
    return (
      <div className="flex items-center gap-2 py-8 text-sm text-muted-foreground">
        <RefreshCw className="h-5 w-5 shrink-0 animate-spin text-emerald-700" aria-hidden />
        Loading allocations…
      </div>
    );
  }

  if (error) {
    return (
      <div className="rounded-md border border-amber-200 bg-amber-50/50 px-3 py-3 text-sm text-amber-950/90">
        <p>{error}</p>
        {onRetry ? (
          <Button type="button" variant="outline" size="sm" className="mt-2 gap-2" onClick={() => void onRetry()}>
            <RefreshCw className="h-4 w-4" aria-hidden />
            Retry
          </Button>
        ) : null}
      </div>
    );
  }

  const highlight =
    typeof highlightActionItemId === "string" && highlightActionItemId.trim().length > 0
      ? highlightActionItemId.trim()
      : null;

  if (allocations.length === 0) {
    const emptyMsg = subsetFilterActive
      ? "No funding allocations for the selected action or filter yet."
      : "No funding allocations have been added yet.";
    return (
      <p className="rounded-lg border border-dashed border-border bg-slate-50/80 px-4 py-10 text-center text-sm text-muted-foreground">
        {emptyMsg}
      </p>
    );
  }

  return (
    <ul className="flex flex-col gap-3">
      {allocations.map((a) => {
        const emphasized = Boolean(highlight && a.actionItemId === highlight);
        return (
          <li
            key={a.id}
            className={
              emphasized
                ? "rounded-lg border border-emerald-200 bg-emerald-50/60 px-3 py-3 text-sm shadow-sm"
                : "rounded-lg border border-border bg-white px-3 py-3 text-sm shadow-sm"
            }
          >
            <div className="flex flex-wrap items-start justify-between gap-2">
              <div className="min-w-0 space-y-1">
                <p className="font-medium text-slate-900">{a.actionTitle}</p>
                <p className="text-xs text-muted-foreground">
                  <span className="font-medium text-slate-700">Source:</span> {a.fundingSourceName}
                </p>
                {a.fundingProgramName?.trim() ? (
                  <p className="text-xs text-muted-foreground">
                    <span className="font-medium text-slate-700">Program:</span> {a.fundingProgramName}
                  </p>
                ) : null}
                {(a.climateExpenditureTagCode?.trim() ?? a.climateExpenditureTagName?.trim()) ? (
                  <p className="text-xs text-muted-foreground">
                    <span className="font-medium text-slate-700">CCET:</span>{" "}
                    {[a.climateExpenditureTagCode, a.climateExpenditureTagName].filter(Boolean).join(" · ")}
                    {a.climateExpenditureTagCategory?.trim() ? ` (${a.climateExpenditureTagCategory})` : ""}
                  </p>
                ) : null}
                <p className="text-xs text-muted-foreground">
                  FY <span className="tabular-nums font-medium text-slate-800">{a.fiscalYear}</span> ·{" "}
                  <span className="tabular-nums font-semibold text-slate-900">
                    {formatAllocated(a.allocatedAmount, a.currencyCode)}
                  </span>{" "}
                  · <span className="rounded bg-slate-100 px-1.5 py-0 text-[11px]">{a.allocationStatus}</span>
                </p>
                {a.notes?.trim() ? (
                  <p className="text-xs leading-snug text-slate-700">
                    <span className="font-medium">Notes:</span> {a.notes}
                  </p>
                ) : null}
                <p className="text-[11px] text-muted-foreground">Created {formatCreated(a.createdAtUtc)}</p>
              </div>
              <Button
                type="button"
                variant="secondary"
                size="sm"
                disabled={archivingId === a.id}
                onClick={() => {
                  if (
                    !confirm(
                      "Archive this funding allocation? It will be removed from active lists (soft delete), not permanently erased."
                    )
                  ) {
                    return;
                  }
                  setArchivingId(a.id);
                  void (async () => {
                    try {
                      await onArchive(a.id);
                    } finally {
                      setArchivingId(null);
                    }
                  })();
                }}
              >
                {archivingId === a.id ? "Archiving…" : "Archive"}
              </Button>
            </div>
          </li>
        );
      })}
    </ul>
  );
}
