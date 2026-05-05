"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { isApiError } from "@/lib/api/api-error";
import { planClient } from "@/lib/plans/plan-client";
import type { PlanSummary, PlanStatus, TemplateMode } from "@/types/plans";

const MIN_YEAR = 2000;
const MAX_YEAR = 2100;

interface PlanMetadataEditFormProps {
  plan: PlanSummary;
  onSuccess: (updatedPlan: PlanSummary) => void;
  onCancel: () => void;
}

function formatSubmitError(err: unknown): string {
  if (isApiError(err)) {
    if (err.status === 409) {
      return "This plan was changed elsewhere. Refresh and try again.";
    }
    const details = err.details as { errors?: Record<string, string[]> } | undefined;
    if (details?.errors?.RowVersion) {
      return "This plan needs to be refreshed before editing because it has no concurrency token.";
    }
    if (details?.errors) {
      const allErrors = Object.values(details.errors).flat();
      if (allErrors.length > 0) {
        return allErrors.join(" ");
      }
    }
    return err.message;
  }
  if (err instanceof Error) {
    return err.message;
  }
  return "Could not update plan. Please try again.";
}

export function PlanMetadataEditForm({ plan, onSuccess, onCancel }: PlanMetadataEditFormProps) {
  const [title, setTitle] = useState(plan.title);
  const [description, setDescription] = useState(plan.description ?? "");
  const [startYear, setStartYear] = useState(plan.startYear.toString());
  const [endYear, setEndYear] = useState(plan.endYear.toString());
  const [status, setStatus] = useState<PlanStatus>(plan.status);
  const [templateMode, setTemplateMode] = useState<TemplateMode>(plan.templateMode);
  const [versionNumber, setVersionNumber] = useState(plan.versionNumber.toString());
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function onSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setErrorMessage(null);

    if (plan.rowVersion === null) {
      setErrorMessage("This plan needs to be refreshed before editing because it has no concurrency token.");
      return;
    }

    const trimmedTitle = title.trim();
    if (!trimmedTitle) {
      setErrorMessage("Plan title is required.");
      return;
    }

    const sy = Number.parseInt(startYear, 10);
    const ey = Number.parseInt(endYear, 10);
    const vn = Number.parseInt(versionNumber, 10);

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
    if (!Number.isFinite(vn) || vn < 1) {
      setErrorMessage("Version number must be at least 1.");
      return;
    }

    setIsSubmitting(true);
    try {
      const updated = await planClient.updatePlanMetadata(plan.id, {
        title: trimmedTitle,
        description: description.trim() || null,
        startYear: sy,
        endYear: ey,
        status,
        templateMode,
        versionNumber: vn,
        rowVersion: plan.rowVersion
      });
      onSuccess(updated);
    } catch (err) {
      setErrorMessage(formatSubmitError(err));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-5 p-4 border rounded-md bg-slate-50">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold">Edit plan metadata</h3>
        <Button
          type="button"
          variant="ghost"
          size="sm"
          onClick={onCancel}
          disabled={isSubmitting}
        >
          Cancel
        </Button>
      </div>

      {errorMessage ? (
        <Alert variant="destructive">
          <AlertTitle>Update failed</AlertTitle>
          <AlertDescription>{errorMessage}</AlertDescription>
        </Alert>
      ) : null}

      <div className="space-y-2">
        <Label htmlFor="edit-plan-title">Plan title</Label>
        <Input
          id="edit-plan-title"
          value={title}
          onChange={(ev) => setTitle(ev.target.value)}
          disabled={isSubmitting}
          required
        />
      </div>

      <div className="space-y-2">
        <Label htmlFor="edit-plan-description">Description</Label>
        <Textarea
          id="edit-plan-description"
          value={description}
          onChange={(ev) => setDescription(ev.target.value)}
          disabled={isSubmitting}
          placeholder="Optional plan description..."
        />
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <div className="space-y-2">
          <Label htmlFor="edit-plan-start-year">Start year</Label>
          <Input
            id="edit-plan-start-year"
            type="number"
            min={MIN_YEAR}
            max={MAX_YEAR}
            value={startYear}
            onChange={(ev) => setStartYear(ev.target.value)}
            disabled={isSubmitting}
            required
          />
        </div>
        <div className="space-y-2">
          <Label htmlFor="edit-plan-end-year">End year</Label>
          <Input
            id="edit-plan-end-year"
            type="number"
            min={MIN_YEAR}
            max={MAX_YEAR}
            value={endYear}
            onChange={(ev) => setEndYear(ev.target.value)}
            disabled={isSubmitting}
            required
          />
        </div>
      </div>

      <div className="grid gap-4 sm:grid-cols-3">
        <div className="space-y-2">
          <Label htmlFor="edit-plan-status">Status</Label>
          <Select
            id="edit-plan-status"
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
          <Label htmlFor="edit-plan-template">Template mode</Label>
          <Select
            id="edit-plan-template"
            value={templateMode}
            onChange={(ev) => setTemplateMode(ev.target.value as TemplateMode)}
            disabled={isSubmitting}
          >
            <option value="New">New</option>
            <option value="Partial">Partial</option>
            <option value="Enhancement">Enhancement</option>
          </Select>
        </div>
        <div className="space-y-2">
          <Label htmlFor="edit-plan-version">Version</Label>
          <Input
            id="edit-plan-version"
            type="number"
            min="1"
            value={versionNumber}
            onChange={(ev) => setVersionNumber(ev.target.value)}
            disabled={isSubmitting}
            required
          />
        </div>
      </div>

      <div className="flex justify-end gap-3">
        <Button
          type="button"
          variant="outline"
          onClick={onCancel}
          disabled={isSubmitting}
        >
          Cancel
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Saving..." : "Save changes"}
        </Button>
      </div>
    </form>
  );
}
