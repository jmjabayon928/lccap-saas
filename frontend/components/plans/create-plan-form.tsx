"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select } from "@/components/ui/select";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { isApiError } from "@/lib/api/api-error";
import { planClient } from "@/lib/plans/plan-client";
import type { PlanStatus, TemplateMode } from "@/types/plans";

const MIN_YEAR = 2000;
const MAX_YEAR = 2100;

function formatSubmitError(err: unknown): string {
  if (isApiError(err)) {
    // Backend returns { errors: string[] } for 400 Bad Request
    const details = err.details as { errors?: string[] } | undefined;
    if (details?.errors && Array.isArray(details.errors) && details.errors.length > 0) {
      return details.errors.join(" ");
    }
    return err.message;
  }
  if (err instanceof Error) {
    return err.message;
  }
  return "Could not create plan. Please try again.";
}

export function CreatePlanForm() {
  const router = useRouter();
  const [title, setTitle] = useState("");
  const [startYear, setStartYear] = useState("");
  const [endYear, setEndYear] = useState("");
  const [status, setStatus] = useState<PlanStatus>("Draft");
  const [templateMode, setTemplateMode] = useState<TemplateMode>("New");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function onSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setErrorMessage(null);

    const trimmedTitle = title.trim();
    if (!trimmedTitle) {
      setErrorMessage("Plan title is required.");
      return;
    }

    const sy = Number.parseInt(startYear, 10);
    const ey = Number.parseInt(endYear, 10);
    if (!Number.isFinite(sy) || !Number.isFinite(ey)) {
      setErrorMessage("Start year and end year are required.");
      return;
    }
    if (sy < MIN_YEAR || sy > MAX_YEAR || ey < MIN_YEAR || ey > MAX_YEAR) {
      setErrorMessage(`Years must be between ${MIN_YEAR} and ${MAX_YEAR}.`);
      return;
    }
    if (sy > ey) {
      setErrorMessage("Start year must be less than or equal to end year.");
      return;
    }

    setIsSubmitting(true);
    try {
      const result = await planClient.createPlan({
        title: trimmedTitle,
        startYear: sy,
        endYear: ey,
        status,
        templateMode,
        versionNumber: 1
      });
      router.push(`/plans/${encodeURIComponent(result.planId)}`);
      router.refresh();
    } catch (err) {
      setErrorMessage(formatSubmitError(err));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <Card className="border-border shadow-sm">
      <CardHeader>
        <CardTitle>Create plan</CardTitle>
        <CardDescription>
          Registers a new LCCAP workspace for your tenant. Default sections are provisioned by the API after creation.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={onSubmit} className="space-y-5">
          {errorMessage ? (
            <Alert variant="destructive">
              <AlertTitle>Create failed</AlertTitle>
              <AlertDescription>{errorMessage}</AlertDescription>
            </Alert>
          ) : null}

          <div className="space-y-2">
            <Label htmlFor="plan-title">Plan title</Label>
            <Input
              id="plan-title"
              name="title"
              value={title}
              onChange={(ev) => setTitle(ev.target.value)}
              disabled={isSubmitting}
              placeholder="e.g. City Climate Action Plan 2026–2030"
              autoComplete="off"
              required
            />
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="plan-start-year">Start year</Label>
              <Input
                id="plan-start-year"
                name="startYear"
                type="number"
                inputMode="numeric"
                min={MIN_YEAR}
                max={MAX_YEAR}
                value={startYear}
                onChange={(ev) => setStartYear(ev.target.value)}
                disabled={isSubmitting}
                required
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="plan-end-year">End year</Label>
              <Input
                id="plan-end-year"
                name="endYear"
                type="number"
                inputMode="numeric"
                min={MIN_YEAR}
                max={MAX_YEAR}
                value={endYear}
                onChange={(ev) => setEndYear(ev.target.value)}
                disabled={isSubmitting}
                required
              />
            </div>
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="plan-status">Status</Label>
              <Select
                id="plan-status"
                name="status"
                value={status}
                onChange={(ev) => setStatus(ev.target.value as PlanStatus)}
                disabled={isSubmitting}
              >
                <option value="Draft">Draft</option>
                <option value="InProgress">In progress</option>
                <option value="ReadyForExport">Ready for export</option>
                <option value="Submitted">Submitted</option>
                <option value="Approved">Approved</option>
                <option value="Archived">Archived</option>
              </Select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="plan-template">Template mode</Label>
              <Select
                id="plan-template"
                name="templateMode"
                value={templateMode}
                onChange={(ev) => setTemplateMode(ev.target.value as TemplateMode)}
                disabled={isSubmitting}
              >
                <option value="New">New</option>
                <option value="Partial">Partial</option>
                <option value="Enhancement">Enhancement</option>
              </Select>
            </div>
          </div>

          <Button type="submit" disabled={isSubmitting} className="w-full sm:w-auto">
            {isSubmitting ? "Creating plan…" : "Create plan"}
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}
