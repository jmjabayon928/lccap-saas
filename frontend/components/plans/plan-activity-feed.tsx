"use client";

import type { PlanActivityItem } from "@/types/plans";

interface PlanActivityFeedProps {
  readonly items: readonly PlanActivityItem[];
}

function formatWhen(iso: string): string {
  try {
    return new Intl.DateTimeFormat(undefined, { dateStyle: "medium", timeStyle: "short" }).format(
      new Date(iso)
    );
  } catch {
    return iso;
  }
}

export function PlanActivityFeed({ items }: PlanActivityFeedProps) {
  if (items.length === 0) {
    return <p className="text-sm text-muted-foreground">No recent activity yet.</p>;
  }

  return (
    <ul className="divide-y divide-border rounded-md border border-border bg-background">
      {items.map((item) => (
        <li key={item.id} className="px-3 py-2.5">
          <p className="text-sm font-medium text-slate-900">{item.summary}</p>
          <p className="mt-0.5 text-xs text-muted-foreground">
            <span className="font-medium text-slate-700">{item.entityType}</span>
            <span aria-hidden> · </span>
            <span>{formatWhen(item.createdAtUtc)}</span>
          </p>
        </li>
      ))}
    </ul>
  );
}
