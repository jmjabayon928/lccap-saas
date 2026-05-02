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
import { actionClient } from "@/lib/actions/action-client";
import { isApiError } from "@/lib/api/api-error";
import type {
  ActionItemDetail,
  ActionItemSummary,
  ActionStatus,
  ActionType,
  CreateActionItemRequest,
  SaveActionItemResult
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

interface ActionFormProps {
  readonly planId: string;
  readonly selectedAction?: ActionItemDetail | ActionItemSummary | null;
  readonly onSaved: (action: SaveActionItemResult) => void;
  readonly onCancelEdit?: () => void;
}

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

function describeSubmitError(err: unknown): string {
  if (isApiError(err)) {
    const notFound = err.status === 404;
    const forbidden = err.status === 403;
    if (notFound || forbidden) {
      return "This action is unavailable or you do not have access to it in your current session.";
    }
    return err.message;
  }
  if (err instanceof Error) {
    return err.message;
  }
  return "Could not save the action item.";
}

export function ActionForm({ planId, selectedAction, onSaved, onCancelEdit }: ActionFormProps) {
  const isEdit = Boolean(selectedAction);

  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [actionType, setActionType] = useState<ActionType>("Adaptation");
  const [sector, setSector] = useState("");
  const [responsibleOffice, setResponsibleOffice] = useState("");
  const [budgetAmount, setBudgetAmount] = useState("0");
  const [fundingSource, setFundingSource] = useState("");
  const [timelineStartLocal, setTimelineStartLocal] = useState("");
  const [timelineEndLocal, setTimelineEndLocal] = useState("");
  const [kpi, setKpi] = useState("");
  const [priorityScore, setPriorityScore] = useState("");
  const [status, setStatus] = useState<ActionStatus>("Planned");

  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const applyFromAction = useCallback((a: ActionItemDetail | ActionItemSummary | null | undefined) => {
    if (!a) {
      setTitle("");
      setDescription("");
      setActionType("Adaptation");
      setSector("");
      setResponsibleOffice("");
      setBudgetAmount("0");
      setFundingSource("");
      setTimelineStartLocal("");
      setTimelineEndLocal("");
      setKpi("");
      setPriorityScore("");
      setStatus("Planned");
      return;
    }
    setTitle(a.title);
    setDescription(a.description ?? "");
    setActionType(a.actionType);
    setSector(a.sector);
    setResponsibleOffice(a.responsibleOffice ?? "");
    setBudgetAmount(String(a.budgetAmount));
    setFundingSource(a.fundingSource ?? "");
    setTimelineStartLocal(isoToDatetimeLocalValue(a.timelineStartUtc));
    setTimelineEndLocal(isoToDatetimeLocalValue(a.timelineEndUtc));
    setKpi(a.kpi ?? "");
    setPriorityScore(a.priorityScore === null || a.priorityScore === undefined ? "" : String(a.priorityScore));
    setStatus(a.status);
  }, []);

  useEffect(() => {
    applyFromAction(selectedAction ?? null);
    setFormError(null);
    setSuccessMessage(null);
  }, [applyFromAction, selectedAction]);

  function validateFieldValues(): string | null {
    if (!title.trim()) {
      return "Title is required.";
    }
    if (!sector.trim()) {
      return "Sector is required.";
    }
    if (!ACTION_TYPES.includes(actionType)) {
      return "Action type must be Adaptation or Mitigation.";
    }
    const budget = Number(budgetAmount);
    if (!Number.isFinite(budget) || budget < 0) {
      return "Budget must be a number greater than or equal to zero.";
    }
    const startIso = datetimeLocalValueToIsoUtc(timelineStartLocal);
    const endIso = datetimeLocalValueToIsoUtc(timelineEndLocal);
    if (startIso && endIso) {
      if (new Date(startIso).getTime() > new Date(endIso).getTime()) {
        return "Timeline start must be on or before the end date.";
      }
    }
    if (priorityScore.trim()) {
      const p = Number(priorityScore);
      if (!Number.isFinite(p) || p < 0) {
        return "Priority score must be a number greater than or equal to zero.";
      }
    }
    return null;
  }

  function buildRequest(): CreateActionItemRequest {
    const budget = Number(budgetAmount);
    const startIso = datetimeLocalValueToIsoUtc(timelineStartLocal);
    const endIso = datetimeLocalValueToIsoUtc(timelineEndLocal);
    const priority = priorityScore.trim() === "" ? null : Number(priorityScore);
    if (priority !== null && !Number.isFinite(priority)) {
      throw new Error("Invalid priority score");
    }
    return {
      title: title.trim(),
      description: description.trim() ? description : null,
      actionType,
      sector: sector.trim(),
      responsibleOffice: responsibleOffice.trim() ? responsibleOffice : null,
      budgetAmount: budget,
      fundingSource: fundingSource.trim() ? fundingSource : null,
      timelineStartUtc: startIso,
      timelineEndUtc: endIso,
      kpi: kpi.trim() ? kpi : null,
      priorityScore: priority,
      status
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

    let request: CreateActionItemRequest;
    try {
      request = buildRequest();
    } catch {
      setFormError("Please check the form values and try again.");
      return;
    }

    setSubmitting(true);
    try {
      if (isEdit && selectedAction) {
        const saved = await actionClient.updateActionItem(selectedAction.id, request);
        onSaved(saved);
        setSuccessMessage("Action updated.");
      } else {
        const saved = await actionClient.createActionItem(planId, request);
        onSaved(saved);
        applyFromAction(null);
        setSuccessMessage("Action created.");
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
                  Edit action
                </>
              ) : (
                <>
                  <Plus className="h-5 w-5 text-emerald-700" aria-hidden />
                  New action
                </>
              )}
            </CardTitle>
            <CardDescription>
              Capture adaptation or mitigation measures as working drafts—scoped to this plan and tenant session.
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
              <Label htmlFor="action-title">Title</Label>
              <Input
                id="action-title"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                autoComplete="off"
                required
                disabled={submitting}
              />
            </div>

            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="action-description">Description</Label>
              <Textarea
                id="action-description"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={3}
                disabled={submitting}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="action-type">Action type</Label>
              <Select
                id="action-type"
                value={actionType}
                onChange={(e) => setActionType(e.target.value as ActionType)}
                disabled={submitting}
              >
                {ACTION_TYPES.map((t) => (
                  <option key={t} value={t}>
                    {t}
                  </option>
                ))}
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="action-status">Status</Label>
              <Select
                id="action-status"
                value={status}
                onChange={(e) => setStatus(e.target.value as ActionStatus)}
                disabled={submitting}
              >
                {ACTION_STATUSES.map((s) => (
                  <option key={s} value={s}>
                    {s}
                  </option>
                ))}
              </Select>
            </div>

            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="action-sector">Sector</Label>
              <Input
                id="action-sector"
                value={sector}
                onChange={(e) => setSector(e.target.value)}
                autoComplete="off"
                disabled={submitting}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="action-office">Responsible office</Label>
              <Input
                id="action-office"
                value={responsibleOffice}
                onChange={(e) => setResponsibleOffice(e.target.value)}
                autoComplete="organization"
                disabled={submitting}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="action-budget">Budget amount (PHP)</Label>
              <Input
                id="action-budget"
                type="number"
                inputMode="decimal"
                min={0}
                step="0.01"
                value={budgetAmount}
                onChange={(e) => setBudgetAmount(e.target.value)}
                disabled={submitting}
              />
            </div>

            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="action-funding">Funding source</Label>
              <Input
                id="action-funding"
                value={fundingSource}
                onChange={(e) => setFundingSource(e.target.value)}
                autoComplete="off"
                disabled={submitting}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="action-start">Timeline start</Label>
              <Input
                id="action-start"
                type="datetime-local"
                value={timelineStartLocal}
                onChange={(e) => setTimelineStartLocal(e.target.value)}
                disabled={submitting}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="action-end">Timeline end</Label>
              <Input
                id="action-end"
                type="datetime-local"
                value={timelineEndLocal}
                onChange={(e) => setTimelineEndLocal(e.target.value)}
                disabled={submitting}
              />
            </div>

            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="action-kpi">KPI</Label>
              <Input id="action-kpi" value={kpi} onChange={(e) => setKpi(e.target.value)} disabled={submitting} />
            </div>

            <div className="space-y-2">
              <Label htmlFor="action-priority">Priority score</Label>
              <Input
                id="action-priority"
                type="number"
                inputMode="numeric"
                min={0}
                step="1"
                value={priorityScore}
                onChange={(e) => setPriorityScore(e.target.value)}
                placeholder="Optional"
                disabled={submitting}
              />
            </div>
          </div>

          <div className="flex flex-wrap gap-2 pt-2">
            <Button type="submit" disabled={submitting} className="gap-2">
              {submitting ? <Loader2 className="h-4 w-4 animate-spin" aria-hidden /> : null}
              {isEdit ? "Save changes" : "Create action"}
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
