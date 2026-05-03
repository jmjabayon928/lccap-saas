"use client";

import { useMemo, useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { isApiError } from "@/lib/api/api-error";
import { monitoringClient } from "@/lib/monitoring/monitoring-client";
import type {
  MonitoringIndicatorSummary,
  MonitoringStatus,
  UpdateMonitoringIndicatorRequest
} from "@/types/monitoring";

const MONITORING_STATUSES: readonly MonitoringStatus[] = [
  "NotStarted",
  "InProgress",
  "OnTrack",
  "Delayed",
  "Completed"
];

function formatError(err: unknown): string {
  if (isApiError(err)) {
    return err.message;
  }
  if (err instanceof Error) {
    return err.message;
  }
  return "Could not save changes.";
}

function parseOptionalNumberField(raw: string): number | null {
  const t = raw.trim();
  if (!t) {
    return null;
  }
  const n = Number(t);
  if (!Number.isFinite(n)) {
    throw new Error("INVALID_NUMBER");
  }
  return n;
}

export interface IndicatorEditFormProps {
  readonly indicator: MonitoringIndicatorSummary;
  readonly onSaved: (updated: MonitoringIndicatorSummary) => void;
  readonly onCancel: () => void;
}

export function IndicatorEditForm({ indicator, onSaved, onCancel }: IndicatorEditFormProps) {
  const initialKey = useMemo(() => `${indicator.id}-${indicator.rowVersion}`, [indicator.id, indicator.rowVersion]);

  const [name, setName] = useState(indicator.name);
  const [description, setDescription] = useState(indicator.description ?? "");
  const [unit, setUnit] = useState(indicator.unit ?? "");
  const [baselineValue, setBaselineValue] = useState(
    indicator.baselineValue === null || indicator.baselineValue === undefined ? "" : String(indicator.baselineValue)
  );
  const [targetValue, setTargetValue] = useState(
    indicator.targetValue === null || indicator.targetValue === undefined ? "" : String(indicator.targetValue)
  );
  const [currentValue, setCurrentValue] = useState(
    indicator.currentValue === null || indicator.currentValue === undefined ? "" : String(indicator.currentValue)
  );
  const [progressPercent, setProgressPercent] = useState(
    indicator.progressPercent === null || indicator.progressPercent === undefined ? "" : String(indicator.progressPercent)
  );
  const [status, setStatus] = useState<MonitoringStatus>(indicator.status);
  const [frequency, setFrequency] = useState(indicator.frequency ?? "");
  const [responsibleOffice, setResponsibleOffice] = useState(indicator.responsibleOffice ?? "");
  const [clientError, setClientError] = useState<string | null>(null);
  const [apiError, setApiError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  function validate(): string | null {
    const n = name.trim();
    if (!n) {
      return "Name is required.";
    }
    if (n.length > 250) {
      return "Name must be 250 characters or fewer.";
    }
    if (unit.trim().length > 80) {
      return "Unit must be 80 characters or fewer.";
    }
    if (!MONITORING_STATUSES.includes(status)) {
      return "Status is not valid.";
    }
    try {
      parseOptionalNumberField(baselineValue);
      parseOptionalNumberField(targetValue);
      parseOptionalNumberField(currentValue);
    } catch {
      return "Baseline, target, and current values must be valid numbers when provided.";
    }
    const pp = progressPercent.trim();
    if (pp) {
      const p = Number(pp);
      if (!Number.isFinite(p) || p < 0 || p > 100) {
        return "Progress must be between 0 and 100 when provided.";
      }
    }
    return null;
  }

  function buildRequest(): UpdateMonitoringIndicatorRequest {
    const baselineValueOut = baselineValue.trim() === "" ? null : parseOptionalNumberField(baselineValue);
    const targetValueOut = targetValue.trim() === "" ? null : parseOptionalNumberField(targetValue);
    const currentValueOut = currentValue.trim() === "" ? null : parseOptionalNumberField(currentValue);
    const ppRaw = progressPercent.trim();
    const progressPercentOut = ppRaw === "" ? null : Number(ppRaw);
    if (progressPercentOut !== null && (!Number.isFinite(progressPercentOut) || progressPercentOut < 0 || progressPercentOut > 100)) {
      throw new Error("INVALID_PROGRESS");
    }
    return {
      name: name.trim(),
      description: description.trim() ? description.trim() : null,
      unit: unit.trim() ? unit.trim() : null,
      baselineValue: baselineValueOut,
      targetValue: targetValueOut,
      currentValue: currentValueOut,
      progressPercent: progressPercentOut,
      status,
      frequency: frequency.trim() ? frequency.trim() : null,
      responsibleOffice: responsibleOffice.trim() ? responsibleOffice.trim() : null,
      rowVersion: indicator.rowVersion
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

    let request: UpdateMonitoringIndicatorRequest;
    try {
      request = buildRequest();
    } catch {
      setClientError("Please check numeric fields and progress (0–100) and try again.");
      return;
    }

    setIsSubmitting(true);
    try {
      const updated = await monitoringClient.updateIndicator(indicator.id, request);
      onSaved(updated);
      setSuccessMessage("Indicator updated.");
      window.setTimeout(() => setSuccessMessage(null), 4000);
    } catch (err) {
      setApiError(formatError(err));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="rounded-lg border border-border bg-slate-50/80 p-4 text-sm shadow-inner" key={initialKey}>
      <h3 className="font-semibold text-slate-900">Edit indicator</h3>
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
            <Label htmlFor={`ie-name-${indicator.id}`}>Name</Label>
            <Input
              id={`ie-name-${indicator.id}`}
              value={name}
              onChange={(ev) => setName(ev.target.value)}
              disabled={isSubmitting}
              autoComplete="off"
            />
          </div>
          <div className="space-y-1 sm:col-span-2">
            <Label htmlFor={`ie-desc-${indicator.id}`}>Description</Label>
            <Textarea
              id={`ie-desc-${indicator.id}`}
              value={description}
              onChange={(ev) => setDescription(ev.target.value)}
              disabled={isSubmitting}
              rows={3}
            />
          </div>
          <div className="space-y-1">
            <Label htmlFor={`ie-unit-${indicator.id}`}>Unit</Label>
            <Input id={`ie-unit-${indicator.id}`} value={unit} onChange={(ev) => setUnit(ev.target.value)} disabled={isSubmitting} />
          </div>
          <div className="space-y-1">
            <Label htmlFor={`ie-status-${indicator.id}`}>Status</Label>
            <Select
              id={`ie-status-${indicator.id}`}
              value={status}
              onChange={(ev) => setStatus(ev.target.value as MonitoringStatus)}
              disabled={isSubmitting}
            >
              {MONITORING_STATUSES.map((s) => (
                <option key={s} value={s}>
                  {s}
                </option>
              ))}
            </Select>
          </div>
          <div className="space-y-1">
            <Label htmlFor={`ie-base-${indicator.id}`}>Baseline value</Label>
            <Input
              id={`ie-base-${indicator.id}`}
              inputMode="decimal"
              value={baselineValue}
              onChange={(ev) => setBaselineValue(ev.target.value)}
              disabled={isSubmitting}
            />
          </div>
          <div className="space-y-1">
            <Label htmlFor={`ie-cur-${indicator.id}`}>Current value</Label>
            <Input
              id={`ie-cur-${indicator.id}`}
              inputMode="decimal"
              value={currentValue}
              onChange={(ev) => setCurrentValue(ev.target.value)}
              disabled={isSubmitting}
            />
          </div>
          <div className="space-y-1">
            <Label htmlFor={`ie-tgt-${indicator.id}`}>Target value</Label>
            <Input
              id={`ie-tgt-${indicator.id}`}
              inputMode="decimal"
              value={targetValue}
              onChange={(ev) => setTargetValue(ev.target.value)}
              disabled={isSubmitting}
            />
          </div>
          <div className="space-y-1">
            <Label htmlFor={`ie-pct-${indicator.id}`}>Progress (%)</Label>
            <Input
              id={`ie-pct-${indicator.id}`}
              inputMode="numeric"
              value={progressPercent}
              onChange={(ev) => setProgressPercent(ev.target.value)}
              placeholder="0–100"
              disabled={isSubmitting}
            />
          </div>
          <div className="space-y-1">
            <Label htmlFor={`ie-freq-${indicator.id}`}>Frequency</Label>
            <Input id={`ie-freq-${indicator.id}`} value={frequency} onChange={(ev) => setFrequency(ev.target.value)} disabled={isSubmitting} />
          </div>
          <div className="space-y-1 sm:col-span-2">
            <Label htmlFor={`ie-office-${indicator.id}`}>Responsible office</Label>
            <Input
              id={`ie-office-${indicator.id}`}
              value={responsibleOffice}
              onChange={(ev) => setResponsibleOffice(ev.target.value)}
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
