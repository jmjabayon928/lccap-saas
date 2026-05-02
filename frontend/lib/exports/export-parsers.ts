import { ApiError } from "@/lib/api/api-error";
import type {
  CreatePdfExportResult,
  ExportJobSummary,
  ExportStatus,
  ExportType
} from "@/types/exports";

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function isNonEmptyString(value: unknown): value is string {
  return typeof value === "string" && value.trim().length > 0;
}

function optionalIsoOrNull(value: unknown): string | null {
  if (value === null || value === undefined) {
    return null;
  }
  if (typeof value === "string") {
    return value;
  }
  return null;
}

const ALLOWED_EXPORT_STATUSES: readonly ExportStatus[] = [
  "Queued",
  "Running",
  "Completed",
  "Failed",
  "Cancelled"
];

function parseExportStatus(raw: unknown): ExportStatus | null {
  if (typeof raw !== "string") {
    return null;
  }
  return ALLOWED_EXPORT_STATUSES.includes(raw as ExportStatus) ? (raw as ExportStatus) : null;
}

function parseExportType(raw: unknown): ExportType | null {
  if (raw === "Pdf") {
    return "Pdf";
  }
  return null;
}

function optionalFileAssetId(value: unknown): string | null {
  if (value === null || value === undefined) {
    return null;
  }
  if (typeof value === "string" && value.trim()) {
    return value;
  }
  return null;
}

function unwrapExportJobPayload(payload: unknown): Record<string, unknown> | null {
  if (!isRecord(payload)) {
    return null;
  }
  // Ignore storedPath / stored_path deliberately — never surface storage internals.
  if (isRecord(payload.exportJob)) {
    return payload.exportJob;
  }
  return payload;
}

export function parseCreatePdfExportResult(payload: unknown): CreatePdfExportResult {
  const record = unwrapExportJobPayload(payload);
  if (!record) {
    throw new ApiError("Invalid create PDF export response: expected object", 502, payload);
  }

  const idRaw = record.exportJobId ?? record.id;
  const status = parseExportStatus(record.status);
  const fileAssetId = optionalFileAssetId(record.fileAssetId);

  if (!isNonEmptyString(idRaw)) {
    throw new ApiError("Invalid create PDF export response: missing export job identifier", 502, payload);
  }
  if (!status) {
    throw new ApiError("Invalid create PDF export response: missing or invalid status", 502, payload);
  }

  return {
    exportJobId: idRaw,
    status,
    fileAssetId
  };
}

export function parseExportJobSummary(payload: unknown): ExportJobSummary {
  const record = unwrapExportJobPayload(payload);
  if (!record) {
    throw new ApiError("Invalid export job response: expected object", 502, payload);
  }

  const idRaw = record.id ?? record.exportJobId;
  const planIdRaw = record.planId;
  const status = parseExportStatus(record.status);
  const fileAssetId = optionalFileAssetId(record.fileAssetId);
  const exportTypeParsed = parseExportType(record.exportType);
  const exportType: ExportType = exportTypeParsed ?? "Pdf";
  const createdAtUtc = optionalIsoOrNull(record.createdAtUtc);
  const startedAtUtc = optionalIsoOrNull(record.startedAtUtc);
  const completedAtUtc = optionalIsoOrNull(record.completedAtUtc);
  const failedAtUtc = optionalIsoOrNull(record.failedAtUtc);
  const errRaw = record.errorMessage;
  const errorMessage = typeof errRaw === "string" ? errRaw : null;

  if (!isNonEmptyString(idRaw)) {
    throw new ApiError("Invalid export job response: missing export job identifier", 502, payload);
  }
  if (!status) {
    throw new ApiError("Invalid export job response: missing or invalid status", 502, payload);
  }

  const planId =
    planIdRaw === null || planIdRaw === undefined
      ? null
      : typeof planIdRaw === "string" && planIdRaw.trim()
        ? planIdRaw
        : null;

  return {
    id: idRaw,
    planId,
    status,
    fileAssetId,
    exportType,
    createdAtUtc,
    startedAtUtc,
    completedAtUtc,
    failedAtUtc,
    errorMessage
  };
}
