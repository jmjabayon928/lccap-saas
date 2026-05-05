"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { isApiError } from "@/lib/api/api-error";
import { planClient } from "@/lib/plans/plan-client";

interface PlanArchiveButtonProps {
  planId: string;
  planTitle: string;
  onSuccess: () => void;
}

const ARCHIVE_CONFIRMATION_TEXT = "ARCHIVE";

export function PlanArchiveButton({ planId, planTitle, onSuccess }: PlanArchiveButtonProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [confirmText, setConfirmText] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  async function handleArchive() {
    if (confirmText !== ARCHIVE_CONFIRMATION_TEXT) return;

    setIsSubmitting(true);
    setErrorMessage(null);
    try {
      await planClient.archivePlan(planId);
      onSuccess();
    } catch (err) {
      if (isApiError(err)) {
        setErrorMessage(err.message);
      } else {
        setErrorMessage("Could not archive plan. Please try again.");
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  if (!isOpen) {
    return (
      <Button
        variant="outline"
        className="text-destructive hover:text-destructive"
        onClick={() => setIsOpen(true)}
      >
        Archive plan
      </Button>
    );
  }

  return (
    <div className="p-4 border border-destructive/50 rounded-md bg-destructive/5 space-y-4">
      <div className="space-y-1">
        <h4 className="text-sm font-semibold text-destructive">Archive Plan</h4>
        <p className="text-xs text-muted-foreground">
          Archive <strong>{planTitle}</strong>? It will be removed from the active plans list, but its related records and audit trail will be preserved.
        </p>
      </div>

      {errorMessage ? (
        <Alert variant="destructive">
          <AlertTitle>Archive failed</AlertTitle>
          <AlertDescription>{errorMessage}</AlertDescription>
        </Alert>
      ) : null}

      <div className="space-y-2">
        <Label htmlFor="archive-confirm" className="text-xs">
          Type <strong>{ARCHIVE_CONFIRMATION_TEXT}</strong> to confirm
        </Label>
        <Input
          id="archive-confirm"
          value={confirmText}
          onChange={(e) => setConfirmText(e.target.value)}
          placeholder={ARCHIVE_CONFIRMATION_TEXT}
          className="h-8 text-sm"
          disabled={isSubmitting}
        />
      </div>

      <div className="flex gap-2">
        <Button
          variant="outline"
          size="sm"
          onClick={() => {
            setIsOpen(false);
            setConfirmText("");
            setErrorMessage(null);
          }}
          disabled={isSubmitting}
        >
          Cancel
        </Button>
        <Button
          variant="danger"
          size="sm"
          onClick={handleArchive}
          disabled={isSubmitting || confirmText !== ARCHIVE_CONFIRMATION_TEXT}
        >
          {isSubmitting ? "Archiving..." : "Archive plan"}
        </Button>
      </div>
    </div>
  );
}
