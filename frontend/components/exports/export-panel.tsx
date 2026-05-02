"use client";

import { useCallback, useState } from "react";
import { Download, Loader2, RefreshCw, Sparkles } from "lucide-react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ExportReadinessCard } from "@/components/exports/export-readiness-card";
import { ExportStatusBadge } from "@/components/exports/export-status-badge";
import { isApiError } from "@/lib/api/api-error";
import { exportClient } from "@/lib/exports/export-client";
import type { CreatePdfExportResult, ExportJobSummary } from "@/types/exports";

interface ExportPanelProps {
  readonly planId: string;
}

const DOWNLOAD_FILENAME = "lccap-draft-package.pdf";

function buildJobFromCreateResult(result: CreatePdfExportResult, planId: string): ExportJobSummary {
  return {
    id: result.exportJobId,
    planId,
    status: result.status,
    fileAssetId: result.fileAssetId,
    exportType: "Pdf",
    createdAtUtc: null,
    startedAtUtc: null,
    completedAtUtc: null,
    failedAtUtc: null,
    errorMessage: null
  };
}

function describeExportOrRefreshError(err: unknown): string {
  if (isApiError(err)) {
    if (err.status === 404) {
      return "This export could not be found, or you do not have access to it in your current tenant session.";
    }
    return err.message;
  }
  if (err instanceof Error) {
    return err.message;
  }
  return "Something went wrong while processing the export.";
}

function describeDownloadError(err: unknown): string {
  if (isApiError(err)) {
    if (err.status === 404) {
      return "The export file could not be found, or you do not have access to it in your current session.";
    }
    if (err.status === 409) {
      return "Export is not ready yet. Refresh status until the job completes, then try again.";
    }
    return err.message;
  }
  if (err instanceof Error) {
    return err.message;
  }
  return "Could not download the export file.";
}

export function ExportPanel({ planId }: ExportPanelProps) {
  const [latestJob, setLatestJob] = useState<ExportJobSummary | null>(null);
  const [panelError, setPanelError] = useState<string | null>(null);
  const [generating, setGenerating] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [downloading, setDownloading] = useState(false);

  const handleGenerate = useCallback(async () => {
    setPanelError(null);
    setGenerating(true);
    try {
      const created = await exportClient.createPdfExport(planId);
      try {
        const refreshed = await exportClient.getExportJob(created.exportJobId);
        setLatestJob(refreshed);
      } catch {
        setLatestJob(buildJobFromCreateResult(created, planId));
      }
    } catch (err) {
      setPanelError(describeExportOrRefreshError(err));
    } finally {
      setGenerating(false);
    }
  }, [planId]);

  const handleRefreshStatus = useCallback(async () => {
    if (!latestJob?.id) {
      return;
    }
    setPanelError(null);
    setRefreshing(true);
    try {
      const refreshed = await exportClient.getExportJob(latestJob.id);
      setLatestJob(refreshed);
    } catch (err) {
      setPanelError(describeExportOrRefreshError(err));
    } finally {
      setRefreshing(false);
    }
  }, [latestJob?.id]);

  const handleDownload = useCallback(async () => {
    if (!latestJob?.id) {
      return;
    }
    setPanelError(null);
    setDownloading(true);
    try {
      const blob = await exportClient.downloadExport(latestJob.id);
      const url = URL.createObjectURL(blob);
      try {
        const anchor = document.createElement("a");
        anchor.href = url;
        anchor.download = DOWNLOAD_FILENAME;
        anchor.rel = "noopener";
        document.body.appendChild(anchor);
        anchor.click();
        anchor.remove();
      } finally {
        window.setTimeout(() => {
          URL.revokeObjectURL(url);
        }, 1500);
      }
    } catch (err) {
      setPanelError(describeDownloadError(err));
    } finally {
      setDownloading(false);
    }
  }, [latestJob?.id]);

  const showRefresh =
    latestJob &&
    (latestJob.status === "Queued" || latestJob.status === "Running");

  const canDownload = latestJob?.status === "Completed";

  return (
    <div className="space-y-4">
      <ExportReadinessCard />

      <Card className="border-border shadow-sm">
        <CardHeader>
          <CardTitle className="flex flex-wrap items-center gap-2 text-lg">
            <Sparkles className="h-5 w-5 text-emerald-700" aria-hidden />
            Generate PDF draft package
          </CardTitle>
          <CardDescription>
            Creates a working PDF through the API for this plan only—use for internal preparation and draft packaging,
            not as an official submission channel.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {panelError ? (
            <Alert variant="destructive">
              <AlertTitle>Export issue</AlertTitle>
              <AlertDescription>{panelError}</AlertDescription>
            </Alert>
          ) : null}

          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              className="gap-2"
              onClick={() => void handleGenerate()}
              disabled={generating}
            >
              {generating ? <Loader2 className="h-4 w-4 animate-spin" aria-hidden /> : null}
              Generate PDF draft
            </Button>
            {showRefresh ? (
              <Button
                type="button"
                variant="outline"
                className="gap-2"
                onClick={() => void handleRefreshStatus()}
                disabled={refreshing}
              >
                {refreshing ? <Loader2 className="h-4 w-4 animate-spin" aria-hidden /> : <RefreshCw className="h-4 w-4" aria-hidden />}
                Refresh status
              </Button>
            ) : null}
            {canDownload ? (
              <Button
                type="button"
                variant="secondary"
                className="gap-2"
                onClick={() => void handleDownload()}
                disabled={downloading}
              >
                {downloading ? <Loader2 className="h-4 w-4 animate-spin" aria-hidden /> : <Download className="h-4 w-4" aria-hidden />}
                Download PDF
              </Button>
            ) : null}
          </div>

          {latestJob ? (
            <div className="rounded-lg border border-border bg-white px-4 py-3 text-sm shadow-sm">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <p className="font-medium text-slate-900">Latest export job</p>
                <ExportStatusBadge status={latestJob.status} />
              </div>
              <dl className="mt-3 grid gap-2 text-muted-foreground sm:grid-cols-2">
                <div>
                  <dt className="text-xs uppercase tracking-wide text-slate-500">Job ID</dt>
                  <dd className="font-mono text-xs text-slate-800">{latestJob.id}</dd>
                </div>
                <div>
                  <dt className="text-xs uppercase tracking-wide text-slate-500">Type</dt>
                  <dd>{latestJob.exportType}</dd>
                </div>
                {latestJob.errorMessage ? (
                  <div className="sm:col-span-2">
                    <dt className="text-xs uppercase tracking-wide text-slate-500">Message</dt>
                    <dd className="text-amber-900">{latestJob.errorMessage}</dd>
                  </div>
                ) : null}
              </dl>
            </div>
          ) : (
            <p className="text-sm text-muted-foreground">
              No export generated yet in this session. Use <strong className="font-medium text-slate-800">Generate PDF draft</strong>{" "}
              when you are ready to produce a working output.
            </p>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
