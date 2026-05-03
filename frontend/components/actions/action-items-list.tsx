"use client";

import { Fragment, useState } from "react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Button } from "@/components/ui/button";
import { ActionArchiveButton } from "@/components/actions/action-archive-button";
import { ActionBudget } from "@/components/actions/action-budget";
import { ActionEditForm } from "@/components/actions/action-edit-form";
import { ActionStatusBadge } from "@/components/actions/action-status-badge";
import { ActionTypeBadge } from "@/components/actions/action-type-badge";
import { cn } from "@/lib/utils";
import type { ActionItemSummary, SaveActionItemResult } from "@/types/actions";

interface ActionItemsListProps {
  readonly items: ActionItemSummary[];
  readonly selectedId: string | null;
  readonly onSelect: (actionId: string) => void;
  readonly onActionUpdated: (updated: SaveActionItemResult) => void;
  readonly onActionArchived: (actionItemId: string) => void;
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

export function ActionItemsList({
  items,
  selectedId,
  onSelect,
  onActionUpdated,
  onActionArchived
}: ActionItemsListProps) {
  const [editingId, setEditingId] = useState<string | null>(null);

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
                    <TableHead className="w-44 text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {items.map((item) => {
                    const isSelected = item.id === selectedId;
                    return (
                      <Fragment key={item.id}>
                        <TableRow
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
                          <TableCell className="text-right">
                            <div className="flex flex-wrap justify-end gap-2" onClick={(e) => e.stopPropagation()}>
                              <Button
                                type="button"
                                variant="secondary"
                                size="sm"
                                onClick={() => setEditingId(editingId === item.id ? null : item.id)}
                              >
                                {editingId === item.id ? "Close" : "Edit"}
                              </Button>
                              <ActionArchiveButton
                                actionItemId={item.id}
                                actionLabel={item.title}
                                onArchived={() => {
                                  setEditingId((cur) => (cur === item.id ? null : cur));
                                  onActionArchived(item.id);
                                }}
                              />
                            </div>
                          </TableCell>
                        </TableRow>
                        {editingId === item.id ? (
                          <TableRow>
                            <TableCell colSpan={8} className="bg-slate-50/50 p-3">
                              <ActionEditForm
                                action={item}
                                onSaved={(updated) => {
                                  onActionUpdated(updated);
                                  setEditingId(null);
                                }}
                                onCancel={() => setEditingId(null)}
                              />
                            </TableCell>
                          </TableRow>
                        ) : null}
                      </Fragment>
                    );
                  })}
                </TableBody>
              </Table>
            </div>

            <ul className="flex flex-col gap-3 lg:hidden">
              {items.map((item) => {
                const isSelected = item.id === selectedId;
                return (
                  <li key={item.id} className="rounded-lg border border-border bg-white text-sm shadow-sm">
                    <button
                      type="button"
                      onClick={() => onSelect(item.id)}
                      className={cn(
                        "w-full px-3 py-3 text-left transition-colors",
                        isSelected ? "bg-emerald-50/90" : "hover:bg-slate-50"
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
                    <div className="border-t border-border px-3 py-2">
                      <div className="flex flex-wrap gap-2">
                        <Button
                          type="button"
                          variant="secondary"
                          size="sm"
                          onClick={() => setEditingId(editingId === item.id ? null : item.id)}
                        >
                          {editingId === item.id ? "Close edit" : "Edit"}
                        </Button>
                        <ActionArchiveButton
                          actionItemId={item.id}
                          actionLabel={item.title}
                          onArchived={() => {
                            setEditingId((cur) => (cur === item.id ? null : cur));
                            onActionArchived(item.id);
                          }}
                        />
                      </div>
                      {editingId === item.id ? (
                        <div className="mt-3">
                          <ActionEditForm
                            action={item}
                            onSaved={(updated) => {
                              onActionUpdated(updated);
                              setEditingId(null);
                            }}
                            onCancel={() => setEditingId(null)}
                          />
                        </div>
                      ) : null}
                    </div>
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
