"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { Download, Loader2, RefreshCw } from "lucide-react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { isApiError } from "@/lib/api/api-error";
import { documentClient } from "@/lib/documents/document-client";
import { DOCUMENT_CATEGORIES } from "@/types/documents";
import type { EvidenceIndexResult, EvidenceStatus } from "@/types/documents";

interface EvidenceIndexPanelProps {
  readonly planId: string;
}

type LoadState =
  | { status: "loading" }
  | { status: "ready"; result: EvidenceIndexResult }
  | { status: "empty" }
  | { status: "error"; message: string };

const EVIDENCE_STATUSES: readonly EvidenceStatus[] = ["Draft", "Internal", "Official", "Public"];

function describeError(err: unknown): string {
  if (isApiError(err)) {
    return err.message;
  }
  if (err instanceof Error) {
    return err.message;
  }
  return "Something went wrong while loading the evidence index.";
}

function formatDate(value: string | null): string {
  if (!value) {
    return "—";
  }
  return value;
}

export function EvidenceIndexPanel({ planId }: EvidenceIndexPanelProps) {
  const [state, setState] = useState<LoadState>({ status: "loading" });
  const [downloading, setDownloading] = useState(false);

  const load = useCallback(async () => {
    setState({ status: "loading" });
    try {
      const result = await documentClient.getEvidenceIndex(planId);
      if (result.totalCount === 0) {
        setState({ status: "empty" });
        return;
      }
      setState({ status: "ready", result });
    } catch (err) {
      setState({ status: "error", message: describeError(err) });
    }
  }, [planId]);

  useEffect(() => {
    void load();
  }, [load]);

  const summary = useMemo(() => {
    if (state.status !== "ready") {
      return null;
    }
    const r = state.result;
    return {
      total: r.totalCount,
      byStatus: EVIDENCE_STATUSES.map((s) => ({ status: s, count: r.countsByEvidenceStatus[s] ?? 0 })),
      byCategory: DOCUMENT_CATEGORIES.map((c) => ({ category: c, count: r.countsByCategory[c] ?? 0 }))
    };
  }, [state]);

  const handleDownload = useCallback(async () => {
    setDownloading(true);
    try {
      const { blob, fileName } = await documentClient.downloadEvidenceIndexCsv(planId);
      const url = URL.createObjectURL(blob);
      try {
        const anchor = document.createElement("a");
        anchor.href = url;
        anchor.download = fileName;
        anchor.rel = "noopener";
        document.body.appendChild(anchor);
        anchor.click();
        anchor.remove();
      } finally {
        window.setTimeout(() => URL.revokeObjectURL(url), 1500);
      }
    } finally {
      setDownloading(false);
    }
  }, [planId]);

  return (
    <Card className="border-border shadow-sm">
      <CardHeader>
        <CardTitle className="text-lg">Evidence index</CardTitle>
        <CardDescription>
          Review a structured list of supporting documents and export a CSV for operational readiness checks.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {state.status === "loading" ? (
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" aria-hidden />
            Loading evidence index…
          </div>
        ) : null}

        {state.status === "error" ? (
          <Alert variant="destructive">
            <AlertTitle>Evidence index unavailable</AlertTitle>
            <AlertDescription className="space-y-3">
              <p>{state.message}</p>
              <Button type="button" variant="outline" size="sm" className="gap-2" onClick={() => void load()}>
                <RefreshCw className="h-4 w-4" aria-hidden />
                Retry
              </Button>
            </AlertDescription>
          </Alert>
        ) : null}

        {state.status === "empty" ? (
          <div className="rounded-lg border border-dashed border-border bg-white px-4 py-6 text-sm text-muted-foreground">
            No evidence documents have been added yet.
          </div>
        ) : null}

        {state.status === "ready" && summary ? (
          <>
            <div className="flex flex-wrap items-center justify-between gap-2">
              <div className="text-sm text-slate-900">
                <span className="font-medium">Total evidence:</span> {summary.total}
              </div>
              <Button type="button" variant="secondary" className="gap-2" onClick={() => void handleDownload()} disabled={downloading}>
                {downloading ? <Loader2 className="h-4 w-4 animate-spin" aria-hidden /> : <Download className="h-4 w-4" aria-hidden />}
                Download CSV
              </Button>
            </div>

            <div className="grid gap-3 md:grid-cols-2">
              <div className="rounded-lg border border-border bg-white p-3">
                <p className="text-xs font-medium uppercase tracking-wide text-slate-500">By evidence status</p>
                <dl className="mt-2 grid grid-cols-2 gap-2 text-sm">
                  {summary.byStatus.map((row) => (
                    <div key={row.status} className="flex items-center justify-between gap-2">
                      <dt className="text-slate-700">{row.status}</dt>
                      <dd className="font-medium text-slate-900">{row.count}</dd>
                    </div>
                  ))}
                </dl>
              </div>
              <div className="rounded-lg border border-border bg-white p-3">
                <p className="text-xs font-medium uppercase tracking-wide text-slate-500">By category</p>
                <dl className="mt-2 grid grid-cols-2 gap-2 text-sm">
                  {summary.byCategory.map((row) => (
                    <div key={row.category} className="flex items-center justify-between gap-2">
                      <dt className="text-slate-700">{row.category}</dt>
                      <dd className="font-medium text-slate-900">{row.count}</dd>
                    </div>
                  ))}
                </dl>
              </div>
            </div>

            <div className="overflow-x-auto rounded-lg border border-border bg-white">
              <table className="min-w-full text-sm">
                <thead className="bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-500">
                  <tr>
                    <th className="px-3 py-2">Title</th>
                    <th className="px-3 py-2">Category</th>
                    <th className="px-3 py-2">Status</th>
                    <th className="px-3 py-2">Source</th>
                    <th className="px-3 py-2">Date</th>
                    <th className="px-3 py-2">Linked section</th>
                    <th className="px-3 py-2">Linked action</th>
                    <th className="px-3 py-2">File</th>
                    <th className="px-3 py-2">Tags</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {state.result.items.map((item) => {
                    const sectionLabel =
                      item.planSectionKey || item.planSectionTitle
                        ? `${item.planSectionKey ?? "—"} · ${item.planSectionTitle ?? "—"}`
                        : "—";
                    const actionLabel = item.actionTitle ? item.actionTitle : "—";
                    const tagsLabel = item.tags.length ? item.tags.join(", ") : "—";

                    return (
                      <tr key={item.documentId} className="align-top">
                        <td className="px-3 py-2 font-medium text-slate-900">{item.title ?? "Untitled"}</td>
                        <td className="px-3 py-2 text-slate-700">{item.category}</td>
                        <td className="px-3 py-2 text-slate-700">{item.evidenceStatus}</td>
                        <td className="px-3 py-2 text-slate-700">{item.sourceAgency ?? "—"}</td>
                        <td className="px-3 py-2 text-slate-700">{formatDate(item.documentDate)}</td>
                        <td className="px-3 py-2 text-slate-700">{sectionLabel}</td>
                        <td className="px-3 py-2 text-slate-700">{actionLabel}</td>
                        <td className="px-3 py-2 text-slate-700">{item.originalFileName ?? "—"}</td>
                        <td className="px-3 py-2 text-slate-700">{tagsLabel}</td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          </>
        ) : null}
      </CardContent>
    </Card>
  );
}

