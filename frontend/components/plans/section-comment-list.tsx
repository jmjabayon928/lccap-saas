"use client";

import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import type { SectionCommentSummary } from "@/types/plans";

function formatWhen(iso: string): string {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) {
    return iso;
  }
  return new Intl.DateTimeFormat(undefined, { dateStyle: "medium", timeStyle: "short" }).format(d);
}

function labelType(t: SectionCommentSummary["commentType"]): string {
  if (t === "DataGap") return "Data gap";
  if (t === "RevisionRequest") return "Revision request";
  return t;
}

export interface SectionCommentListProps {
  readonly items: SectionCommentSummary[];
  readonly busy?: boolean;
  readonly onResolve: (commentId: string) => Promise<void>;
  readonly onReopen: (commentId: string) => Promise<void>;
  readonly onArchive: (commentId: string) => Promise<void>;
}

export function SectionCommentList({ items, busy, onResolve, onReopen, onArchive }: SectionCommentListProps) {
  if (items.length === 0) {
    return (
      <div className="rounded-lg border border-dashed border-border px-4 py-6 text-sm text-muted-foreground">
        No review comments for this section yet.
      </div>
    );
  }

  return (
    <ul className="space-y-3">
      {items.map((c) => (
        <li
          key={c.id}
          className={cn(
            "rounded-lg border border-border bg-background px-4 py-3",
            c.isResolved ? "opacity-80" : "shadow-sm"
          )}
        >
          <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
            <div className="min-w-0">
              <div className="flex flex-wrap items-center gap-2">
                <span className={cn("text-xs font-medium", c.isResolved ? "text-muted-foreground" : "text-slate-900")}>
                  {labelType(c.commentType)}
                </span>
                <span className="text-xs text-muted-foreground">•</span>
                <span className="text-xs text-muted-foreground">Created {formatWhen(c.createdAtUtc)}</span>
                {c.isResolved && c.resolvedAtUtc ? (
                  <>
                    <span className="text-xs text-muted-foreground">•</span>
                    <span className="text-xs text-muted-foreground">Resolved {formatWhen(c.resolvedAtUtc)}</span>
                  </>
                ) : null}
              </div>
              <p className={cn("mt-2 whitespace-pre-wrap text-sm", c.isResolved ? "text-muted-foreground" : "text-slate-900")}>
                {c.commentText}
              </p>
            </div>

            <div className="flex flex-wrap gap-2 sm:justify-end">
              {!c.isResolved ? (
                <Button
                  type="button"
                  variant="secondary"
                  size="sm"
                  disabled={busy}
                  onClick={() => void onResolve(c.id)}
                >
                  Resolve
                </Button>
              ) : (
                <Button
                  type="button"
                  variant="secondary"
                  size="sm"
                  disabled={busy}
                  onClick={() => void onReopen(c.id)}
                >
                  Reopen
                </Button>
              )}
              <Button
                type="button"
                variant="outline"
                size="sm"
                disabled={busy}
                onClick={() => void onArchive(c.id)}
              >
                Archive
              </Button>
            </div>
          </div>
        </li>
      ))}
    </ul>
  );
}

