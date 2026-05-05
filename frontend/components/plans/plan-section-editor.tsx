"use client";

import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { isApiError } from "@/lib/api/api-error";
import { planClient } from "@/lib/plans/plan-client";
import { SectionHistoryPanel } from "./section-history-panel";
import type { PlanSectionSummary, SavePlanSectionResult } from "@/types/plans";

function formatEdited(iso: string | null): string {
  if (!iso || !iso.trim()) {
    return "Not saved yet";
  }
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) {
    return iso;
  }
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(d);
}

function formatSaveError(err: unknown): string {
  if (isApiError(err)) {
    return err.message;
  }
  if (err instanceof Error) {
    return err.message;
  }
  return "Save failed. Please try again.";
}

export interface PlanSectionEditorProps {
  readonly planId: string;
  readonly section: PlanSectionSummary;
  readonly onSaved: (updated: PlanSectionSummary) => void;
}

export function PlanSectionEditor({ planId, section, onSaved }: PlanSectionEditorProps) {
  const [title, setTitle] = useState(section.title);
  const [content, setContent] = useState(section.content);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [showSuccess, setShowSuccess] = useState(false);

  useEffect(() => {
    setTitle(section.title);
    setContent(section.content);
    setErrorMessage(null);
    setShowSuccess(false);
  }, [section.id, section.sectionKey, section.title, section.content, section.lastEditedAtUtc]);

  useEffect(() => {
    if (!showSuccess) {
      return;
    }
    const t = window.setTimeout(() => setShowSuccess(false), 4000);
    return () => window.clearTimeout(t);
  }, [showSuccess]);

  async function handleSave(): Promise<void> {
    setErrorMessage(null);
    const trimmed = title.trim();
    if (!trimmed) {
      setErrorMessage("Title is required.");
      return;
    }

    setIsSaving(true);
    try {
      const result = await planClient.savePlanSection(planId, section.sectionKey, {
        title: trimmed,
        content,
        sortOrder: section.sortOrder
      });

      // Merge compact response with existing section data
      const updated: PlanSectionSummary = {
        ...section,
        id: result.sectionId, // backend returns sectionId
        title: trimmed,
        content,
        lastEditedAtUtc: result.lastEditedAtUtc
      };

      onSaved(updated);
      setShowSuccess(true);
    } catch (err) {
      setErrorMessage(formatSaveError(err));
    } finally {
      setIsSaving(false);
    }
  }

  function handleRestored(result: SavePlanSectionResult, restoredTitle: string, restoredContent: string): void {
    setTitle(restoredTitle);
    setContent(restoredContent);
    setShowSuccess(true);

    const updated: PlanSectionSummary = {
      ...section,
      id: result.sectionId,
      title: restoredTitle,
      content: restoredContent,
      lastEditedAtUtc: result.lastEditedAtUtc
    };

    onSaved(updated);
  }

  return (
    <Card className="border-border shadow-sm">
      <CardHeader className="space-y-1">
        <CardTitle className="text-lg">Edit section</CardTitle>
        <CardDescription>
          Changes are saved with your existing session; section key and order are controlled by the API.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form
          onSubmit={(ev) => {
            ev.preventDefault();
            void handleSave();
          }}
          className="space-y-5"
        >
          {errorMessage ? (
            <Alert variant="destructive">
              <AlertTitle>Save failed</AlertTitle>
              <AlertDescription>{errorMessage}</AlertDescription>
            </Alert>
          ) : null}

          {showSuccess ? (
            <div
              role="status"
              className="rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-900"
            >
              <p className="font-medium">Saved</p>
              <p className="mt-1 text-emerald-800/90">
                Section updated. The section list shows the latest title and timestamps.
              </p>
            </div>
          ) : null}

          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="section-key-display">Section key</Label>
              <Input id="section-key-display" value={section.sectionKey} readOnly tabIndex={-1} className="font-mono text-xs" />
            </div>
            <div className="space-y-2">
              <Label htmlFor="section-order-display">Sort order</Label>
              <Input
                id="section-order-display"
                value={String(section.sortOrder)}
                readOnly
                tabIndex={-1}
                className="tabular-nums"
              />
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="section-title-input">Title</Label>
            <Input
              id="section-title-input"
              value={title}
              onChange={(ev) => setTitle(ev.target.value)}
              disabled={isSaving}
              autoComplete="off"
              required
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="section-content-input">Content</Label>
            <Textarea
              id="section-content-input"
              value={content}
              onChange={(ev) => setContent(ev.target.value)}
              disabled={isSaving}
              placeholder="Draft narrative, bullets, or structured notes for this section."
              rows={12}
            />
            <p className="text-xs text-muted-foreground">Content may be left empty while you sketch the outline.</p>
          </div>

          <div className="flex flex-col gap-2 border-t border-border pt-4 text-sm text-muted-foreground sm:flex-row sm:items-center sm:justify-between">
            <div className="flex items-center gap-4">
              <span>Last edited: {formatEdited(section.lastEditedAtUtc)}</span>
              <SectionHistoryPanel planId={planId} sectionKey={section.sectionKey} onRestored={handleRestored} />
            </div>
            <Button type="submit" disabled={isSaving}>
              {isSaving ? "Saving…" : "Save section"}
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
