"use client";

import { useRef, useState } from "react";
import { Button } from "@/components/ui/button";
import { FileInput } from "@/components/ui/file-input";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { isApiError } from "@/lib/api/api-error";
import { documentClient, uploadResultToSummary } from "@/lib/documents/document-client";
import {
  ALLOWED_DOCUMENT_EXTENSIONS,
  DOCUMENT_CATEGORIES,
  MAX_DOCUMENT_UPLOAD_BYTES,
  type DocumentCategory,
  type DocumentSummary
} from "@/types/documents";

function extensionAllowed(fileName: string): boolean {
  const lower = fileName.toLowerCase();
  return ALLOWED_DOCUMENT_EXTENSIONS.some((ext) => lower.endsWith(ext));
}

function formatUploadError(err: unknown): string {
  if (isApiError(err)) {
    return err.message;
  }
  if (err instanceof Error) {
    return err.message;
  }
  return "Upload failed. Please try again.";
}

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

export interface DocumentUploadFormProps {
  readonly planId: string;
  readonly onUploaded: (document: DocumentSummary) => void;
}

export function DocumentUploadForm({ planId, onUploaded }: DocumentUploadFormProps) {
  const fileRef = useRef<HTMLInputElement>(null);
  const [title, setTitle] = useState("");
  const [category, setCategory] = useState<DocumentCategory>("Reference");
  const [description, setDescription] = useState("");
  const [clientError, setClientError] = useState<string | null>(null);
  const [apiError, setApiError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  function resetFileInput(): void {
    if (fileRef.current) {
      fileRef.current.value = "";
    }
  }

  async function handleUpload(e: React.FormEvent<HTMLFormElement>): Promise<void> {
    e.preventDefault();
    setClientError(null);
    setApiError(null);
    setSuccessMessage(null);

    const submittedTitle = title.trim();
    if (!submittedTitle) {
      setClientError("Title is required.");
      return;
    }

    if (isCommandLike(submittedTitle)) {
      setClientError("Document title looks like a local command. Please enter a descriptive document title.");
      return;
    }

    const file = fileRef.current?.files?.[0];
    if (!file) {
      setClientError("Choose a file to upload.");
      return;
    }
    if (file.size <= 0) {
      setClientError("File must not be empty.");
      return;
    }
    if (file.size > MAX_DOCUMENT_UPLOAD_BYTES) {
      setClientError(`File must be ${MAX_DOCUMENT_UPLOAD_BYTES / (1024 * 1024)} MB or smaller for this MVP UI check.`);
      return;
    }
    if (!extensionAllowed(file.name)) {
      setClientError(`Allowed types: ${ALLOWED_DOCUMENT_EXTENSIONS.join(", ")}`);
      return;
    }

    const snapshot = {
      planId,
      category,
      title: submittedTitle,
      description: description.trim() || null,
      file
    } as const;

    setIsSubmitting(true);
    try {
      const result = await documentClient.uploadDocument({
        planId: snapshot.planId,
        category: snapshot.category,
        title: snapshot.title,
        description: snapshot.description ?? undefined,
        file: snapshot.file
      });
      const summary = uploadResultToSummary(result, {
        planId: snapshot.planId,
        category: snapshot.category,
        title: snapshot.title,
        description: snapshot.description ?? undefined,
        file: snapshot.file
      });
      onUploaded(summary);
      setSuccessMessage(`Uploaded “${snapshot.title}”.`);
      setTitle("");
      setDescription("");
      resetFileInput();
      window.setTimeout(() => setSuccessMessage(null), 5000);
    } catch (err) {
      setApiError(formatUploadError(err));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <Card className="border-border shadow-sm">
      <CardHeader>
        <CardTitle>Upload document</CardTitle>
        <CardDescription>
          Attach a plan-scoped file for CLUP references, maps, studies, or annexes. Tenant scope follows your session.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={(ev) => void handleUpload(ev)} className="space-y-4">
          {clientError ? (
            <Alert variant="destructive">
              <AlertTitle>Check your input</AlertTitle>
              <AlertDescription>{clientError}</AlertDescription>
            </Alert>
          ) : null}
          {apiError ? (
            <Alert variant="destructive">
              <AlertTitle>Upload failed</AlertTitle>
              <AlertDescription>{apiError}</AlertDescription>
            </Alert>
          ) : null}
          {successMessage ? (
            <div role="status" className="rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-900">
              {successMessage}
            </div>
          ) : null}

          <div className="space-y-2">
            <Label htmlFor="doc-title">Title</Label>
            <Input
              id="doc-title"
              value={title}
              onChange={(ev) => setTitle(ev.target.value)}
              disabled={isSubmitting}
              autoComplete="off"
              required
              placeholder="Document title shown in the library"
            />
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="doc-category">Category</Label>
              <Select
                id="doc-category"
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
            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="doc-desc">Description (optional)</Label>
              <Textarea
                id="doc-desc"
                value={description}
                onChange={(ev) => setDescription(ev.target.value)}
                disabled={isSubmitting}
                rows={3}
                placeholder="Short note for reviewers (optional)"
              />
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="doc-file">File</Label>
            <FileInput
              id="doc-file"
              ref={fileRef}
              disabled={isSubmitting}
              accept={ALLOWED_DOCUMENT_EXTENSIONS.join(",")}
            />
            <p className="text-xs text-muted-foreground">
              Allowed: PDF, Word, Excel, PNG, JPG/JPEG · max {MAX_DOCUMENT_UPLOAD_BYTES / (1024 * 1024)} MB (frontend check).
            </p>
          </div>

          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? "Uploading…" : "Upload"}
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}
