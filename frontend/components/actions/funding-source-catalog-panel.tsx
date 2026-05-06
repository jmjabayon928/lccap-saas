"use client";

import { RefreshCw } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import type {
  FundingSourcesResult,
  FundingSourceSummary,
  FundingSourceType
} from "@/types/actions";

const SOURCE_TYPE_ORDER: readonly FundingSourceType[] = [
  "LGUInternal",
  "NationalGovernment",
  "ProvincialGovernment",
  "Donor",
  "NGO",
  "PrivateSector",
  "BankLoan",
  "ClimateFund",
  "Other"
] as const;

function formatSourceTypeLabel(t: FundingSourceType): string {
  switch (t) {
    case "LGUInternal":
      return "LGU internal";
    case "NationalGovernment":
      return "National government";
    case "ProvincialGovernment":
      return "Provincial government";
    case "BankLoan":
      return "Bank loan";
    case "ClimateFund":
      return "Climate fund";
    case "PrivateSector":
      return "Private sector";
    default:
      return t;
  }
}

export interface FundingSourceCatalogPanelProps {
  readonly status: "idle" | "loading" | "ready" | "error";
  readonly result?: FundingSourcesResult;
  readonly errorMessage?: string;
  readonly onRetry?: () => void;
}

export function FundingSourceCatalogPanel({ status, result, errorMessage, onRetry }: FundingSourceCatalogPanelProps) {
  const busy = status === "idle" || status === "loading";
  const failed = status === "error";

  if (busy) {
    return (
      <Card className="border-border shadow-sm">
        <CardHeader className="pb-3">
          <CardTitle className="text-base">Funding sources</CardTitle>
          <CardDescription>Read-only tenant catalog.</CardDescription>
        </CardHeader>
        <CardContent className="flex items-center gap-3 py-6 text-sm text-muted-foreground">
          <RefreshCw className="h-5 w-5 shrink-0 animate-spin text-emerald-700" aria-hidden />
          Loading funding sources…
        </CardContent>
      </Card>
    );
  }

  if (failed) {
    return (
      <Card className="border-amber-200 bg-amber-50/50 shadow-sm">
        <CardHeader className="pb-2">
          <CardTitle className="text-base text-amber-950">Funding sources could not be loaded</CardTitle>
          <CardDescription className="text-amber-950/85">
            {errorMessage ?? "Something went wrong while loading funding sources."}
          </CardDescription>
        </CardHeader>
        {onRetry ? (
          <CardContent className="pt-0">
            <Button type="button" variant="outline" size="sm" className="gap-2" onClick={() => void onRetry()}>
              <RefreshCw className="h-4 w-4" aria-hidden />
              Retry funding sources
            </Button>
          </CardContent>
        ) : null}
      </Card>
    );
  }

  if (!result) {
    return null;
  }

  const byType = new Map<FundingSourceType, FundingSourceSummary[]>();
  for (const s of result.items) {
    const list = byType.get(s.sourceType);
    if (list) {
      list.push(s);
    } else {
      byType.set(s.sourceType, [s]);
    }
  }

  for (const list of byType.values()) {
    list.sort((a, b) => a.name.localeCompare(b.name));
  }

  return (
    <Card className="border-border shadow-sm">
      <CardHeader className="pb-3">
        <div className="flex flex-wrap items-start justify-between gap-2">
          <div className="min-w-0">
            <CardTitle className="text-base">Funding sources</CardTitle>
            <CardDescription>
              Read-only catalog ({result.totalCount} source{result.totalCount === 1 ? "" : "s"}).
            </CardDescription>
          </div>
          <Badge variant="secondary" className="shrink-0">
            {result.totalCount}
          </Badge>
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        {result.items.length === 0 ? (
          <p className="rounded-md border border-dashed border-border bg-slate-50/80 px-3 py-8 text-center text-sm text-muted-foreground">
            No funding sources are available yet.
          </p>
        ) : (
          <div className="space-y-4">
            {SOURCE_TYPE_ORDER.map((typeKey) => {
              const rows = byType.get(typeKey);
              if (!rows?.length) {
                return null;
              }
              return (
                <section key={typeKey} className="space-y-2" aria-labelledby={`fs-group-${typeKey}`}>
                  <h3 id={`fs-group-${typeKey}`} className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                    {formatSourceTypeLabel(typeKey)}
                  </h3>
                  <ul className="space-y-2 text-sm">
                    {rows.map((row) => (
                      <li key={row.id} className="rounded-md border border-border bg-white px-3 py-2 shadow-sm">
                        <div className="font-medium text-slate-900">{row.name}</div>
                        {(row.contactName ?? row.contactEmail ?? row.websiteUrl) ? (
                          <ul className="mt-1 space-y-0.5 text-xs text-muted-foreground">
                            {row.contactName?.trim() ? (
                              <li>
                                Contact:{" "}
                                <span className="text-slate-700">{row.contactName.trim()}</span>
                              </li>
                            ) : null}
                            {row.contactEmail?.trim() ? (
                              <li>
                                Email:{" "}
                                <a className="text-emerald-800 underline underline-offset-2" href={`mailto:${row.contactEmail.trim()}`}>
                                  {row.contactEmail.trim()}
                                </a>
                              </li>
                            ) : null}
                            {row.websiteUrl?.trim() ? (
                              <li>
                                Web:{" "}
                                <a
                                  className="text-emerald-800 underline underline-offset-2"
                                  href={row.websiteUrl.trim()}
                                  rel="noopener noreferrer"
                                  target="_blank"
                                >
                                  {row.websiteUrl.trim()}
                                </a>
                              </li>
                            ) : null}
                          </ul>
                        ) : null}
                        {row.description?.trim() ? (
                          <p className="mt-2 text-[11px] leading-snug text-muted-foreground">{row.description.trim()}</p>
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
