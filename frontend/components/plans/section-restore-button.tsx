"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { planClient } from "@/lib/plans/plan-client";
import { isApiError } from "@/lib/api/api-error";
import type { PlanSectionHistoryEntry, SavePlanSectionResult } from "@/types/plans";

export interface SectionRestoreButtonProps {
  readonly planId: string;
  readonly sectionKey: string;
  readonly entry: PlanSectionHistoryEntry;
  readonly onRestored: (result: SavePlanSectionResult, restoredTitle: string, restoredContent: string) => void;
}

export function SectionRestoreButton({ planId, sectionKey, entry, onRestored }: SectionRestoreButtonProps) {
  const [isConfirming, setIsConfirming] = useState(false);
  const [restoreReason, setRestoreReason] = useState("");
  const [isRestoring, setIsRestoring] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  async function handleRestore() {
    setIsRestoring(true);
    setErrorMessage(null);
    try {
      const result = await planClient.restorePlanSection(planId, sectionKey, {
        auditLogId: entry.auditLogId,
        restoreReason: restoreReason.trim() || undefined,
      });
      onRestored(result, entry.title, entry.content);
      setIsConfirming(false);
    } catch (err) {
      if (isApiError(err)) {
        setErrorMessage(err.message);
      } else if (err instanceof Error) {
        setErrorMessage(err.message);
      } else {
        setErrorMessage("Restore failed. Please try again.");
      }
    } finally {
      setIsRestoring(false);
    }
  }

  if (!isConfirming) {
    return (
      <Button variant="outline" size="sm" onClick={() => setIsConfirming(true)}>
        Restore this version
      </Button>
    );
  }

  return (
    <div className="mt-2 space-y-3 rounded-md border border-amber-200 bg-amber-50 p-3 text-sm">
      <p className="font-medium text-amber-900">Restore this version?</p>
      <p className="text-amber-800/90">
        The current content will be kept in the audit trail before the restore is applied.
      </p>

      {errorMessage ? (
        <Alert variant="destructive" className="bg-white">
          <AlertTitle>Restore failed</AlertTitle>
          <AlertDescription>{errorMessage}</AlertDescription>
        </Alert>
      ) : null}

      <div className="space-y-2">
        <Label htmlFor={`restore-reason-${entry.auditLogId}`} className="text-amber-900">
          Reason for restore (optional)
        </Label>
        <Textarea
          id={`restore-reason-${entry.auditLogId}`}
          placeholder="e.g., Accidental deletion, reverting to previous draft..."
          value={restoreReason}
          onChange={(e) => setRestoreReason(e.target.value)}
          disabled={isRestoring}
          className="bg-white"
          rows={2}
        />
      </div>

      <div className="flex items-center gap-2">
        <Button
          variant="ghost"
          size="sm"
          onClick={() => setIsConfirming(false)}
          disabled={isRestoring}
          className="text-amber-900 hover:bg-amber-100 hover:text-amber-950"
        >
          Cancel
        </Button>
        <Button
          variant="default"
          size="sm"
          onClick={handleRestore}
          disabled={isRestoring}
          className="bg-amber-600 text-white hover:bg-amber-700"
        >
          {isRestoring ? "Restoring..." : "Confirm restore"}
        </Button>
      </div>
    </div>
  );
}
