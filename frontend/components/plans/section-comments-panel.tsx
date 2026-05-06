"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { isApiError } from "@/lib/api/api-error";
import { planClient } from "@/lib/plans/plan-client";
import { SectionCommentForm } from "@/components/plans/section-comment-form";
import { SectionCommentList } from "@/components/plans/section-comment-list";
import type { CreateSectionCommentRequest, SectionCommentSummary } from "@/types/plans";

type LoadState =
  | { status: "loading" }
  | { status: "ready"; items: SectionCommentSummary[] }
  | { status: "error"; message: string };

function describeError(err: unknown): string {
  if (isApiError(err)) {
    return err.message;
  }
  if (err instanceof Error) {
    return err.message;
  }
  return "Could not load section review comments.";
}

function sortComments(items: SectionCommentSummary[]): SectionCommentSummary[] {
  return [...items].sort((a, b) => {
    if (a.isResolved !== b.isResolved) {
      return a.isResolved ? 1 : -1;
    }
    const ta = Date.parse(a.createdAtUtc) || 0;
    const tb = Date.parse(b.createdAtUtc) || 0;
    return tb - ta;
  });
}

export interface SectionCommentsPanelProps {
  readonly planId: string;
  readonly sectionKey: string;
}

export function SectionCommentsPanel({ planId, sectionKey }: SectionCommentsPanelProps) {
  const [state, setState] = useState<LoadState>({ status: "loading" });
  const [busy, setBusy] = useState(false);

  const heading = useMemo(() => `Review comments`, []);

  const load = useCallback(async () => {
    setState({ status: "loading" });
    try {
      const items = await planClient.getSectionComments(planId, sectionKey);
      setState({ status: "ready", items: sortComments(items) });
    } catch (err) {
      setState({ status: "error", message: describeError(err) });
    }
  }, [planId, sectionKey]);

  useEffect(() => {
    void load();
  }, [load]);

  async function handleCreate(request: CreateSectionCommentRequest): Promise<void> {
    setBusy(true);
    try {
      const created = await planClient.createSectionComment(planId, sectionKey, request);
      setState((s) => {
        if (s.status !== "ready") {
          return { status: "ready", items: sortComments([created]) };
        }
        return { status: "ready", items: sortComments([created, ...s.items]) };
      });
    } finally {
      setBusy(false);
    }
  }

  async function handleResolve(commentId: string): Promise<void> {
    setBusy(true);
    try {
      await planClient.resolveSectionComment(commentId);
      await load();
    } finally {
      setBusy(false);
    }
  }

  async function handleReopen(commentId: string): Promise<void> {
    setBusy(true);
    try {
      await planClient.reopenSectionComment(commentId);
      await load();
    } finally {
      setBusy(false);
    }
  }

  async function handleArchive(commentId: string): Promise<void> {
    setBusy(true);
    try {
      await planClient.archiveSectionComment(commentId);
      await load();
    } finally {
      setBusy(false);
    }
  }

  return (
    <Card className="border-border shadow-sm">
      <CardHeader className="space-y-1">
        <CardTitle className="text-lg">{heading}</CardTitle>
        <CardDescription>
          Leave section-level review notes to coordinate revisions and validate evidence alignment.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {state.status === "error" ? (
          <Alert variant="destructive">
            <AlertTitle>Comments unavailable</AlertTitle>
            <AlertDescription>{state.message}</AlertDescription>
          </Alert>
        ) : null}

        <div className="flex flex-wrap items-center justify-between gap-2">
          <div className="text-xs text-muted-foreground">
            Section key: <span className="font-mono">{sectionKey}</span>
          </div>
          <Button type="button" variant="outline" size="sm" disabled={busy || state.status === "loading"} onClick={() => void load()}>
            Refresh
          </Button>
        </div>

        <SectionCommentForm disabled={busy || state.status === "loading"} onSubmit={handleCreate} />

        {state.status === "loading" ? (
          <div className="text-sm text-muted-foreground">Loading comments…</div>
        ) : state.status === "ready" ? (
          <SectionCommentList
            items={state.items}
            busy={busy}
            onResolve={handleResolve}
            onReopen={handleReopen}
            onArchive={handleArchive}
          />
        ) : null}
      </CardContent>
    </Card>
  );
}

