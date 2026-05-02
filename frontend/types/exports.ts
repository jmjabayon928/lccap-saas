/**
 * HTTP-oriented export job types for the LCCAP API client (no database DTOs).
 */

export type ExportType = "Pdf";

export type ExportStatus = "Queued" | "Running" | "Completed" | "Failed" | "Cancelled";

export interface CreatePdfExportResult {
  readonly exportJobId: string;
  readonly status: ExportStatus;
  readonly fileAssetId: string | null;
}

export interface ExportJobSummary {
  readonly id: string;
  readonly planId: string | null;
  readonly status: ExportStatus;
  readonly fileAssetId: string | null;
  readonly exportType: ExportType;
  readonly createdAtUtc: string | null;
  readonly startedAtUtc: string | null;
  readonly completedAtUtc: string | null;
  readonly failedAtUtc: string | null;
  readonly errorMessage: string | null;
}
