"use client";

import { useCallback, useEffect, useState } from "react";
import { Download, Loader2, Package, RefreshCw } from "lucide-react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { isApiError } from "@/lib/api/api-error";
import { actionClient } from "@/lib/actions/action-client";
import { documentClient } from "@/lib/documents/document-client";
import type { ExportPackageManifest } from "@/types/actions";

interface ExportPackagePanelProps {
  readonly planId: string;
}

type PanelState =
  | { readonly status: "loading" }
  | { readonly status: "ready"; readonly manifest: ExportPackageManifest }
  | { readonly status: "error"; readonly message: string };

function describeError(err: unknown): string {
  if (isApiError(err)) {
    return err.message;
  }
  if (err instanceof Error) {
    return err.message;
  }
  return "Could not load the export package manifest.";
}

function triggerBlobDownload(blob: Blob, fileName: string): void {
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
}

function formatGeneratedAt(iso: string): string {
  const t = Date.parse(iso);
  if (!Number.isFinite(t)) {
    return iso;
  }
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(t);
}

export function ExportPackagePanel({ planId }: ExportPackagePanelProps) {
  const [state, setState] = useState<PanelState>({ status: "loading" });
  const [downloadingKey, setDownloadingKey] = useState<string | null>(null);
  const [downloadError, setDownloadError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setState({ status: "loading" });
    try {
      const manifest = await actionClient.getExportPackageManifest(planId);
      setState({ status: "ready", manifest });
    } catch (err) {
      setState({ status: "error", message: describeError(err) });
    }
  }, [planId]);

  useEffect(() => {
    void load();
  }, [load]);

  const handleDownloadCsv = useCallback(
    async (key: string, loader: () => Promise<{ blob: Blob; fileName: string }>) => {
      setDownloadError(null);
      setDownloadingKey(key);
      try {
        const { blob, fileName } = await loader();
        triggerBlobDownload(blob, fileName);
      } catch (err) {
        setDownloadError(describeError(err));
      } finally {
        setDownloadingKey(null);
      }
    },
    []
  );

  const readinessChip = useCallback((ok: boolean, label: string) => {
    return (
      <span
        className={
          ok
            ? "rounded-full bg-emerald-50 px-2 py-0.5 text-xs font-medium text-emerald-900 ring-1 ring-emerald-200"
            : "rounded-full bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-600 ring-1 ring-slate-200"
        }
      >
        {label}: {ok ? "Yes" : "No"}
      </span>
    );
  }, []);

  return (
    <Card className="border-border shadow-sm">
      <CardHeader>
        <CardTitle className="flex flex-wrap items-center gap-2 text-lg">
          <Package className="h-5 w-5 text-emerald-800" aria-hidden />
          LGU export package (CSV)
        </CardTitle>
        <CardDescription>
          Downloads are generated on demand from your tenant workspace data—nothing is queued as a PDF job here.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {state.status === "loading" ? (
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" aria-hidden />
            Loading package summary…
          </div>
        ) : null}

        {state.status === "error" ? (
          <Alert variant="destructive">
            <AlertTitle>Package summary unavailable</AlertTitle>
            <AlertDescription className="space-y-3">
              <p>{state.message}</p>
              <Button type="button" variant="outline" size="sm" className="gap-2" onClick={() => void load()}>
                <RefreshCw className="h-4 w-4" aria-hidden />
                Retry
              </Button>
            </AlertDescription>
          </Alert>
        ) : null}

        {state.status === "ready" ? (
          <>
            <p className="text-xs text-muted-foreground">
              Generated {formatGeneratedAt(state.manifest.generatedAtUtc)} · {state.manifest.planTitle} · FY{" "}
              {state.manifest.planningPeriodStart}–{state.manifest.planningPeriodEnd}
            </p>

            <Alert variant="default" className="border-emerald-200 bg-emerald-50/70 text-emerald-950">
              <AlertTitle className="text-emerald-900">Internal preparation only</AlertTitle>
              <AlertDescription className="text-emerald-900/90">
                {state.manifest.notes.length > 0
                  ? state.manifest.notes.join(" ")
                  : "This package supports internal LCCAP preparation and review. It is not an official submission portal."}
              </AlertDescription>
            </Alert>

            {state.manifest.readiness.hasUnresolvedComments ? (
              <Alert variant="default" className="border-amber-200 bg-amber-50 text-amber-950">
                <AlertTitle className="text-amber-900">Unresolved section comments</AlertTitle>
                <AlertDescription className="text-amber-900/90">
                  This plan has {state.manifest.counts.unresolvedSectionComments} unresolved section comment
                  {state.manifest.counts.unresolvedSectionComments === 1 ? "" : "s"}. Resolve or track them before
                  external review.
                </AlertDescription>
              </Alert>
            ) : null}

            <div className="grid gap-3 sm:grid-cols-2">
              <div className="rounded-lg border border-border bg-white p-3 text-sm shadow-sm">
                <p className="text-xs font-medium uppercase tracking-wide text-slate-500">Counts</p>
                <dl className="mt-2 grid grid-cols-2 gap-x-3 gap-y-1 text-slate-800">
                  <dt className="text-muted-foreground">Documents</dt>
                  <dd className="text-right font-medium">{state.manifest.counts.documents}</dd>
                  <dt className="text-muted-foreground">Official evidence</dt>
                  <dd className="text-right font-medium">{state.manifest.counts.officialEvidence}</dd>
                  <dt className="text-muted-foreground">Public evidence</dt>
                  <dd className="text-right font-medium">{state.manifest.counts.publicEvidence}</dd>
                  <dt className="text-muted-foreground">Actions</dt>
                  <dd className="text-right font-medium">{state.manifest.counts.actions}</dd>
                  <dt className="text-muted-foreground">Indicators</dt>
                  <dd className="text-right font-medium">{state.manifest.counts.monitoringIndicators}</dd>
                  <dt className="text-muted-foreground">Monitoring updates</dt>
                  <dd className="text-right font-medium">{state.manifest.counts.monitoringUpdates}</dd>
                  <dt className="text-muted-foreground">Funding rows</dt>
                  <dd className="text-right font-medium">{state.manifest.counts.fundingAllocations}</dd>
                  <dt className="text-muted-foreground">CCET-tagged</dt>
                  <dd className="text-right font-medium">{state.manifest.counts.ccetTaggedAllocations}</dd>
                </dl>
              </div>
              <div className="rounded-lg border border-border bg-white p-3 text-sm shadow-sm">
                <p className="text-xs font-medium uppercase tracking-wide text-slate-500">Readiness</p>
                <div className="mt-2 flex flex-wrap gap-2">
                  {readinessChip(state.manifest.readiness.hasOfficialEvidence, "Official evidence")}
                  {readinessChip(state.manifest.readiness.hasActions, "Actions")}
                  {readinessChip(state.manifest.readiness.hasMonitoring, "Monitoring")}
                  {readinessChip(state.manifest.readiness.hasFundingAllocations, "Funding")}
                </div>
              </div>
            </div>

            {downloadError ? (
              <Alert variant="destructive">
                <AlertTitle>Download failed</AlertTitle>
                <AlertDescription>{downloadError}</AlertDescription>
              </Alert>
            ) : null}

            <div className="flex flex-wrap gap-2">
              <Button
                type="button"
                variant="secondary"
                className="gap-2"
                disabled={downloadingKey !== null}
                onClick={() =>
                  void handleDownloadCsv("evidence", () => documentClient.downloadEvidenceIndexCsv(planId))
                }
              >
                {downloadingKey === "evidence" ? (
                  <Loader2 className="h-4 w-4 animate-spin" aria-hidden />
                ) : (
                  <Download className="h-4 w-4" aria-hidden />
                )}
                Evidence index CSV
              </Button>
              <Button
                type="button"
                variant="outline"
                className="gap-2"
                disabled={downloadingKey !== null}
                onClick={() =>
                  void handleDownloadCsv("action", () => actionClient.downloadActionMatrixCsv(planId))
                }
              >
                {downloadingKey === "action" ? (
                  <Loader2 className="h-4 w-4 animate-spin" aria-hidden />
                ) : (
                  <Download className="h-4 w-4" aria-hidden />
                )}
                Action matrix CSV
              </Button>
              <Button
                type="button"
                variant="outline"
                className="gap-2"
                disabled={downloadingKey !== null}
                onClick={() =>
                  void handleDownloadCsv("monitoring", () => actionClient.downloadMonitoringMatrixCsv(planId))
                }
              >
                {downloadingKey === "monitoring" ? (
                  <Loader2 className="h-4 w-4 animate-spin" aria-hidden />
                ) : (
                  <Download className="h-4 w-4" aria-hidden />
                )}
                Monitoring matrix CSV
              </Button>
              <Button
                type="button"
                variant="outline"
                className="gap-2"
                disabled={downloadingKey !== null}
                onClick={() =>
                  void handleDownloadCsv("funding", () => actionClient.downloadFundingReadinessCsv(planId))
                }
              >
                {downloadingKey === "funding" ? (
                  <Loader2 className="h-4 w-4 animate-spin" aria-hidden />
                ) : (
                  <Download className="h-4 w-4" aria-hidden />
                )}
                Funding readiness CSV
              </Button>
            </div>
          </>
        ) : null}
      </CardContent>
    </Card>
  );
}
