"use client";

import { useMemo, useState } from "react";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import type { CreateSectionCommentRequest, SectionCommentType } from "@/types/plans";

const COMMENT_TYPES: readonly SectionCommentType[] = [
  "General",
  "DataGap",
  "Validation",
  "RevisionRequest"
] as const;

export interface SectionCommentFormProps {
  readonly disabled?: boolean;
  readonly onSubmit: (request: CreateSectionCommentRequest) => Promise<void>;
}

export function SectionCommentForm({ disabled, onSubmit }: SectionCommentFormProps) {
  const [commentType, setCommentType] = useState<SectionCommentType>("General");
  const [commentText, setCommentText] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const trimmedText = useMemo(() => commentText.trim(), [commentText]);

  async function handleSubmit(): Promise<void> {
    if (!trimmedText) {
      return;
    }

    setIsSubmitting(true);
    try {
      await onSubmit({ commentType, commentText: trimmedText });
      setCommentText("");
      setCommentType("General");
    } finally {
      setIsSubmitting(false);
    }
  }

  const isDisabled = Boolean(disabled) || isSubmitting;

  return (
    <form
      className="space-y-3"
      onSubmit={(ev) => {
        ev.preventDefault();
        void handleSubmit();
      }}
    >
      <div className="grid gap-3 sm:grid-cols-2">
        <div className="space-y-2">
          <Label htmlFor="section-comment-type">Type</Label>
          <select
            id="section-comment-type"
            className="h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
            value={commentType}
            onChange={(ev) => setCommentType(ev.target.value as SectionCommentType)}
            disabled={isDisabled}
          >
            {COMMENT_TYPES.map((t) => (
              <option key={t} value={t}>
                {t === "DataGap" ? "Data gap" : t === "RevisionRequest" ? "Revision request" : t}
              </option>
            ))}
          </select>
        </div>
        <div className="space-y-2 sm:pt-7">
          <Button type="submit" disabled={isDisabled || !trimmedText} className="w-full sm:w-auto">
            {isSubmitting ? "Adding…" : "Add comment"}
          </Button>
        </div>
      </div>

      <div className="space-y-2">
        <Label htmlFor="section-comment-text">Comment</Label>
        <Textarea
          id="section-comment-text"
          value={commentText}
          onChange={(ev) => setCommentText(ev.target.value)}
          disabled={isDisabled}
          rows={4}
          placeholder="Write a review note tied to this section."
        />
        <p className="text-xs text-muted-foreground">
          Keep comments specific and actionable (e.g., data gaps, validation notes, revision requests).
        </p>
      </div>
    </form>
  );
}

