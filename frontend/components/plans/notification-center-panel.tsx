"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { RefreshCw } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { isApiError } from "@/lib/api/api-error";
import { planClient } from "@/lib/plans/plan-client";
import type { MyNotificationsResult } from "@/types/plans";

type NotificationCenterPanelState =
  | { status: "loading" }
  | { status: "ready"; result: MyNotificationsResult }
  | { status: "error"; message: string; retryable: boolean };

function formatWhen(iso: string): string {
  try {
    return new Intl.DateTimeFormat(undefined, { dateStyle: "medium", timeStyle: "short" }).format(new Date(iso));
  } catch {
    return iso;
  }
}

export function NotificationCenterPanel() {
  const [unreadOnly, setUnreadOnly] = useState(false);
  const [state, setState] = useState<NotificationCenterPanelState>({ status: "loading" });

  const load = useCallback(async () => {
    setState({ status: "loading" });
    try {
      const result = await planClient.getMyNotifications({ limit: 25, unreadOnly });
      setState({ status: "ready", result });
    } catch (err) {
      const message = isApiError(err) ? err.message : err instanceof Error ? err.message : "Could not load notifications.";
      setState({ status: "error", message, retryable: true });
    }
  }, [unreadOnly]);

  useEffect(() => {
    void load();
  }, [load]);

  const unreadCount = useMemo(() => (state.status === "ready" ? state.result.unreadCount : 0), [state]);

  async function onMarkRead(notificationId: string): Promise<void> {
    try {
      await planClient.markNotificationRead(notificationId);
      await load();
    } catch (err) {
      const message = isApiError(err) ? err.message : err instanceof Error ? err.message : "Could not mark notification as read.";
      setState({ status: "error", message, retryable: true });
    }
  }

  async function onMarkAllRead(): Promise<void> {
    try {
      await planClient.markAllNotificationsRead();
      await load();
    } catch (err) {
      const message = isApiError(err) ? err.message : err instanceof Error ? err.message : "Could not mark notifications as read.";
      setState({ status: "error", message, retryable: true });
    }
  }

  if (state.status === "loading" || state.status === "error") {
    return (
      <Card className={state.status === "error" ? "border-amber-200 bg-amber-50/50" : undefined}>
        <CardHeader className="pb-2">
          <CardTitle className="text-base flex items-center justify-between gap-3">
            <span>Notifications</span>
            {unreadCount > 0 ? <Badge variant="secondary">{unreadCount} unread</Badge> : <span />}
          </CardTitle>
          <CardDescription>In-app updates for your tenant.</CardDescription>
        </CardHeader>
        <CardContent>
          {state.status === "loading" ? (
            <div className="flex items-center gap-3 py-6 text-sm text-muted-foreground">
              <RefreshCw className="h-5 w-5 shrink-0 animate-spin text-emerald-700" aria-hidden />
              Loading notifications…
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
            Notifications
            {result.unreadCount > 0 ? <Badge variant="secondary">{result.unreadCount} unread</Badge> : null}
          </CardTitle>
          <CardDescription>In-app updates for your tenant.</CardDescription>
        </div>

        <div className="flex flex-wrap gap-2">
          <Button
            type="button"
            variant={unreadOnly ? "secondary" : "outline"}
            size="sm"
            onClick={() => setUnreadOnly((v) => !v)}
          >
            {unreadOnly ? "Showing unread" : "Show unread only"}
          </Button>
          <Button type="button" variant="outline" size="sm" onClick={() => void onMarkAllRead()}>
            Mark all read
          </Button>
        </div>
      </CardHeader>

      <CardContent className="space-y-4">
        {result.items.length === 0 ? (
          <p className="text-sm text-muted-foreground">No notifications yet.</p>
        ) : (
          <ul className="divide-y divide-border rounded-md border border-border bg-background">
            {result.items.map((n) => (
              <li key={n.id} className="px-3 py-2.5">
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div className="min-w-0">
                    <p className="text-sm font-medium text-slate-900 truncate">{n.title}</p>
                    <p className="mt-1 text-xs text-muted-foreground truncate">{n.message}</p>
                    <p className="mt-1 text-xs text-muted-foreground">
                      <span className="font-medium text-slate-700">{n.eventType}</span>
                      <span aria-hidden> · </span>
                      <span>{formatWhen(n.createdAtUtc)}</span>
                    </p>
                  </div>

                  <div className="flex flex-col items-end gap-2">
                    <Badge variant={n.isRead ? "secondary" : "default"}>{n.isRead ? "Read" : "Unread"}</Badge>
                    {!n.isRead ? (
                      <Button type="button" variant="secondary" size="sm" onClick={() => void onMarkRead(n.id)}>
                        Mark read
                      </Button>
                    ) : null}
                  </div>
                </div>
              </li>
            ))}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}

