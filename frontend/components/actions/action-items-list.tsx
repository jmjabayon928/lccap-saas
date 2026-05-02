"use client";

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { ActionBudget } from "@/components/actions/action-budget";
import { ActionStatusBadge } from "@/components/actions/action-status-badge";
import { ActionTypeBadge } from "@/components/actions/action-type-badge";
import { cn } from "@/lib/utils";
import type { ActionItemSummary } from "@/types/actions";

interface ActionItemsListProps {
  readonly items: ActionItemSummary[];
  readonly selectedId: string | null;
  readonly onSelect: (actionId: string) => void;
}

function formatTimeline(start: string | null, end: string | null): string {
  const fmt = (raw: string | null): string => {
    if (!raw?.trim()) {
      return "";
    }
    const d = new Date(raw);
    if (Number.isNaN(d.getTime())) {
      return raw;
    }
    return new Intl.DateTimeFormat(undefined, { dateStyle: "medium" }).format(d);
  };
  const a = fmt(start);
  const b = fmt(end);
  if (!a && !b) {
    return "—";
  }
  if (a && b) {
    return `${a} → ${b}`;
  }
  return a || b;
}

export function ActionItemsList({ items, selectedId, onSelect }: ActionItemsListProps) {
  return (
    <Card className="border-border shadow-sm">
      <CardHeader>
        <CardTitle>Action items</CardTitle>
        <CardDescription>
          Adaptation and mitigation actions for this plan—internal preparation for your climate action planning workspace.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {items.length === 0 ? (
          <div className="rounded-lg border border-dashed border-border bg-slate-50/80 px-4 py-10 text-center text-sm text-muted-foreground">
            No action items yet. Add one using the form beside this list (or below on smaller screens).
          </div>
        ) : (
          <>
            <div className="hidden lg:block">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Title</TableHead>
                    <TableHead className="w-28">Type</TableHead>
                    <TableHead>Sector</TableHead>
                    <TableHead className="hidden xl:table-cell">Office</TableHead>
                    <TableHead className="w-32">Status</TableHead>
                    <TableHead className="w-36 text-right">Budget</TableHead>
                    <TableHead className="hidden min-w-[200px] xl:table-cell">Timeline</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {items.map((item) => {
                    const isSelected = item.id === selectedId;
                    return (
                      <TableRow
                        key={item.id}
                        data-state={isSelected ? "selected" : undefined}
                        className={cn(
                          "cursor-pointer transition-colors",
                          isSelected ? "bg-emerald-50/90 hover:bg-emerald-50" : "hover:bg-slate-50"
                        )}
                        onClick={() => onSelect(item.id)}
                        onKeyDown={(e) => {
                          if (e.key === "Enter" || e.key === " ") {
                            e.preventDefault();
                            onSelect(item.id);
                          }
                        }}
                        tabIndex={0}
                        role="button"
                        aria-pressed={isSelected}
                      >
                        <TableCell className="max-w-[220px] font-medium text-slate-900 xl:max-w-xs">{item.title}</TableCell>
                        <TableCell>
                          <ActionTypeBadge actionType={item.actionType} />
                        </TableCell>
                        <TableCell className="text-muted-foreground">{item.sector}</TableCell>
                        <TableCell className="hidden text-muted-foreground xl:table-cell">
                          {item.responsibleOffice?.trim() ? item.responsibleOffice : "—"}
                        </TableCell>
                        <TableCell>
                          <ActionStatusBadge status={item.status} />
                        </TableCell>
                        <TableCell className="text-right">
                          <ActionBudget budgetAmount={item.budgetAmount} />
                        </TableCell>
                        <TableCell className="hidden text-muted-foreground xl:table-cell">
                          {formatTimeline(item.timelineStartUtc, item.timelineEndUtc)}
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </div>

            <ul className="flex flex-col gap-3 lg:hidden">
              {items.map((item) => {
                const isSelected = item.id === selectedId;
                return (
                  <li key={item.id}>
                    <button
                      type="button"
                      onClick={() => onSelect(item.id)}
                      className={cn(
                        "w-full rounded-lg border px-3 py-3 text-left text-sm shadow-sm transition-colors",
                        isSelected ? "border-emerald-300 bg-emerald-50/90" : "border-border bg-white hover:bg-slate-50"
                      )}
                    >
                      <div className="flex flex-wrap items-start justify-between gap-2">
                        <p className="font-medium text-slate-900">{item.title}</p>
                        <ActionTypeBadge actionType={item.actionType} />
                      </div>
                      <p className="mt-1 text-xs text-muted-foreground">{item.sector}</p>
                      <div className="mt-2 flex flex-wrap items-center gap-2">
                        <ActionStatusBadge status={item.status} />
                        <span className="text-xs text-muted-foreground">
                          {item.responsibleOffice?.trim() ? item.responsibleOffice : "No office listed"}
                        </span>
                      </div>
                      <div className="mt-2 flex flex-wrap items-center justify-between gap-2 text-xs">
                        <span className="text-muted-foreground">
                          {formatTimeline(item.timelineStartUtc, item.timelineEndUtc)}
                        </span>
                        <ActionBudget budgetAmount={item.budgetAmount} />
                      </div>
                    </button>
                  </li>
                );
              })}
            </ul>
          </>
        )}
      </CardContent>
    </Card>
  );
}
