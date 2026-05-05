"use client";

import { useMemo, useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { isApiError } from "@/lib/api/api-error";
import { actionClient } from "@/lib/actions/action-client";
import type {
  ActionItemSummary,
  ActionStatus,
  ActionType,
  SaveActionItemResult,
  UpdateActionItemRequest
} from "@/types/actions";

const ACTION_TYPES: readonly ActionType[] = ["Adaptation", "Mitigation"];

const ACTION_STATUSES: readonly ActionStatus[] = [
  "Planned",
  "InProgress",
  "OnTrack",
  "Delayed",
  "Completed",
  "Cancelled"
];

function isoToDatetimeLocalValue(iso: string | null): string {
  if (!iso?.trim()) {
    return "";
  }
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) {
    return "";
  }
  const yyyy = d.getFullYear();
  const mm = String(d.getMonth() + 1).padStart(2, "0");
  const dd = String(d.getDate()).padStart(2, "0");
  const hh = String(d.getHours()).padStart(2, "0");
  const min = String(d.getMinutes()).padStart(2, "0");
  return `${yyyy}-${mm}-${dd}T${hh}:${min}`;
}

function datetimeLocalValueToIsoUtc(value: string): string | null {
  const v = value.trim();
  if (!v) {
    return null;
  }
  const d = new Date(v);
  if (Number.isNaN(d.getTime())) {
    return null;
  }
  return d.toISOString();
}

function formatError(err: unknown): string {
  if (isApiError(err)) {
    if (err.status === 409) {
      return "This action was changed elsewhere. Refresh and try again.";
    }
    const details = err.details as { errors?: Record<string, string[]> } | undefined;
    if (details?.errors?.RowVersion || details?.errors?.rowVersion) {
      return "This action needs to be refreshed before editing.";
    }
    return err.message;
  }
  if (err instanceof Error) {
    return err.message;
  }
  return "Could not save changes.";
}

export interface ActionEditFormProps {
  readonly action: ActionItemSummary;
  readonly onSaved: (updated: SaveActionItemResult) => void;
  readonly onCancel: () => void;
}

export function ActionEditForm({ action, onSaved, onCancel }: ActionEditFormProps) {
  const initialKey = useMemo(() => `${action.id}-${action.rowVersion}`, [action.id, action.rowVersion]);

  const [title, setTitle] = useState(action.title);
  const [description, setDescription] = useState(action.description ?? "");
  const [actionType, setActionType] = useState<ActionType>(action.actionType);
  const [sector, setSector] = useState(action.sector);
  const [responsibleOffice, setResponsibleOffice] = useState(action.responsibleOffice ?? "");
  const [budgetAmount, setBudgetAmount] = useState(String(action.budgetAmount));
  const [fundingSource, setFundingSource] = useState(action.fundingSource ?? "");
  const [timelineStartLocal, setTimelineStartLocal] = useState(() => isoToDatetimeLocalValue(action.timelineStartUtc));
  const [timelineEndLocal, setTimelineEndLocal] = useState(() => isoToDatetimeLocalValue(action.timelineEndUtc));
  const [kpi, setKpi] = useState(action.kpi ?? "");
  const [priorityScore, setPriorityScore] = useState(
    action.priorityScore === null || action.priorityScore === undefined ? "" : String(action.priorityScore)
  );
  const [status, setStatus] = useState<ActionStatus>(action.status);
  const [clientError, setClientError] = useState<string | null>(null);
  const [apiError, setApiError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  function validate(): string | null {
    const t = title.trim();
    if (!t) {
      return "Title is required.";
    }
    if (t.length > 250) {
      return "Title must be 250 characters or fewer.";
    }
    const sec = sector.trim();
    if (!sec) {
      return "Sector is required.";
    }
    if (sec.length > 100) {
      return "Sector must be 100 characters or fewer.";
    }
    if (responsibleOffice.trim().length > 150) {
      return "Responsible office must be 150 characters or fewer.";
    }
    if (fundingSource.trim().length > 150) {
      return "Funding source must be 150 characters or fewer.";
    }
    if (!ACTION_TYPES.includes(actionType)) {
      return "Action type must be Adaptation or Mitigation.";
    }
    if (!ACTION_STATUSES.includes(status)) {
      return "Status is not valid.";
    }
    const budget = Number(budgetAmount);
    if (!Number.isFinite(budget) || budget < 0) {
      return "Budget must be a number greater than or equal to zero.";
    }
    const startIso = datetimeLocalValueToIsoUtc(timelineStartLocal);
    const endIso = datetimeLocalValueToIsoUtc(timelineEndLocal);
    if (startIso && endIso && new Date(startIso).getTime() > new Date(endIso).getTime()) {
      return "Timeline start must be on or before the end date.";
    }
    if (priorityScore.trim()) {
      const p = Number(priorityScore);
      if (!Number.isFinite(p) || p < 0 || p > 100) {
        return "Priority score must be between 0 and 100 when provided.";
      }
    }
    return null;
  }

  function buildRequest(): UpdateActionItemRequest {
    const budget = Number(budgetAmount);
    const startIso = datetimeLocalValueToIsoUtc(timelineStartLocal);
    const endIso = datetimeLocalValueToIsoUtc(timelineEndLocal);
    const priority = priorityScore.trim() === "" ? null : Number(priorityScore);
    if (priority !== null && !Number.isFinite(priority)) {
      throw new Error("Invalid priority score");
    }
    return {
      title: title.trim(),
      description: description.trim() ? description.trim() : null,
      actionType,
      sector: sector.trim(),
      responsibleOffice: responsibleOffice.trim() ? responsibleOffice.trim() : null,
      budgetAmount: budget,
      fundingSource: fundingSource.trim() ? fundingSource.trim() : null,
      timelineStartUtc: startIso,
      timelineEndUtc: endIso,
      kpi: kpi.trim() ? kpi.trim() : null,
      priorityScore: priority,
      status,
      rowVersion: action.rowVersion
    };
  }

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>): Promise<void> {
    e.preventDefault();
    setClientError(null);
    setApiError(null);
    setSuccessMessage(null);

    const v = validate();
    if (v) {
      setClientError(v);
      return;
    }

    let request: UpdateActionItemRequest;
    try {
      request = buildRequest();
    } catch {
      setClientError("Please check the form values and try again.");
      return;
    }

    setIsSubmitting(true);
    try {
      const updated = await actionClient.updateActionItem(action.id, request);
      onSaved(updated);
      setSuccessMessage("Action updated.");
      window.setTimeout(() => setSuccessMessage(null), 4000);
    } catch (err) {
      setApiError(formatError(err));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="rounded-lg border border-border bg-slate-50/80 p-4 text-sm shadow-inner" key={initialKey}>
      <h3 className="font-semibold text-slate-900">Edit action</h3>
      <p className="mt-1 text-xs text-muted-foreground">Changes are saved to this plan and tenant session.</p>
      <form className="mt-4 space-y-3" onSubmit={(ev) => void handleSubmit(ev)}>
        {clientError ? (
          <Alert variant="destructive">
            <AlertTitle>Check your input</AlertTitle>
            <AlertDescription>{clientError}</AlertDescription>
          </Alert>
        ) : null}
        {apiError ? (
          <Alert variant="destructive">
            <AlertTitle>Save failed</AlertTitle>
            <AlertDescription>{apiError}</AlertDescription>
          </Alert>
        ) : null}
        {successMessage ? (
          <div
            role="status"
            className="rounded-lg border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-900"
          >
            {successMessage}
          </div>
        ) : null}

        <div className="grid gap-3 sm:grid-cols-2">
          <div className="space-y-1 sm:col-span-2">
            <Label htmlFor={`ae-title-${action.id}`}>Title</Label>
            <Input
              id={`ae-title-${action.id}`}
              value={title}
              onChange={(ev) => setTitle(ev.target.value)}
              disabled={isSubmitting}
              autoComplete="off"
            />
          </div>
          <div className="space-y-1 sm:col-span-2">
            <Label htmlFor={`ae-desc-${action.id}`}>Description</Label>
            <Textarea
              id={`ae-desc-${action.id}`}
              value={description}
              onChange={(ev) => setDescription(ev.target.value)}
              disabled={isSubmitting}
              rows={3}
            />
          </div>
          <div className="space-y-1">
            <Label htmlFor={`ae-type-${action.id}`}>Action type</Label>
            <Select
              id={`ae-type-${action.id}`}
              value={actionType}
              onChange={(ev) => setActionType(ev.target.value as ActionType)}
              disabled={isSubmitting}
            >
              {ACTION_TYPES.map((t) => (
                <option key={t} value={t}>
                  {t}
                </option>
              ))}
            </Select>
          </div>
          <div className="space-y-1">
            <Label htmlFor={`ae-status-${action.id}`}>Status</Label>
            <Select
              id={`ae-status-${action.id}`}
              value={status}
              onChange={(ev) => setStatus(ev.target.value as ActionStatus)}
              disabled={isSubmitting}
            >
              {ACTION_STATUSES.map((s) => (
                <option key={s} value={s}>
                  {s}
                </option>
              ))}
            </Select>
          </div>
          <div className="space-y-1 sm:col-span-2">
            <Label htmlFor={`ae-sector-${action.id}`}>Sector</Label>
            <Input
              id={`ae-sector-${action.id}`}
              value={sector}
              onChange={(ev) => setSector(ev.target.value)}
              disabled={isSubmitting}
              autoComplete="off"
            />
          </div>
          <div className="space-y-1">
            <Label htmlFor={`ae-office-${action.id}`}>Responsible office</Label>
            <Input
              id={`ae-office-${action.id}`}
              value={responsibleOffice}
              onChange={(ev) => setResponsibleOffice(ev.target.value)}
              disabled={isSubmitting}
              autoComplete="organization"
            />
          </div>
          <div className="space-y-1">
            <Label htmlFor={`ae-budget-${action.id}`}>Budget amount (PHP)</Label>
            <Input
              id={`ae-budget-${action.id}`}
              type="number"
              inputMode="decimal"
              min={0}
              step="0.01"
              value={budgetAmount}
              onChange={(ev) => setBudgetAmount(ev.target.value)}
              disabled={isSubmitting}
            />
          </div>
          <div className="space-y-1 sm:col-span-2">
            <Label htmlFor={`ae-funding-${action.id}`}>Funding source</Label>
            <Input
              id={`ae-funding-${action.id}`}
              value={fundingSource}
              onChange={(ev) => setFundingSource(ev.target.value)}
              disabled={isSubmitting}
              autoComplete="off"
            />
          </div>
          <div className="space-y-1">
            <Label htmlFor={`ae-start-${action.id}`}>Timeline start</Label>
            <Input
              id={`ae-start-${action.id}`}
              type="datetime-local"
              value={timelineStartLocal}
              onChange={(ev) => setTimelineStartLocal(ev.target.value)}
              disabled={isSubmitting}
            />
          </div>
          <div className="space-y-1">
            <Label htmlFor={`ae-end-${action.id}`}>Timeline end</Label>
            <Input
              id={`ae-end-${action.id}`}
              type="datetime-local"
              value={timelineEndLocal}
              onChange={(ev) => setTimelineEndLocal(ev.target.value)}
              disabled={isSubmitting}
            />
          </div>
          <div className="space-y-1 sm:col-span-2">
            <Label htmlFor={`ae-kpi-${action.id}`}>KPI</Label>
            <Input id={`ae-kpi-${action.id}`} value={kpi} onChange={(ev) => setKpi(ev.target.value)} disabled={isSubmitting} />
          </div>
          <div className="space-y-1">
            <Label htmlFor={`ae-priority-${action.id}`}>Priority score</Label>
            <Input
              id={`ae-priority-${action.id}`}
              type="number"
              inputMode="numeric"
              min={0}
              max={100}
              step="1"
              value={priorityScore}
              onChange={(ev) => setPriorityScore(ev.target.value)}
              placeholder="Optional"
              disabled={isSubmitting}
            />
          </div>
        </div>
        <div className="flex flex-wrap gap-2 pt-1">
          <Button type="submit" size="sm" disabled={isSubmitting}>
            {isSubmitting ? "Saving…" : "Save"}
          </Button>
          <Button type="button" variant="outline" size="sm" disabled={isSubmitting} onClick={onCancel}>
            Cancel
          </Button>
        </div>
      </form>
    </div>
  );
}
