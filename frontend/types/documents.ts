/**
 * HTTP contract types for documents (no database or server-internal DTOs).
 */

export type DocumentCategory =
  | "Clup"
  | "Cdp"
  | "Drrm"
  | "HazardStudy"
  | "ClimateData"
  | "Map"
  | "Reference"
  | "Other";

export const DOCUMENT_CATEGORIES: readonly DocumentCategory[] = [
  "Clup",
  "Cdp",
  "Drrm",
  "HazardStudy",
  "ClimateData",
  "Map",
  "Reference",
  "Other"
] as const;

/** MVP frontend mirror of backend allowlist — validation is UX-only; API enforces truth. */
export const ALLOWED_DOCUMENT_EXTENSIONS = [
  ".pdf",
  ".docx",
  ".xlsx",
  ".png",
  ".jpg",
  ".jpeg"
] as const;

export const MAX_DOCUMENT_UPLOAD_BYTES = 25 * 1024 * 1024;

export interface DocumentSummary {
  readonly id: string;
  readonly planId: string;
  readonly fileAssetId: string | null;
  readonly title: string;
  readonly category: string;
  readonly description: string | null;
  readonly originalFileName: string | null;
  readonly contentType: string | null;
  readonly sizeBytes: number | null;
  readonly uploadedAtUtc: string | null;
  readonly createdAtUtc: string | null;
}

export interface UploadDocumentRequest {
  readonly planId: string;
  readonly category: DocumentCategory;
  readonly title: string;
  readonly description?: string;
  readonly file: File;
}

export interface UploadDocumentResult {
  readonly id: string;
  // Optional fields for backward compatibility or full responses
  readonly fileAssetId?: string;
  readonly planId?: string;
  readonly title?: string;
  readonly category?: string;
  readonly originalFileName?: string;
  readonly contentType?: string;
  readonly sizeBytes?: number;
}
