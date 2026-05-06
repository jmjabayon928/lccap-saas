"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { isApiError } from "@/lib/api/api-error";
import { actionClient } from "@/lib/actions/action-client";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Select } from "@/components/ui/select";
import { ActionFundingAllocationForm } from "@/components/actions/action-funding-allocation-form";
import { ActionFundingAllocationList } from "@/components/actions/action-funding-allocation-list";
import type {
  ActionFundingAllocationSummary,
  ActionItemSummary,
  ClimateExpenditureTagSummary,
  FundingProgramsLoadState,
  FundingSourcesLoadState
} from "@/types/actions";

export interface ActionFundingAllocationPanelProps {
  readonly planId: string;
  readonly actionItems: readonly ActionItemSummary[];
  readonly ccetTags: readonly ClimateExpenditureTagSummary[];
  readonly fundingSourcesState: FundingSourcesLoadState;
  readonly fundingProgramsState: FundingProgramsLoadState;
  readonly selectedActionId: string | null;
  readonly onPlanAllocationsChange?: (items: readonly ActionFundingAllocationSummary[]) => void;
}

export function ActionFundingAllocationPanel({
  planId,
  actionItems,
  ccetTags,
  fundingSourcesState,
  fundingProgramsState,
  selectedActionId,
  onPlanAllocationsChange
}: ActionFundingAllocationPanelProps) {
  const [allocations, setAllocations] = useState<ActionFundingAllocationSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [listScope, setListScope] = useState<"all" | "selected">("all");

  const reload = useCallback(async () => {
    if (!planId.trim()) {
      setAllocations([]);
      setLoading(false);
      setError(null);
      onPlanAllocationsChange?.([]);
      return;
    }
    setLoading(true);
    setError(null);
    try {
      const result = await actionClient.getActionFundingAllocationsByPlan(planId);
      const items = [...result.items];
      setAllocations(items);
      onPlanAllocationsChange?.(items);
    } catch (err) {
      if (isApiError(err)) {
        const notFound = err.status === 404;
        const forbidden = err.status === 403;
        setError(
          notFound || forbidden
            ? "Funding allocations could not be loaded for this plan in your session."
            : err.message
        );
      } else if (err instanceof Error) {
        setError(err.message);
      } else {
        setError("Could not load allocations.");
      }
      setAllocations([]);
      onPlanAllocationsChange?.([]);
    } finally {
      setLoading(false);
    }
  }, [planId, onPlanAllocationsChange]);

  useEffect(() => {
    void reload();
  }, [reload]);

  useEffect(() => {
    if (!selectedActionId) {
      setListScope("all");
    }
  }, [selectedActionId]);

  const displayedAllocations = useMemo(() => {
    if (listScope === "selected" && selectedActionId?.trim()) {
      return allocations.filter((a) => a.actionItemId === selectedActionId.trim());
    }
    return allocations;
  }, [allocations, listScope, selectedActionId]);

  const totalsByCurrency = useMemo(() => {
    const m = new Map<string, number>();
    for (const a of allocations) {
      const cur = a.currencyCode.trim().toUpperCase();
      const prev = m.get(cur) ?? 0;
      m.set(cur, prev + a.allocatedAmount);
    }
    return [...m.entries()].sort(([a], [b]) => a.localeCompare(b));
  }, [allocations]);

  const fundingSourcesReady = fundingSourcesState.status === "ready";
  const fundingProgramsReady = fundingProgramsState.status === "ready";
  const fundingSources = fundingSourcesReady ? [...fundingSourcesState.data.items] : [];
  const fundingPrograms = fundingProgramsReady ? [...fundingProgramsState.data.items] : [];

  const selectedSummary = useMemo(() => {
    if (!selectedActionId?.trim()) {
      return null;
    }
    const n = allocations.filter((a) => a.actionItemId === selectedActionId.trim()).length;
    const title = actionItems.find((x) => x.id === selectedActionId.trim())?.title ?? "Selected action";
    return { title, count: n };
  }, [allocations, selectedActionId, actionItems]);

  async function handleArchive(allocationId: string): Promise<void> {
    await actionClient.archiveActionFundingAllocation(allocationId);
    await reload();
  }

  return (
    <Card className="border-border shadow-sm">
      <CardHeader className="pb-3">
        <CardTitle className="text-base">Funding allocations</CardTitle>
        <CardDescription>
          Planned amounts linked to actions and funding sources (tenant-scoped). Only create and archive—edits come in a later
          slice.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {!loading && !error ? (
          <div className="flex flex-wrap items-baseline gap-x-4 gap-y-1 text-sm">
            <span className="text-muted-foreground">
              Total <span className="font-semibold tabular-nums text-slate-900">{allocations.length}</span> allocation
              {allocations.length === 1 ? "" : "s"} for this plan
            </span>
            {totalsByCurrency.map(([cur, sum]) => (
              <span key={cur} className="text-muted-foreground">
                <span className="font-medium text-slate-800">{cur}</span>:{" "}
                <span className="tabular-nums font-semibold text-slate-900">{sum.toFixed(2)}</span>{" "}
                <span className="text-xs">(sum of allocated)</span>
              </span>
            ))}
          </div>
        ) : null}

        {selectedSummary ? (
          <p className="text-xs text-muted-foreground">
            Selected action &ldquo;{selectedSummary.title}&rdquo;:{" "}
            <span className="font-medium text-slate-800">{selectedSummary.count}</span> allocation
            {selectedSummary.count === 1 ? "" : "s"} on this plan.
          </p>
        ) : null}

        <div className="flex flex-wrap items-center gap-2">
          <label htmlFor="alloc-list-scope" className="text-xs font-medium text-muted-foreground">
            List view
          </label>
          <Select
            id="alloc-list-scope"
            className="max-w-[220px]"
            value={listScope}
            disabled={!selectedActionId?.trim()}
            onChange={(e) => setListScope(e.target.value === "selected" ? "selected" : "all")}
          >
            <option value="all">All plan allocations</option>
            <option value="selected">Selected action only</option>
          </Select>
        </div>

        <ActionFundingAllocationForm
          planId={planId}
          actionItems={actionItems}
          ccetTags={ccetTags}
          fundingSources={fundingSources}
          fundingPrograms={fundingPrograms}
          fundingSourcesReady={fundingSourcesReady}
          fundingProgramsReady={fundingProgramsReady}
          selectedActionId={selectedActionId}
          onCreated={() => void reload()}
        />

        <ActionFundingAllocationList
          allocations={displayedAllocations}
          subsetFilterActive={listScope === "selected"}
          loading={loading}
          error={error}
          onArchive={(id) => handleArchive(id)}
          onRetry={() => void reload()}
          highlightActionItemId={listScope === "all" ? selectedActionId : null}
        />
      </CardContent>
    </Card>
  );
}
