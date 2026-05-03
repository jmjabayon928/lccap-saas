"use client";

import { useCallback, useEffect, useState } from "react";
import { CheckCircle2, Loader2, Pencil, Plus } from "lucide-react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { isApiError } from "@/lib/api/api-error";
import { monitoringClient } from "@/lib/monitoring/monitoring-client";
import type {
  CreateMonitoringIndicatorRequest,
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

interface IndicatorFormProps {
  readonly planId: string;
  readonly selectedIndicator?: MonitoringIndicatorSummary | null;
  readonly onSaved: (indicator: MonitoringIndicatorSummary) => void;
  readonly onCancelEdit?: () => void;
}

function describeSubmitError(err: unknown): string {
  if (isApiError(err)) {
    const notFound = err.status === 404;
    const forbidden = err.status === 403;
    if (notFound || forbidden) {
      return "This indicator is unavailable or you do not have access to it in your current session.";
    }
    return err.message;
  }
  if (err instanceof Error) {
    return err.message;
  }
  return "Could not save the indicator.";
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

export function IndicatorForm({ planId, selectedIndicator, onSaved, onCancelEdit }: IndicatorFormProps) {
  const isEdit = Boolean(selectedIndicator);

  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [unit, setUnit] = useState("");
  const [baselineValue, setBaselineValue] = useState("");
  const [targetValue, setTargetValue] = useState("");
  const [currentValue, setCurrentValue] = useState("");
  const [progressPercent, setProgressPercent] = useState("");
  const [status, setStatus] = useState<MonitoringStatus>("NotStarted");
  const [frequency, setFrequency] = useState("");
  const [responsibleOffice, setResponsibleOffice] = useState("");

  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const applyFromIndicator = useCallback((row: MonitoringIndicatorSummary | null | undefined) => {
    if (!row) {
      setName("");
      setDescription("");
      setUnit("");
      setBaselineValue("");
      setTargetValue("");
      setCurrentValue("");
      setProgressPercent("");
      setStatus("NotStarted");
      setFrequency("");
      setResponsibleOffice("");
      return;
    }
    setName(row.name);
    setDescription(row.description ?? "");
    setUnit(row.unit ?? "");
    setBaselineValue(row.baselineValue === null || row.baselineValue === undefined ? "" : String(row.baselineValue));
    setTargetValue(row.targetValue === null || row.targetValue === undefined ? "" : String(row.targetValue));
    setCurrentValue(row.currentValue === null || row.currentValue === undefined ? "" : String(row.currentValue));
    setProgressPercent(
      row.progressPercent === null || row.progressPercent === undefined ? "" : String(row.progressPercent)
    );
    setStatus(row.status);
    setFrequency(row.frequency ?? "");
    setResponsibleOffice(row.responsibleOffice ?? "");
  }, []);

  useEffect(() => {
    applyFromIndicator(selectedIndicator ?? null);
    setFormError(null);
    setSuccessMessage(null);
  }, [applyFromIndicator, selectedIndicator]);

  function validateFieldValues(): string | null {
    if (!name.trim()) {
      return "Name is required.";
    }
    if (name.trim().length > 250) {
      return "Name must be 250 characters or fewer.";
    }
    if (unit.trim().length > 80) {
      return "Unit must be 80 characters or fewer.";
    }
    if (!MONITORING_STATUSES.includes(status)) {
      return "Status must be a supported monitoring status.";
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

  function buildNumericPayload(): {
    baselineValue: number | null;
    targetValue: number | null;
    currentValue: number | null;
    progressPercent: number | null;
  } {
    const baselineValueOut = baselineValue.trim() === "" ? null : parseOptionalNumberField(baselineValue);
    const targetValueOut = targetValue.trim() === "" ? null : parseOptionalNumberField(targetValue);
    const currentValueOut = currentValue.trim() === "" ? null : parseOptionalNumberField(currentValue);
    const ppRaw = progressPercent.trim();
    const progressPercentOut = ppRaw === "" ? null : Number(ppRaw);
    if (progressPercentOut !== null && (!Number.isFinite(progressPercentOut) || progressPercentOut < 0 || progressPercentOut > 100)) {
      throw new Error("INVALID_PROGRESS");
    }
    return {
      baselineValue: baselineValueOut,
      targetValue: targetValueOut,
      currentValue: currentValueOut,
      progressPercent: progressPercentOut
    };
  }

  function buildCreateRequest(): CreateMonitoringIndicatorRequest {
    const nums = buildNumericPayload();
    return {
      planId,
      name: name.trim(),
      description: description.trim() ? description : null,
      unit: unit.trim() ? unit : null,
      baselineValue: nums.baselineValue,
      targetValue: nums.targetValue,
      currentValue: nums.currentValue,
      progressPercent: nums.progressPercent,
      status,
      frequency: frequency.trim() ? frequency : null,
      responsibleOffice: responsibleOffice.trim() ? responsibleOffice : null
    };
  }

  function buildUpdateRequest(): UpdateMonitoringIndicatorRequest {
    if (!selectedIndicator) {
      throw new Error("MISSING_INDICATOR");
    }
    const nums = buildNumericPayload();
    return {
      name: name.trim(),
      description: description.trim() ? description : null,
      unit: unit.trim() ? unit : null,
      baselineValue: nums.baselineValue,
      targetValue: nums.targetValue,
      currentValue: nums.currentValue,
      progressPercent: nums.progressPercent,
      status,
      frequency: frequency.trim() ? frequency : null,
      responsibleOffice: responsibleOffice.trim() ? responsibleOffice : null,
      rowVersion: selectedIndicator.rowVersion
    };
  }

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>): Promise<void> {
    e.preventDefault();
    setFormError(null);
    setSuccessMessage(null);

    const v = validateFieldValues();
    if (v) {
      setFormError(v);
      return;
    }

    let createReq: CreateMonitoringIndicatorRequest;
    let updateReq: UpdateMonitoringIndicatorRequest;
    try {
      createReq = buildCreateRequest();
      updateReq = buildUpdateRequest();
    } catch {
      setFormError("Please check numeric fields and progress (0–100) and try again.");
      return;
    }

    setSubmitting(true);
    try {
      if (isEdit && selectedIndicator) {
        const updated = await monitoringClient.updateIndicator(selectedIndicator.id, updateReq);
        onSaved(updated);
        applyFromIndicator(updated);
        setSuccessMessage("Indicator updated.");
      } else {
        const created = await monitoringClient.createIndicator(createReq);
        onSaved(created);
        applyFromIndicator(null);
        setSuccessMessage("Indicator created.");
      }
    } catch (err) {
      setFormError(describeSubmitError(err));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <Card className="border-border shadow-sm">
      <CardHeader>
        <div className="flex flex-wrap items-start justify-between gap-2">
          <div>
            <CardTitle className="flex items-center gap-2 text-lg">
              {isEdit ? (
                <>
                  <Pencil className="h-5 w-5 text-emerald-700" aria-hidden />
                  Edit indicator
                </>
              ) : (
                <>
                  <Plus className="h-5 w-5 text-emerald-700" aria-hidden />
                  New indicator
                </>
              )}
            </CardTitle>
            <CardDescription>
              Track implementation progress for this plan—workspace drafts that support export-ready packages.
            </CardDescription>
          </div>
          {isEdit && onCancelEdit ? (
            <Button type="button" variant="outline" size="sm" onClick={onCancelEdit}>
              Cancel edit
            </Button>
          ) : null}
        </div>
      </CardHeader>
      <CardContent>
        <form className="space-y-4" onSubmit={handleSubmit}>
          {formError ? (
            <Alert variant="destructive">
              <AlertTitle>Could not save</AlertTitle>
              <AlertDescription>{formError}</AlertDescription>
            </Alert>
          ) : null}

          {successMessage ? (
            <Alert className="border-emerald-200 bg-emerald-50 text-emerald-950 [&>svg]:text-emerald-700">
              <CheckCircle2 className="h-4 w-4" aria-hidden />
              <AlertTitle>Saved</AlertTitle>
              <AlertDescription>{successMessage}</AlertDescription>
            </Alert>
          ) : null}

          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="indicator-name">Name</Label>
              <Input
                id="indicator-name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                autoComplete="off"
                required
                disabled={submitting}
              />
            </div>

            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="indicator-description">Description</Label>
              <Textarea
                id="indicator-description"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={3}
                disabled={submitting}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="indicator-unit">Unit</Label>
              <Input
                id="indicator-unit"
                value={unit}
                onChange={(e) => setUnit(e.target.value)}
                autoComplete="off"
                disabled={submitting}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="indicator-status">Status</Label>
              <Select
                id="indicator-status"
                value={status}
                onChange={(e) => setStatus(e.target.value as MonitoringStatus)}
                disabled={submitting}
              >
                {MONITORING_STATUSES.map((s) => (
                  <option key={s} value={s}>
                    {s}
                  </option>
                ))}
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="indicator-baseline">Baseline value</Label>
              <Input
                id="indicator-baseline"
                inputMode="decimal"
                value={baselineValue}
                onChange={(e) => setBaselineValue(e.target.value)}
                autoComplete="off"
                disabled={submitting}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="indicator-current">Current value</Label>
              <Input
                id="indicator-current"
                inputMode="decimal"
                value={currentValue}
                onChange={(e) => setCurrentValue(e.target.value)}
                autoComplete="off"
                disabled={submitting}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="indicator-target">Target value</Label>
              <Input
                id="indicator-target"
                inputMode="decimal"
                value={targetValue}
                onChange={(e) => setTargetValue(e.target.value)}
                autoComplete="off"
                disabled={submitting}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="indicator-progress">Progress (%)</Label>
              <Input
                id="indicator-progress"
                inputMode="numeric"
                value={progressPercent}
                onChange={(e) => setProgressPercent(e.target.value)}
                placeholder="0–100"
                autoComplete="off"
                disabled={submitting}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="indicator-frequency">Frequency</Label>
              <Input
                id="indicator-frequency"
                value={frequency}
                onChange={(e) => setFrequency(e.target.value)}
                autoComplete="off"
                disabled={submitting}
              />
            </div>

            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="indicator-office">Responsible office</Label>
              <Input
                id="indicator-office"
                value={responsibleOffice}
                onChange={(e) => setResponsibleOffice(e.target.value)}
                autoComplete="off"
                disabled={submitting}
              />
            </div>
          </div>

          <Button type="submit" disabled={submitting} className="gap-2">
            {submitting ? <Loader2 className="h-4 w-4 animate-spin" aria-hidden /> : null}
            {isEdit ? "Save changes" : "Create indicator"}
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}
