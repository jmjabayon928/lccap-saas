import { ApiError } from "@/lib/api/api-error";
import type { DocumentSummary, UploadDocumentResult } from "@/types/documents";

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function isFiniteNumber(value: unknown): value is number {
  return typeof value === "number" && Number.isFinite(value);
}

function isNonEmptyString(value: unknown): value is string {
  return typeof value === "string" && value.trim().length > 0;
}

function optionalStringOrNull(value: unknown): string | null {
  if (value === null || value === undefined) {
    return null;
  }
  if (typeof value === "string") {
    return value;
  }
  return null;
}

function optionalIsoOrNull(value: unknown): string | null {
  return optionalStringOrNull(value);
}

/** Ignore server storage paths — never surface to UI. */
function stripIgnoredFields(record: Record<string, unknown>): void {
  delete record.storedPath;
  delete record.stored_path;
}

function parseDocumentRecord(raw: Record<string, unknown>): DocumentSummary | null {
  stripIgnoredFields(raw);

  const idRaw = raw.id ?? raw.documentId;
  const planId = raw.planId;
  const fileAssetId = raw.fileAssetId;
  const title = raw.title;
  const category = raw.category;
  const description = optionalStringOrNull(raw.description);
  const originalFileNameRaw = raw.originalFileName ?? raw.fileName;
  const contentType = raw.contentType;
  const sizeBytes = raw.sizeBytes;
  const uploadedAtUtc = optionalIsoOrNull(raw.uploadedAtUtc);
  const createdAtUtc = optionalIsoOrNull(raw.createdAtUtc);

  if (
    !isNonEmptyString(idRaw) ||
    !isNonEmptyString(planId) ||
    !isNonEmptyString(fileAssetId) ||
    !isNonEmptyString(title) ||
    typeof category !== "string" ||
    !isNonEmptyString(category) ||
    !isNonEmptyString(originalFileNameRaw) ||
    typeof contentType !== "string" ||
    !contentType.trim() ||
    !isFiniteNumber(sizeBytes)
  ) {
    return null;
  }

  return {
    id: idRaw,
    planId,
    fileAssetId,
    title,
    category,
    description,
    originalFileName: originalFileNameRaw,
    contentType,
    sizeBytes,
    uploadedAtUtc,
    createdAtUtc
  };
}

export function parseDocumentSummary(payload: unknown): DocumentSummary {
  if (!isRecord(payload)) {
    throw new ApiError("Invalid document response: expected object", 502, payload);
  }
  const inner = isRecord(payload.document) ? payload.document : payload;
  stripIgnoredFields(inner);
  const parsed = parseDocumentRecord(inner);
  if (!parsed) {
    throw new ApiError("Invalid document response: malformed fields", 502, payload);
  }
  return parsed;
}

function sortKey(d: DocumentSummary): number {
  const primary = d.uploadedAtUtc ?? d.createdAtUtc;
  if (!primary) {
    return 0;
  }
  const t = Date.parse(primary);
  return Number.isNaN(t) ? 0 : t;
}

export function parseDocumentsList(payload: unknown): DocumentSummary[] {
  let list: unknown[] = [];

  if (Array.isArray(payload)) {
    list = payload;
  } else if (isRecord(payload)) {
    if (Array.isArray(payload.documents)) {
      list = payload.documents;
    } else if (Array.isArray(payload.items)) {
      list = payload.items;
    } else {
      throw new ApiError("Invalid documents list response: expected array or wrapper", 502, payload);
    }
  } else {
    throw new ApiError("Invalid documents list response: expected array or object", 502, payload);
  }

  const parsed: DocumentSummary[] = [];
  for (const item of list) {
    if (!isRecord(item)) {
      throw new ApiError("Invalid documents list response: malformed entry", 502, payload);
    }
    stripIgnoredFields(item);
    const doc = parseDocumentRecord(item);
    if (!doc) {
      throw new ApiError("Invalid documents list response: malformed document fields", 502, payload);
    }
    parsed.push(doc);
  }

  return [...parsed].sort((a, b) => sortKey(b) - sortKey(a));
}

export function parseUploadDocumentResult(payload: unknown): UploadDocumentResult {
  let record: Record<string, unknown> | null = null;

  if (isRecord(payload)) {
    if (isRecord(payload.document)) {
      record = payload.document;
    } else {
      record = payload;
    }
  }

  if (!record) {
    throw new ApiError("Invalid upload response: expected object", 502, payload);
  }
  stripIgnoredFields(record);

  const idRaw = record.id ?? record.documentId;
  const fileAssetId = record.fileAssetId;
  const planId = record.planId;
  const title = record.title;
  const category = record.category;
  const originalFileNameRaw = record.originalFileName ?? record.fileName;
  const contentType = record.contentType;
  const sizeBytes = record.sizeBytes;

  if (
    !isNonEmptyString(idRaw) ||
    !isNonEmptyString(fileAssetId) ||
    !isNonEmptyString(planId) ||
    !isNonEmptyString(title) ||
    typeof category !== "string" ||
    !isNonEmptyString(category) ||
    !isNonEmptyString(originalFileNameRaw) ||
    typeof contentType !== "string" ||
    !contentType.trim() ||
    !isFiniteNumber(sizeBytes)
  ) {
    throw new ApiError("Invalid upload response: malformed fields", 502, payload);
  }

  return {
    id: idRaw,
    fileAssetId,
    planId,
    title,
    category,
    originalFileName: originalFileNameRaw,
    contentType,
    sizeBytes
  };
}
