"use client";

import { useMemo, useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { isApiError } from "@/lib/api/api-error";
import { documentClient } from "@/lib/documents/document-client";
import {
  DOCUMENT_CATEGORIES,
  type DocumentCategory,
  type EvidenceStatus,
  type DocumentSummary,
  type UpdateDocumentMetadataRequest
} from "@/types/documents";
import type { ActionItemSummary } from "@/types/actions";
import type { PlanSectionSummary } from "@/types/plans";
import {
  buildActionLinkOptions,
  buildSectionLinkOptions,
  enrichActionOptionsWithCurrentIfMissing,
  enrichSectionOptionsWithCurrentIfMissing
} from "@/components/documents/document-link-select-options";

function isCommandLike(value: string): boolean {
  const lower = value.toLowerCase();
  return (
    lower.includes("npm run") ||
    lower.includes("$env:") ||
    lower.includes("dotnet run") ||
    lower.includes("git ") ||
    lower.includes("powershell")
  );
}

function normalizeTagsFromCommaInput(raw: string): { ok: true; tags: string[] } | { ok: false; error: string } {
  const parts = raw.split(",");
  const seen = new Set<string>();
  const tags: string[] = [];
  for (const part of parts) {
    const t = part.trim();
    if (!t) {
      continue;
    }
    if (t.length > 50) {
      return { ok: false, error: "Each tag must be 50 characters or fewer." };
    }
    const key = t.toLowerCase();
    if (seen.has(key)) {
      continue;
    }
    seen.add(key);
    tags.push(t);
    if (tags.length > 20) {
      return { ok: false, error: "At most 20 tags are allowed." };
    }
  }
  return { ok: true, tags };
}

function toDateInputValue(isoOrYmd: string | null): string {
  if (!isoOrYmd || !isoOrYmd.trim()) {
    return "";
  }
  const s = isoOrYmd.trim();
  if (/^\d{4}-\d{2}-\d{2}$/.test(s)) {
    return s;
  }
  const d = new Date(s);
  if (Number.isNaN(d.getTime())) {
    return "";
  }
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${y}-${m}-${day}`;
}

function fromDateInputValue(value: string): string | null {
  const v = value.trim();
  if (!v) {
    return null;
  }
  if (!/^\d{4}-\d{2}-\d{2}$/.test(v)) {
    return null;
  }
  return v;
}

function formatError(err: unknown): string {
  if (isApiError(err)) {
    return err.message;
  }
  if (err instanceof Error) {
    return err.message;
  }
  return "Could not save changes.";
}

export interface DocumentEditFormProps {
  readonly document: DocumentSummary;
  readonly planSections: readonly PlanSectionSummary[];
  readonly actionItems: readonly ActionItemSummary[];
  readonly onSaved: (updated: DocumentSummary) => void;
  readonly onCancel: () => void;
}

export function DocumentEditForm({ document, planSections, actionItems, onSaved, onCancel }: DocumentEditFormProps) {
  const initialTagsText = useMemo(() => document.tags.join(", "), [document.tags]);

  const [category, setCategory] = useState<DocumentCategory>(() => {
    const c = document.category;
    return DOCUMENT_CATEGORIES.includes(c as DocumentCategory) ? (c as DocumentCategory) : "Reference";
  });
  const [title, setTitle] = useState(document.title ?? "");
  const [description, setDescription] = useState(document.description ?? "");
  const [sourceAgency, setSourceAgency] = useState(document.sourceAgency ?? "");
  const [documentDate, setDocumentDate] = useState(() => toDateInputValue(document.documentDate));
  const [tagsText, setTagsText] = useState(initialTagsText);
  const [evidenceStatus, setEvidenceStatus] = useState<EvidenceStatus>(document.evidenceStatus ?? "Internal");
  const [linkedSectionId, setLinkedSectionId] = useState<string>(
    () => document.planSectionId?.trim() ?? ""
  );
  const [linkedActionId, setLinkedActionId] = useState<string>(() => document.actionItemId?.trim() ?? "");
  const [clientError, setClientError] = useState<string | null>(null);
  const [apiError, setApiError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const sectionSelectOptions = useMemo(() => {
    const built = buildSectionLinkOptions(planSections);
    return enrichSectionOptionsWithCurrentIfMissing(built, document.planSectionId);
  }, [planSections, document.planSectionId]);

  const actionSelectOptions = useMemo(() => {
    const built = buildActionLinkOptions(actionItems);
    return enrichActionOptionsWithCurrentIfMissing(built, document.actionItemId);
  }, [actionItems, document.actionItemId]);

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>): Promise<void> {
    e.preventDefault();
    setClientError(null);
    setApiError(null);
    setSuccessMessage(null);

    const trimmedTitle = title.trim();
    if (trimmedTitle.length > 250) {
      setClientError("Title must be 250 characters or fewer.");
      return;
    }

    if (trimmedTitle.length > 0 && isCommandLike(trimmedTitle)) {
      setClientError("Document title looks like a local command. Please enter a descriptive document title.");
      return;
    }

    const sa = sourceAgency.trim();
    if (sa.length > 200) {
      setClientError("Source agency must be 200 characters or fewer.");
      return;
    }

    const tagResult = normalizeTagsFromCommaInput(tagsText);
    if (!tagResult.ok) {
      setClientError(tagResult.error);
      return;
    }

    const request: UpdateDocumentMetadataRequest = {
      category,
      title: trimmedTitle.length > 0 ? trimmedTitle : null,
      description: description.trim().length > 0 ? description.trim() : null,
      documentDate: fromDateInputValue(documentDate),
      sourceAgency: sa.length > 0 ? sa : null,
      tags: tagResult.tags,
      evidenceStatus,
      planSectionId: linkedSectionId.trim().length > 0 ? linkedSectionId.trim() : null,
      actionItemId: linkedActionId.trim().length > 0 ? linkedActionId.trim() : null
    };

    setIsSubmitting(true);
    try {
      const updated = await documentClient.updateDocumentMetadata(document.id, request);
      onSaved(updated);
      setSuccessMessage("Metadata saved.");
      window.setTimeout(() => setSuccessMessage(null), 4000);
    } catch (err) {
      setApiError(formatError(err));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="rounded-lg border border-border bg-slate-50/80 p-4 text-sm shadow-inner">
      <h3 className="font-semibold text-slate-900">Edit metadata</h3>
      <p className="mt-1 text-xs text-muted-foreground">File content cannot be changed here—only catalog fields.</p>
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
            <Label htmlFor={`doc-edit-title-${document.id}`}>Title</Label>
            <Input
              id={`doc-edit-title-${document.id}`}
              value={title}
              onChange={(ev) => setTitle(ev.target.value)}
              disabled={isSubmitting}
              autoComplete="off"
              placeholder="Optional title"
            />
          </div>
          <div className="space-y-1">
            <Label htmlFor={`doc-edit-category-${document.id}`}>Category</Label>
            <Select
              id={`doc-edit-category-${document.id}`}
              value={category}
              onChange={(ev) => setCategory(ev.target.value as DocumentCategory)}
              disabled={isSubmitting}
            >
              {DOCUMENT_CATEGORIES.map((c) => (
                <option key={c} value={c}>
                  {c}
                </option>
              ))}
            </Select>
          </div>
          <div className="space-y-1">
            <Label htmlFor={`doc-edit-date-${document.id}`}>Document date</Label>
            <Input
              id={`doc-edit-date-${document.id}`}
              type="date"
              value={documentDate}
              onChange={(ev) => setDocumentDate(ev.target.value)}
              disabled={isSubmitting}
            />
          </div>
          <div className="space-y-1">
            <Label htmlFor={`doc-edit-evidence-${document.id}`}>Evidence status</Label>
            <Select
              id={`doc-edit-evidence-${document.id}`}
              value={evidenceStatus}
              onChange={(ev) => setEvidenceStatus(ev.target.value as EvidenceStatus)}
              disabled={isSubmitting}
            >
              <option value="Draft">Draft</option>
              <option value="Internal">Internal</option>
              <option value="Official">Official</option>
              <option value="Public">Public</option>
            </Select>
          </div>
          <div className="space-y-1">
            <Label htmlFor={`doc-edit-linked-section-${document.id}`}>Linked LCCAP section</Label>
            <Select
              id={`doc-edit-linked-section-${document.id}`}
              value={linkedSectionId}
              onChange={(ev) => setLinkedSectionId(ev.target.value)}
              disabled={isSubmitting}
            >
              <option value="">No linked section</option>
              {sectionSelectOptions.map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label}
                </option>
              ))}
            </Select>
          </div>
          <div className="space-y-1">
            <Label htmlFor={`doc-edit-linked-action-${document.id}`}>Linked action item</Label>
            <Select
              id={`doc-edit-linked-action-${document.id}`}
              value={linkedActionId}
              onChange={(ev) => setLinkedActionId(ev.target.value)}
              disabled={isSubmitting}
            >
              <option value="">No linked action</option>
              {actionSelectOptions.map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label}
                </option>
              ))}
            </Select>
          </div>
          <div className="space-y-1 sm:col-span-2">
            <Label htmlFor={`doc-edit-source-${document.id}`}>Source agency</Label>
            <Input
              id={`doc-edit-source-${document.id}`}
              value={sourceAgency}
              onChange={(ev) => setSourceAgency(ev.target.value)}
              disabled={isSubmitting}
              autoComplete="off"
            />
          </div>
          <div className="space-y-1 sm:col-span-2">
            <Label htmlFor={`doc-edit-tags-${document.id}`}>Tags (comma-separated)</Label>
            <Input
              id={`doc-edit-tags-${document.id}`}
              value={tagsText}
              onChange={(ev) => setTagsText(ev.target.value)}
              disabled={isSubmitting}
              autoComplete="off"
              placeholder="e.g. Annex, Flood map"
            />
          </div>
          <div className="space-y-1 sm:col-span-2">
            <Label htmlFor={`doc-edit-desc-${document.id}`}>Description</Label>
            <Textarea
              id={`doc-edit-desc-${document.id}`}
              value={description}
              onChange={(ev) => setDescription(ev.target.value)}
              disabled={isSubmitting}
              rows={3}
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
