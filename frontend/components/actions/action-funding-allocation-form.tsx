"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { isApiError } from "@/lib/api/api-error";
import { actionClient } from "@/lib/actions/action-client";
import type {
  ActionItemSummary,
  ClimateExpenditureTagSummary,
  CreateActionFundingAllocationRequest,
  FundingProgramSummary,
  FundingSourceSummary
} from "@/types/actions";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";

export interface ActionFundingAllocationFormProps {
  readonly planId: string;
  readonly actionItems: readonly ActionItemSummary[];
  readonly ccetTags: readonly ClimateExpenditureTagSummary[];
  readonly fundingSources: readonly FundingSourceSummary[];
  readonly fundingPrograms: readonly FundingProgramSummary[];
  readonly fundingSourcesReady: boolean;
  readonly fundingProgramsReady: boolean;
  readonly selectedActionId: string | null;
  readonly onCreated: () => void;
}

function formatApiValidationMessage(details: unknown): string | null {
  if (typeof details !== "object" || details === null || Array.isArray(details)) {
    return null;
  }
  const rec = details as Record<string, unknown>;
  const errs = rec.errors;
  if (Array.isArray(errs)) {
    const parts = errs.filter((x): x is string => typeof x === "string" && x.trim().length > 0);
    return parts.length ? parts.join(" ") : null;
  }
  if (typeof errs === "object" && errs !== null && !Array.isArray(errs)) {
    const nested = errs as Record<string, unknown>;
    const parts = Object.values(nested).flatMap((v) =>
      Array.isArray(v)
        ? v.filter((x): x is string => typeof x === "string")
        : typeof v === "string"
          ? [v]
          : []
    );
    return parts.length ? parts.join(" ") : null;
  }
  return null;
}

const currencyRegex = /^[A-Z]{3}$/;

export function ActionFundingAllocationForm({
  planId,
  actionItems,
  ccetTags,
  fundingSources,
  fundingPrograms,
  fundingSourcesReady,
  fundingProgramsReady,
  selectedActionId,
  onCreated
}: ActionFundingAllocationFormProps) {
  const [actionItemId, setActionItemId] = useState<string>("");
  const [fundingSourceId, setFundingSourceId] = useState("");
  const [fundingProgramId, setFundingProgramId] = useState("");
  const [climateTagId, setClimateTagId] = useState<string>("");
  const [fiscalYear, setFiscalYear] = useState<number>(new Date().getUTCFullYear());
  const [allocatedAmount, setAllocatedAmount] = useState("");
  const [currencyCode, setCurrencyCode] = useState("PHP");
  const [notes, setNotes] = useState("");
  const [validationError, setValidationError] = useState<string | null>(null);
  const [apiError, setApiError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const activeCcetTags = useMemo(() => ccetTags.filter((t) => t.isActive), [ccetTags]);

  const catalogBusy = !fundingSourcesReady || !fundingProgramsReady;
  const noFundingSourcesConfigured = fundingSourcesReady && fundingSources.length === 0;

  const programsForSelectedSource = useMemo(() => {
    const sid = fundingSourceId.trim();
    if (!sid) {
      return [];
    }
    return fundingPrograms.filter((p) => p.fundingSourceId === sid);
  }, [fundingPrograms, fundingSourceId]);

  useEffect(() => {
    if (selectedActionId && actionItems.some((a) => a.id === selectedActionId)) {
      setActionItemId(selectedActionId);
    }
  }, [selectedActionId, actionItems]);

  const resetForm = useCallback(() => {
    setFundingSourceId("");
    setFundingProgramId("");
    setClimateTagId("");
    setFiscalYear(new Date().getUTCFullYear());
    setAllocatedAmount("");
    setCurrencyCode("PHP");
    setNotes("");
    setActionItemId(
      selectedActionId && actionItems.some((a) => a.id === selectedActionId) ? selectedActionId : ""
    );
  }, [selectedActionId, actionItems]);

  async function submit(e: React.FormEvent): Promise<void> {
    e.preventDefault();
    setValidationError(null);
    setApiError(null);

    const src = fundingSourceId.trim();
    if (!actionItemId.trim()) {
      setValidationError("Select an action item.");
      return;
    }
    if (!src) {
      setValidationError("Select a funding source.");
      return;
    }
    if (!Number.isInteger(fiscalYear) || fiscalYear < 2000 || fiscalYear > 2100) {
      setValidationError("Fiscal year must be an integer from 2000 to 2100.");
      return;
    }
    const amountNum = Number.parseFloat(allocatedAmount.trim());
    if (!Number.isFinite(amountNum) || amountNum < 0) {
      setValidationError("Allocated amount must be a number greater than or equal to 0.");
      return;
    }
    const curr = currencyCode.trim().toUpperCase();
    if (curr.length > 0 && !currencyRegex.test(curr)) {
      setValidationError("Currency code must be blank (defaults to PHP) or exactly three letters A–Z.");
      return;
    }

    const programTrim = fundingProgramId.trim();
    const tagTrim = climateTagId.trim();
    const notesTrim = notes.trim();

    const request: CreateActionFundingAllocationRequest = {
      actionItemId: actionItemId.trim(),
      fundingSourceId: src,
      fundingProgramId: programTrim.length ? programTrim : null,
      climateExpenditureTagId: tagTrim.length ? tagTrim : null,
      fiscalYear,
      allocatedAmount: amountNum,
      currencyCode: curr.length ? curr : null,
      allocationStatus: "Planned",
      notes: notesTrim.length ? notesTrim : null
    };

    setBusy(true);
    try {
      await actionClient.createActionFundingAllocation(planId, request);
      resetForm();
      onCreated();
    } catch (err) {
      if (isApiError(err)) {
        const fromBody = formatApiValidationMessage(err.details);
        setApiError(fromBody ?? err.message);
      } else if (err instanceof Error) {
        setApiError(err.message);
      } else {
        setApiError("Could not create allocation.");
      }
    } finally {
      setBusy(false);
    }
  }

  const errorLine = validationError ?? apiError;

  return (
    <form className="space-y-3 rounded-lg border border-border bg-white px-4 py-3 shadow-sm" onSubmit={(e) => void submit(e)}>
        <fieldset className="space-y-3" disabled={busy || actionItems.length === 0}>
          <legend className="text-sm font-medium text-slate-900">Add planned allocation</legend>

          {catalogBusy ? (
          <p className="rounded-md border border-border bg-slate-50 px-3 py-2 text-xs text-muted-foreground">
            Loading funding source and program catalogs…
          </p>
          ) : null}

          {!catalogBusy && noFundingSourcesConfigured ? (
          <p className="rounded-md border border-amber-200 bg-amber-50/70 px-3 py-2 text-xs text-amber-950">
            No funding sources are available yet—you cannot record an allocation until at least one source exists for your LGU tenant.
          </p>
          ) : null}

        <div className="space-y-1">
          <label htmlFor="alloc-action-item" className="text-xs font-medium text-muted-foreground">
            Action item
          </label>
          <Select
            id="alloc-action-item"
            required
            value={actionItemId}
            onChange={(e) => setActionItemId(e.target.value)}
          >
            <option value="">Select action…</option>
            {actionItems.map((a) => (
              <option key={a.id} value={a.id}>
                {a.title}
              </option>
            ))}
          </Select>
        </div>

        <div className="space-y-1">
          <label htmlFor="alloc-funding-source" className="text-xs font-medium text-muted-foreground">
            Funding source
          </label>
          <Select
            id="alloc-funding-source"
            required
            value={fundingSourceId}
            disabled={catalogBusy || noFundingSourcesConfigured}
            onChange={(e) => {
              const next = e.target.value;
              setFundingSourceId(next);
              setFundingProgramId((prev) => {
                if (!prev.trim()) {
                  return "";
                }
                const belongs = fundingPrograms.some(
                  (p) => p.id === prev.trim() && p.fundingSourceId === next.trim()
                );
                return belongs ? prev : "";
              });
            }}
          >
            <option value="">Select funding source…</option>
            {fundingSources.map((s) => (
              <option key={s.id} value={s.id}>
                {s.name}
              </option>
            ))}
          </Select>
          {fundingSourcesReady && fundingSources.length === 1 ? (
            <p className="text-[11px] text-muted-foreground leading-snug">Only one tenant source is configured.</p>
          ) : null}
        </div>

        <div className="space-y-1">
          <label htmlFor="alloc-program" className="text-xs font-medium text-muted-foreground">
            Funding program (optional)
          </label>
          <Select
            id="alloc-program"
            value={fundingProgramId}
            disabled={
              catalogBusy
              || noFundingSourcesConfigured
              || !fundingSourceId.trim()
            }
            onChange={(e) => setFundingProgramId(e.target.value)}
          >
            <option value="">
              {!fundingSourceId.trim() ? "Select a funding source first" : "None / not linked to a program"}
            </option>
            {programsForSelectedSource.map((p) => (
              <option key={p.id} value={p.id}>
                {p.name}
                {p.programCode?.trim() ? ` (${p.programCode.trim()})` : ""}
              </option>
            ))}
          </Select>
          {fundingSourceId.trim() && programsForSelectedSource.length === 0 ? (
            <p className="text-[11px] text-muted-foreground leading-snug">
              No programs for this source—you can submit the allocation without a program.
            </p>
          ) : null}
        </div>

        <div className="space-y-1">
          <label htmlFor="alloc-ccet" className="text-xs font-medium text-muted-foreground">
            Climate expenditure tag (optional)
          </label>
          <Select id="alloc-ccet" value={climateTagId} onChange={(e) => setClimateTagId(e.target.value)}>
            <option value="">None</option>
            {activeCcetTags.map((t) => (
              <option key={t.id} value={t.id}>
                {t.tagCode} — {t.tagName}
              </option>
            ))}
          </Select>
        </div>

        <div className="grid gap-3 sm:grid-cols-2">
          <div className="space-y-1">
            <label htmlFor="alloc-fy" className="text-xs font-medium text-muted-foreground">
              Fiscal year
            </label>
            <Input
              id="alloc-fy"
              type="number"
              min={2000}
              max={2100}
              step={1}
              value={fiscalYear}
              onChange={(e) => setFiscalYear(Number.parseInt(e.target.value, 10) || 0)}
            />
          </div>
          <div className="space-y-1">
            <label htmlFor="alloc-amount" className="text-xs font-medium text-muted-foreground">
              Allocated amount
            </label>
            <Input
              id="alloc-amount"
              type="text"
              inputMode="decimal"
              value={allocatedAmount}
              onChange={(e) => setAllocatedAmount(e.target.value)}
              placeholder="0.00"
            />
          </div>
        </div>

        <div className="space-y-1">
          <label htmlFor="alloc-currency" className="text-xs font-medium text-muted-foreground">
            Currency code
          </label>
          <Input
            id="alloc-currency"
            value={currencyCode}
            onChange={(e) => setCurrencyCode(e.target.value.toUpperCase())}
            placeholder="PHP"
            maxLength={3}
            className="max-w-[120px] font-mono uppercase"
          />
        </div>

        <div className="space-y-1">
          <label htmlFor="alloc-notes" className="text-xs font-medium text-muted-foreground">
            Notes (optional)
          </label>
          <Input id="alloc-notes" value={notes} onChange={(e) => setNotes(e.target.value)} placeholder="" />
        </div>

        {errorLine ? <p className="text-xs text-red-700">{errorLine}</p> : null}

        <Button
          type="submit"
          size="sm"
          disabled={
            busy
            || actionItems.length === 0
            || catalogBusy
            || noFundingSourcesConfigured
          }
        >
          {busy ? "Saving…" : "Create planned allocation"}
        </Button>

        {actionItems.length === 0 ? (
          <p className="text-xs text-muted-foreground">Add at least one action item before recording allocations.</p>
        ) : null}
      </fieldset>
    </form>
  );
}
