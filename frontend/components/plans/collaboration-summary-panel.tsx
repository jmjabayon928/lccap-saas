"use client";

import { useCallback, useEffect, useState } from "react";
import { RefreshCw } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { isApiError } from "@/lib/api/api-error";
import { planClient } from "@/lib/plans/plan-client";
import type { CollaborationSummaryResult, CollaborationGroupSummary } from "@/types/plans";

type CollaborationSummaryPanelState =
  | { status: "loading" }
  | { status: "ready"; result: CollaborationSummaryResult }
  | { status: "error"; message: string; retryable: boolean };

function formatWhen(iso: string): string {
  try {
    return new Intl.DateTimeFormat(undefined, { dateStyle: "medium", timeStyle: "short" }).format(new Date(iso));
  } catch {
    return iso;
  }
}

function GroupCard({ group }: { group: CollaborationGroupSummary }) {
  return (
    <div className="rounded-lg border border-border bg-background p-3">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="space-y-1">
          <h3 className="text-sm font-semibold text-slate-900">{group.name}</h3>
          <p className="text-xs text-muted-foreground">
            {group.memberCount} member{group.memberCount === 1 ? "" : "s"} · Created {formatWhen(group.createdAtUtc)}
          </p>
        </div>
        {group.memberCount > 0 ? <Badge variant="secondary">{group.memberCount} active</Badge> : null}
      </div>
      {group.members.length === 0 ? null : (
        <ul className="mt-3 space-y-2">
          {group.members.map((m) => (
            <li key={m.userId} className="flex items-start justify-between gap-3">
              <div className="min-w-0">
                <p className="text-sm font-medium text-slate-900 truncate">{m.fullName}</p>
                <p className="text-xs text-muted-foreground truncate">{m.email}</p>
              </div>
              <Badge variant="outline" className="shrink-0">
                {m.role}
              </Badge>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

export function CollaborationSummaryPanel() {
  const [state, setState] = useState<CollaborationSummaryPanelState>({ status: "loading" });

  const load = useCallback(async () => {
    setState({ status: "loading" });
    try {
      const result = await planClient.getCollaborationSummary();
      setState({ status: "ready", result });
    } catch (err) {
      const message = isApiError(err) ? err.message : err instanceof Error ? err.message : "Could not load collaboration.";
      setState({ status: "error", message, retryable: true });
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  if (state.status === "loading" || state.status === "error") {
    return (
      <Card className={state.status === "error" ? "border-amber-200 bg-amber-50/50" : undefined}>
        <CardHeader className="pb-2">
          <CardTitle className="text-base">Collaboration</CardTitle>
          <CardDescription>Tenant group member awareness.</CardDescription>
        </CardHeader>
        <CardContent>
          {state.status === "loading" ? (
            <div className="flex items-center gap-3 py-6 text-sm text-muted-foreground">
              <RefreshCw className="h-5 w-5 shrink-0 animate-spin text-emerald-700" aria-hidden />
              Loading collaboration groups…
            </div>
          ) : (
            <div className="flex flex-wrap items-center gap-2">
              {state.retryable ? (
                <Button type="button" variant="outline" size="sm" className="gap-2" onClick={() => void load()}>
                  <RefreshCw className="h-4 w-4" aria-hidden />
                  Retry
                </Button>
              ) : null}
            </div>
          )}
        </CardContent>
      </Card>
    );
  }

  const result = state.result;

  return (
    <Card>
      <CardHeader className="flex flex-row flex-wrap items-start justify-between gap-3 pb-2">
        <div className="space-y-1">
          <CardTitle className="text-base flex items-center gap-2">
            Collaboration
            {result.totalGroups > 0 ? <Badge variant="secondary">{result.totalGroups} groups</Badge> : null}
          </CardTitle>
          <CardDescription>{result.totalMembers} active member{result.totalMembers === 1 ? "" : "s"} across groups.</CardDescription>
        </div>
      </CardHeader>

      <CardContent className="space-y-4">
        {result.groups.length === 0 ? (
          <p className="text-sm text-muted-foreground">No collaboration groups have been configured yet.</p>
        ) : (
          <div className="space-y-3">
            {result.groups.map((g) => (
              <GroupCard key={g.id} group={g} />
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  );
}

