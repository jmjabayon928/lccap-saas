"use client";

import { Fragment, useState } from "react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Button } from "@/components/ui/button";
import { IndicatorArchiveButton } from "@/components/monitoring/indicator-archive-button";
import { IndicatorEditForm } from "@/components/monitoring/indicator-edit-form";
import { IndicatorProgress } from "@/components/monitoring/indicator-progress";
import { IndicatorStatusBadge } from "@/components/monitoring/indicator-status-badge";
import { IndicatorValueSummary } from "@/components/monitoring/indicator-value-summary";
import { cn } from "@/lib/utils";
import type { MonitoringIndicatorSummary } from "@/types/monitoring";

interface IndicatorsListProps {
  readonly indicators: MonitoringIndicatorSummary[];
  readonly selectedId: string | null;
  readonly onSelect: (indicatorId: string) => void;
  readonly onIndicatorUpdated: (updated: MonitoringIndicatorSummary) => void;
  readonly onIndicatorArchived: (indicatorId: string) => void;
}

function formatLastUpdated(row: MonitoringIndicatorSummary): string {
  const raw = row.lastUpdatedAtUtc ?? row.updatedAtUtc ?? row.createdAtUtc;
  if (!raw?.trim()) {
    return "—";
  }
  const d = new Date(raw);
  if (Number.isNaN(d.getTime())) {
    return raw;
  }
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(d);
}

export function IndicatorsList({
  indicators,
  selectedId,
  onSelect,
  onIndicatorUpdated,
  onIndicatorArchived
}: IndicatorsListProps) {
  const [editingId, setEditingId] = useState<string | null>(null);

  return (
    <Card className="border-border shadow-sm">
      <CardHeader>
        <CardTitle>Monitoring indicators</CardTitle>
        <CardDescription>
          LGU implementation tracking for this plan—working preparation that complements existing official systems.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {indicators.length === 0 ? (
          <div className="rounded-lg border border-dashed border-border bg-slate-50/80 px-4 py-10 text-center text-sm text-muted-foreground">
            No indicators yet. Add one using the form beside this list (or below on smaller screens).
          </div>
        ) : (
          <>
            <div className="hidden lg:block">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Name</TableHead>
                    <TableHead className="w-32">Status</TableHead>
                    <TableHead className="min-w-[140px]">Progress</TableHead>
                    <TableHead className="hidden xl:table-cell w-24">Unit</TableHead>
                    <TableHead className="min-w-[220px]">Baseline / current / target</TableHead>
                    <TableHead className="hidden xl:table-cell max-w-[140px]">Office</TableHead>
                    <TableHead className="hidden xl:table-cell">Frequency</TableHead>
                    <TableHead className="w-40 text-right">Last updated</TableHead>
                    <TableHead className="w-44 text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {indicators.map((item) => {
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
                          <TableCell className="max-w-[200px] font-medium text-slate-900 xl:max-w-xs">{item.name}</TableCell>
                          <TableCell>
                            <IndicatorStatusBadge status={item.status} />
                          </TableCell>
                          <TableCell>
                            <IndicatorProgress progressPercent={item.progressPercent} />
                          </TableCell>
                          <TableCell className="hidden text-muted-foreground xl:table-cell">
                            {item.unit?.trim() ? item.unit : "—"}
                          </TableCell>
                          <TableCell>
                            <IndicatorValueSummary indicator={item} compact />
                          </TableCell>
                          <TableCell className="hidden text-muted-foreground xl:table-cell">
                            {item.responsibleOffice?.trim() ? item.responsibleOffice : "—"}
                          </TableCell>
                          <TableCell className="hidden text-muted-foreground xl:table-cell">
                            {item.frequency?.trim() ? item.frequency : "—"}
                          </TableCell>
                          <TableCell className="text-right text-sm text-muted-foreground">
                            {formatLastUpdated(item)}
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
                              <IndicatorArchiveButton
                                indicatorId={item.id}
                                indicatorLabel={item.name}
                                onArchived={() => {
                                  setEditingId((cur) => (cur === item.id ? null : cur));
                                  onIndicatorArchived(item.id);
                                }}
                              />
                            </div>
                          </TableCell>
                        </TableRow>
                        {editingId === item.id ? (
                          <TableRow>
                            <TableCell colSpan={9} className="bg-slate-50/50 p-3">
                              <IndicatorEditForm
                                indicator={item}
                                onSaved={(updated) => {
                                  onIndicatorUpdated(updated);
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
              {indicators.map((item) => {
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
                        <p className="font-medium text-slate-900">{item.name}</p>
                        <IndicatorStatusBadge status={item.status} />
                      </div>
                      <div className="mt-2">
                        <IndicatorProgress progressPercent={item.progressPercent} />
                      </div>
                      <div className="mt-2">
                        <IndicatorValueSummary indicator={item} />
                      </div>
                      <div className="mt-2 flex flex-wrap gap-x-4 gap-y-1 text-xs text-muted-foreground">
                        <span>{item.unit?.trim() ? `Unit: ${item.unit}` : "Unit: —"}</span>
                        <span>{item.frequency?.trim() ? `Frequency: ${item.frequency}` : "Frequency: —"}</span>
                      </div>
                      <div className="mt-2 flex flex-wrap items-center justify-between gap-2 text-xs">
                        <span className="text-muted-foreground">
                          {item.responsibleOffice?.trim() ? item.responsibleOffice : "No office listed"}
                        </span>
                        <span className="text-slate-600">{formatLastUpdated(item)}</span>
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
                        <IndicatorArchiveButton
                          indicatorId={item.id}
                          indicatorLabel={item.name}
                          onArchived={() => {
                            setEditingId((cur) => (cur === item.id ? null : cur));
                            onIndicatorArchived(item.id);
                          }}
                        />
                      </div>
                      {editingId === item.id ? (
                        <div className="mt-3">
                          <IndicatorEditForm
                            indicator={item}
                            onSaved={(updated) => {
                              onIndicatorUpdated(updated);
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
