"use client";

import Link from "next/link";
import { AlertCircle, Loader2 } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button, buttonVariants } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow
} from "@/components/ui/table";
import { cn } from "@/lib/utils";
import type { PlanSummary } from "@/types/plans";

export interface ExistingPlansListProps {
  readonly plans: PlanSummary[];
  readonly isLoading?: boolean;
  readonly errorMessage?: string | null;
  readonly onRetry?: () => void;
}

function formatStamp(iso: string | null): string | null {
  if (!iso) {
    return null;
  }
  const t = Date.parse(iso);
  if (!Number.isFinite(t)) {
    return null;
  }
  return new Date(t).toLocaleString(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  });
}

export function ExistingPlansList({
  plans,
  isLoading = false,
  errorMessage = null,
  onRetry
}: ExistingPlansListProps) {
  const hasPlans = plans.length > 0;

  return (
    <Card className="border-slate-200/80 shadow-sm">
      <CardHeader className="space-y-1 pb-2">
        <CardTitle className="text-lg">Your workspaces</CardTitle>
        <CardDescription>
          Plans available for your organization. Open a workspace to edit sections, documents, actions, and monitoring.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4 pt-2">
        {isLoading ? (
          <div
            className="flex items-center gap-2 rounded-lg border border-dashed border-slate-200 bg-slate-50/50 px-4 py-6 text-sm text-muted-foreground"
            role="status"
            aria-live="polite"
          >
            <Loader2 className="h-4 w-4 shrink-0 animate-spin" aria-hidden />
            Loading plans…
          </div>
        ) : null}

        {!isLoading && errorMessage ? (
          <div className="flex flex-col gap-3 rounded-lg border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm sm:flex-row sm:items-center sm:justify-between">
            <div className="flex items-start gap-2 text-destructive">
              <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" aria-hidden />
              <span>{errorMessage}</span>
            </div>
            {onRetry ? (
              <Button type="button" variant="outline" size="sm" className="shrink-0" onClick={() => onRetry()}>
                Retry
              </Button>
            ) : null}
          </div>
        ) : null}

        {!isLoading && !errorMessage && !hasPlans ? (
          <p className="rounded-lg border border-dashed border-slate-200 bg-white px-4 py-6 text-center text-sm text-muted-foreground">
            No plans yet. Create your first LCCAP workspace below.
          </p>
        ) : null}

        {!isLoading && !errorMessage && hasPlans ? (
          <>
            <div className="hidden md:block">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="min-w-[11rem]">Plan</TableHead>
                    <TableHead className="whitespace-nowrap">Period</TableHead>
                    <TableHead className="whitespace-nowrap">Status</TableHead>
                    <TableHead className="whitespace-nowrap">Template</TableHead>
                    <TableHead className="min-w-[9rem]">Updated</TableHead>
                    <TableHead className="text-right"> </TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {plans.map((plan) => {
                    const created = formatStamp(plan.createdAtUtc);
                    const updated = formatStamp(plan.updatedAtUtc);
                    const activity = updated ?? created;
                    return (
                      <TableRow key={plan.id}>
                        <TableCell className="font-medium text-foreground">{plan.title}</TableCell>
                        <TableCell className="whitespace-nowrap text-muted-foreground">
                          {plan.startYear}–{plan.endYear}
                        </TableCell>
                        <TableCell>
                          <Badge variant="secondary">{plan.status}</Badge>
                        </TableCell>
                        <TableCell className="text-muted-foreground">
                          {plan.templateMode}
                          <span className="text-muted-foreground/80"> · v{plan.versionNumber}</span>
                        </TableCell>
                        <TableCell className="text-muted-foreground">
                          {activity ?? "—"}
                          {updated && created && updated !== created ? (
                            <span className="mt-0.5 block text-xs text-muted-foreground/80">
                              Created {created}
                            </span>
                          ) : null}
                        </TableCell>
                        <TableCell className="text-right">
                          <Link
                            href={`/plans/${plan.id}`}
                            className={cn(buttonVariants({ variant: "default", size: "sm" }), "inline-flex")}
                          >
                            Open workspace
                          </Link>
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </div>

            <div className="grid gap-3 md:hidden">
              {plans.map((plan) => {
                const created = formatStamp(plan.createdAtUtc);
                const updated = formatStamp(plan.updatedAtUtc);
                const activity = updated ?? created;
                return (
                  <div
                    key={plan.id}
                    className="rounded-lg border border-slate-200 bg-white/80 p-4 shadow-sm"
                  >
                    <div className="flex flex-wrap items-start justify-between gap-2">
                      <div className="space-y-1">
                        <p className="font-semibold text-foreground">{plan.title}</p>
                        <p className="text-xs text-muted-foreground">
                          {plan.startYear}–{plan.endYear}
                        </p>
                      </div>
                      <Badge variant="secondary">{plan.status}</Badge>
                    </div>
                    <p className="mt-2 text-xs text-muted-foreground">
                      {plan.templateMode} · version {plan.versionNumber}
                    </p>
                    {activity ? (
                      <p className="mt-2 text-xs text-muted-foreground">
                        {updated ? "Updated" : "Created"} {activity}
                        {updated && created && updated !== created ? (
                          <span className="mt-1 block">Created {created}</span>
                        ) : null}
                      </p>
                    ) : null}
                    <Link
                      href={`/plans/${plan.id}`}
                      className={cn(
                        buttonVariants({ variant: "default", size: "sm" }),
                        "mt-3 inline-flex w-full justify-center"
                      )}
                    >
                      Open workspace
                    </Link>
                  </div>
                );
              })}
            </div>
          </>
        ) : null}
      </CardContent>
    </Card>
  );
}
