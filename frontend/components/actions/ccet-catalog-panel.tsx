"use client";

import { RefreshCw } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import type {
  ClimateExpenditureTagCategory,
  ClimateExpenditureTagSummary,
  ClimateExpenditureTagsResult
} from "@/types/actions";

const CATEGORY_ORDER: readonly ClimateExpenditureTagCategory[] = [
  "Adaptation",
  "Mitigation",
  "CrossCutting",
  "DisasterRiskReduction",
  "CapacityDevelopment",
  "Other"
] as const;

function formatCategoryLabel(category: ClimateExpenditureTagCategory): string {
  switch (category) {
    case "CrossCutting":
      return "Cross-cutting";
    case "DisasterRiskReduction":
      return "Disaster risk reduction";
    case "CapacityDevelopment":
      return "Capacity development";
    default:
      return category;
  }
}

export interface CcetCatalogPanelProps {
  readonly status: "idle" | "loading" | "ready" | "error";
  readonly result?: ClimateExpenditureTagsResult;
  readonly errorMessage?: string;
  readonly onRetry?: () => void;
}

export function CcetCatalogPanel({ status, result, errorMessage, onRetry }: CcetCatalogPanelProps) {
  const busy = status === "idle" || status === "loading";
  const failed = status === "error";

  if (busy) {
    return (
      <Card className="border-border shadow-sm">
        <CardHeader className="pb-3">
          <CardTitle className="text-base">Climate expenditure tags (CCET)</CardTitle>
          <CardDescription>Catalog tags your LGU may reference when planning expenditures.</CardDescription>
        </CardHeader>
        <CardContent className="flex items-center gap-3 py-6 text-sm text-muted-foreground">
          <RefreshCw className="h-5 w-5 shrink-0 animate-spin text-emerald-700" aria-hidden />
          Loading CCET catalog…
        </CardContent>
      </Card>
    );
  }

  if (failed) {
    return (
      <Card className="border-amber-200 bg-amber-50/50 shadow-sm">
        <CardHeader className="pb-2">
          <CardTitle className="text-base text-amber-950">CCET catalog could not be loaded</CardTitle>
          <CardDescription className="text-amber-950/85">
            {errorMessage ?? "Something went wrong while loading climate expenditure tags."}
          </CardDescription>
        </CardHeader>
        {onRetry ? (
          <CardContent className="pt-0">
            <Button type="button" variant="outline" size="sm" className="gap-2" onClick={() => void onRetry()}>
              <RefreshCw className="h-4 w-4" aria-hidden />
              Retry CCET catalog
            </Button>
          </CardContent>
        ) : null}
      </Card>
    );
  }

  if (!result) {
    return null;
  }

  const activeTags = result.items.filter((t) => t.isActive);
  const totalActive = activeTags.length;

  const byCategory = new Map<ClimateExpenditureTagCategory, ClimateExpenditureTagSummary[]>();
  for (const tag of activeTags) {
    const list = byCategory.get(tag.tagCategory);
    if (list) {
      list.push(tag);
    } else {
      byCategory.set(tag.tagCategory, [tag]);
    }
  }

  for (const list of byCategory.values()) {
    list.sort((a, b) => a.tagCode.localeCompare(b.tagCode));
  }

  return (
    <Card className="border-border shadow-sm">
      <CardHeader className="pb-3">
        <div className="flex flex-wrap items-start justify-between gap-2">
          <div className="min-w-0">
            <CardTitle className="text-base">Climate expenditure tags (CCET)</CardTitle>
            <CardDescription>Read-only catalog for planning reference ({totalActive} active tag{totalActive === 1 ? "" : "s"}).</CardDescription>
          </div>
          <Badge variant="secondary" className="shrink-0">
            {totalActive} active
          </Badge>
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        {totalActive === 0 ? (
          <p className="rounded-md border border-dashed border-border bg-slate-50/80 px-3 py-8 text-center text-sm text-muted-foreground">
            No active CCET tags are available yet.
          </p>
        ) : (
          <div className="space-y-4">
            {CATEGORY_ORDER.map((category) => {
              const tagsInCategory = byCategory.get(category);
              if (!tagsInCategory?.length) {
                return null;
              }
              return (
                <section key={category} className="space-y-2" aria-labelledby={`ccet-group-${category}`}>
                  <h3 id={`ccet-group-${category}`} className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                    {formatCategoryLabel(category)}
                  </h3>
                  <ul className="space-y-2 text-sm">
                    {tagsInCategory.map((tag) => (
                      <li
                        key={tag.id}
                        className="rounded-md border border-border bg-white px-3 py-2 shadow-sm"
                      >
                        <div className="flex flex-wrap items-baseline gap-x-2 gap-y-1">
                          <span className="font-mono text-xs text-slate-600">{tag.tagCode}</span>
                          <span className="font-medium text-slate-900">{tag.tagName}</span>
                          {tag.weightPercent !== null ? (
                            <Badge variant="outline" className="text-[10px] font-normal">
                              {tag.weightPercent}%
                            </Badge>
                          ) : null}
                        </div>
                        {tag.description?.trim() ? (
                          <p className="mt-1 text-xs text-muted-foreground">{tag.description}</p>
                        ) : null}
                      </li>
                    ))}
                  </ul>
                </section>
              );
            })}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
