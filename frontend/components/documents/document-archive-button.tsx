"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { isApiError } from "@/lib/api/api-error";
import { documentClient } from "@/lib/documents/document-client";

function formatError(err: unknown): string {
  if (isApiError(err)) {
    return err.message;
  }
  if (err instanceof Error) {
    return err.message;
  }
  return "Could not archive document.";
}

export interface DocumentArchiveButtonProps {
  readonly documentId: string;
  /** Short label for button context */
  readonly documentLabel: string;
  readonly onArchived: () => void;
}

export function DocumentArchiveButton({ documentId, documentLabel, onArchived }: DocumentArchiveButtonProps) {
  const [phase, setPhase] = useState<"idle" | "confirm" | "loading" | "error">("idle");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  function resetIdle(): void {
    setPhase("idle");
    setErrorMessage(null);
  }

  async function confirmArchive(): Promise<void> {
    setPhase("loading");
    setErrorMessage(null);
    try {
      await documentClient.archiveDocument(documentId);
      onArchived();
      resetIdle();
    } catch (err) {
      setErrorMessage(formatError(err));
      setPhase("error");
    }
  }

  if (phase === "confirm" || phase === "loading" || phase === "error") {
    return (
      <div className="rounded-md border border-amber-200 bg-amber-50/80 px-3 py-2 text-xs text-amber-950">
        <p className="font-medium">
          Archive this document? It will be removed from the active workspace list, but the audit trail will keep the
          action.
        </p>
        {errorMessage ? (
          <Alert variant="destructive" className="mt-2">
            <AlertTitle>Archive failed</AlertTitle>
            <AlertDescription>{errorMessage}</AlertDescription>
          </Alert>
        ) : null}
        <div className="mt-2 flex flex-wrap gap-2">
          <Button type="button" size="sm" variant="outline" disabled={phase === "loading"} onClick={resetIdle}>
            Cancel
          </Button>
          <Button type="button" size="sm" variant="danger" disabled={phase === "loading"} onClick={() => void confirmArchive()}>
            {phase === "loading" ? "Archiving…" : "Archive document"}
          </Button>
        </div>
      </div>
    );
  }

  return (
    <Button
      type="button"
      variant="outline"
      size="sm"
      className="text-amber-900 border-amber-200"
      onClick={() => setPhase("confirm")}
      aria-label={`Archive document ${documentLabel}`}
    >
      Archive
    </Button>
  );
}
