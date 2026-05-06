"use client";

import { useEffect, useMemo, useState } from "react";
import { CheckCircle2, Loader2, Plus } from "lucide-react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { isApiError } from "@/lib/api/api-error";
import { monitoringClient } from "@/lib/monitoring/monitoring-client";
import type { CreateMonitoringUpdateRequest, MonitoringStatus, MonitoringUpdateSummary } from "@/types/monitoring";

const MONITORING_STATUSES: readonly MonitoringStatus[] = [
  "NotStarted",
  "InProgress",
  "OnTrack",
  "Delayed",
  "Completed"
];

export interface MonitoringUpdateFormProps {
  readonly indicatorId: string;
  readonly defaultStatus?: MonitoringStatus;
  readonly onCreated: (update: MonitoringUpdateSummary) => void;
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
  return "Could not create the monitoring update.";
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

export function MonitoringUpdateForm({ indicatorId, defaultStatus, onCreated }: MonitoringUpdateFormProps) {
  const [periodLabel, setPeriodLabel] = useState("");
  const [actualValue, setActualValue] = useState("");
  const [progressPercent, setProgressPercent] = useState("");
  const [status, setStatus] = useState<MonitoringStatus>(defaultStatus ?? "NotStarted");
  const [notes, setNotes] = useState("");

  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const canSubmit = useMemo(() => {
    return Boolean(indicatorId) && !submitting;
  }, [indicatorId, submitting]);

  useEffect(() => {
    setStatus(defaultStatus ?? "NotStarted");
  }, [defaultStatus]);

  function validate(): string | null {
    if (!periodLabel.trim()) {
      return "Period label is required.";
    }
    if (periodLabel.trim().length > 50) {
      return "Period label must be 50 characters or fewer.";
    }
    if (!MONITORING_STATUSES.includes(status)) {
      return "Status must be a supported monitoring status.";
    }

    try {
      parseOptionalNumberField(actualValue);
    } catch {
      return "Actual value must be a valid number when provided.";
    }

    const pp = progressPercent.trim();
    if (pp) {
      const p = Number(pp);
      if (!Number.isFinite(p) || p < 0 || p > 100) {
        return "Progress must be between 0 and 100 when provided.";
      }
    }

    if (notes.trim().length > 2000) {
      return "Notes must be 2000 characters or fewer.";
    }

    return null;
  }

  function buildRequest(): CreateMonitoringUpdateRequest {
    const actualValueOut = actualValue.trim() === "" ? null : parseOptionalNumberField(actualValue);
    const ppRaw = progressPercent.trim();
    const progressPercentOut = ppRaw === "" ? null : Number(ppRaw);
    if (
      progressPercentOut !== null &&
      (!Number.isFinite(progressPercentOut) || progressPercentOut < 0 || progressPercentOut > 100)
    ) {
      throw new Error("INVALID_PROGRESS");
    }

    return {
      periodLabel: periodLabel.trim(),
      actualValue: actualValueOut,
      progressPercent: progressPercentOut,
      status,
      notes: notes.trim() ? notes : null
    };
  }

  async function onSubmit() {
    setFormError(null);
    setSuccessMessage(null);

    const v = validate();
    if (v) {
      setFormError(v);
      return;
    }

    let payload: CreateMonitoringUpdateRequest;
    try {
      payload = buildRequest();
    } catch {
      setFormError("Update values must be valid numbers when provided.");
      return;
    }

    setSubmitting(true);
    try {
      const created = await monitoringClient.createUpdate(indicatorId, payload);
      setSuccessMessage("Update saved.");
      onCreated(created);
      setPeriodLabel("");
      setActualValue("");
      setProgressPercent("");
      setNotes("");
    } catch (err) {
      setFormError(describeSubmitError(err));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Plus className="h-5 w-5" />
          Add update
        </CardTitle>
        <CardDescription>Log a periodic progress update for this indicator.</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {formError ? (
          <Alert variant="destructive">
            <AlertTitle>Could not save</AlertTitle>
            <AlertDescription>{formError}</AlertDescription>
          </Alert>
        ) : null}

        {successMessage ? (
          <Alert>
            <CheckCircle2 className="h-4 w-4" />
            <AlertTitle>Saved</AlertTitle>
            <AlertDescription>{successMessage}</AlertDescription>
          </Alert>
        ) : null}

        <div className="grid gap-3 md:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor="periodLabel">Period label</Label>
            <Input
              id="periodLabel"
              value={periodLabel}
              onChange={(e) => setPeriodLabel(e.target.value)}
              placeholder="e.g., Q2 2026"
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="status">Status</Label>
            <Select
              id="status"
              value={status}
              onChange={(e) => setStatus(e.target.value as MonitoringStatus)}
            >
              {MONITORING_STATUSES.map((s) => (
                <option key={s} value={s}>
                  {s}
                </option>
              ))}
            </Select>
          </div>

          <div className="space-y-2">
            <Label htmlFor="actualValue">Actual value (optional)</Label>
            <Input
              id="actualValue"
              value={actualValue}
              onChange={(e) => setActualValue(e.target.value)}
              inputMode="decimal"
              placeholder="e.g., 42"
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="progressPercent">Progress % (optional)</Label>
            <Input
              id="progressPercent"
              value={progressPercent}
              onChange={(e) => setProgressPercent(e.target.value)}
              inputMode="decimal"
              placeholder="0 - 100"
            />
          </div>
        </div>

        <div className="space-y-2">
          <Label htmlFor="notes">Notes (optional)</Label>
          <Textarea
            id="notes"
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            placeholder="What changed since the last update?"
          />
        </div>

        <div className="flex justify-end">
          <Button onClick={onSubmit} disabled={!canSubmit}>
            {submitting ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
            Save update
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}

