import { ApiError } from "@/lib/api/api-error";
import { DOCUMENT_CATEGORIES } from "@/types/documents";
import type {
  DocumentCategory,
  DocumentSummary,
  EvidenceIndexItem,
  EvidenceIndexResult,
  EvidenceStatus,
  UploadDocumentResult
} from "@/types/documents";

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

const ALLOWED_EVIDENCE_STATUSES: readonly EvidenceStatus[] = ["Draft", "Internal", "Official", "Public"];

function normalizeEvidenceStatus(input: string | null): EvidenceStatus {
  const trimmed = input?.trim();
  if (!trimmed) {
    return "Internal";
  }

  const normalized = ALLOWED_EVIDENCE_STATUSES.find(
    (s) => s.toLowerCase() === trimmed.toLowerCase()
  );
  return normalized ?? "Internal";
}

function normalizeDocumentCategory(input: string | null): DocumentCategory {
  const trimmed = input?.trim();
  if (!trimmed) {
    return "Other";
  }

  const normalized = DOCUMENT_CATEGORIES.find((c) => c.toLowerCase() === trimmed.toLowerCase());
  return normalized ?? "Other";
}

/** Ignore server storage paths — never surface to UI. */
function stripIgnoredFields(record: Record<string, unknown>): void {
  delete record.storedPath;
  delete record.stored_path;
}

function parseTagsField(raw: Record<string, unknown>): string[] {
  const direct = raw.tags;
  if (Array.isArray(direct)) {
    const out: string[] = [];
    for (const el of direct) {
      if (typeof el === "string") {
        const t = el.trim();
        if (t.length > 0) {
          out.push(t);
        }
      }
    }
    return out;
  }

  const tagsJson = raw.tagsJson;
  if (typeof tagsJson === "string" && tagsJson.trim().length > 0) {
    try {
      const parsed: unknown = JSON.parse(tagsJson);
      if (Array.isArray(parsed)) {
        const out: string[] = [];
        for (const el of parsed) {
          if (typeof el === "string") {
            const t = el.trim();
            if (t.length > 0) {
              out.push(t);
            }
          }
        }
        return out;
      }
    } catch {
      return [];
    }
  }

  return [];
}

function parseDocumentRecord(raw: Record<string, unknown>): DocumentSummary | null {
  stripIgnoredFields(raw);

  const idRaw = raw.id ?? raw.documentId;
  const planId = raw.planId;
  const fileAssetId = optionalStringOrNull(raw.fileAssetId);
  const title = optionalStringOrNull(raw.title);
  const category = raw.category;
  const description = optionalStringOrNull(raw.description);
  const documentDate = optionalIsoOrNull(raw.documentDate);
  const sourceAgency = optionalStringOrNull(raw.sourceAgency);
  const planSectionId = optionalStringOrNull(raw.planSectionId);
  const actionItemId = optionalStringOrNull(raw.actionItemId);
  const evidenceStatus = normalizeEvidenceStatus(optionalStringOrNull(raw.evidenceStatus));
  const tags = parseTagsField(raw);
  const originalFileNameRaw = raw.originalFileName ?? raw.fileName;
  const contentType = raw.contentType;
  const sizeBytes = raw.sizeBytes;
  const uploadedAtUtc = optionalIsoOrNull(raw.uploadedAtUtc ?? raw.fileCreatedAtUtc);
  const createdAtUtc = optionalIsoOrNull(raw.createdAtUtc);

  if (!isNonEmptyString(idRaw) || !isNonEmptyString(planId) || typeof category !== "string" || !isNonEmptyString(category)) {
    return null;
  }

  return {
    id: idRaw,
    planId,
    fileAssetId,
    title,
    category,
    description,
    documentDate,
    sourceAgency,
    planSectionId,
    actionItemId,
    evidenceStatus,
    tags,
    originalFileName: typeof originalFileNameRaw === "string" ? originalFileNameRaw : null,
    contentType: typeof contentType === "string" ? contentType : null,
    sizeBytes: isFiniteNumber(sizeBytes) ? sizeBytes : null,
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
  if (!isRecord(payload)) {
    throw new ApiError("Invalid upload response: expected object", 502, payload);
  }

  // 1. Try compact response (actual backend shape)
  const id = payload.id;
  if (isNonEmptyString(id)) {
    return { id };
  }

  // 2. Try full response (legacy/fallback)
  const inner = isRecord(payload.document) ? payload.document : payload;
  stripIgnoredFields(inner);

  const idRaw = inner.id ?? inner.documentId;
  const fileAssetId = inner.fileAssetId;
  const planId = inner.planId;
  const title = inner.title;
  const category = inner.category;
  const originalFileNameRaw = inner.originalFileName ?? inner.fileName;
  const contentType = inner.contentType;
  const sizeBytes = inner.sizeBytes;

  if (
    !isNonEmptyString(idRaw) ||
    !isNonEmptyString(fileAssetId) ||
    !isNonEmptyString(planId) ||
    typeof title !== "string" ||
    !title.trim() ||
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
    fileAssetId: String(fileAssetId),
    planId: String(planId),
    title: String(title),
    category: String(category),
    originalFileName: String(originalFileNameRaw),
    contentType: String(contentType),
    sizeBytes: Number(sizeBytes)
  };
}

function parseCountMap(raw: unknown): Record<string, number> {
  if (!isRecord(raw)) {
    return {};
  }
  const out: Record<string, number> = {};
  for (const [k, v] of Object.entries(raw)) {
    if (typeof k === "string" && isFiniteNumber(v)) {
      out[k] = v;
    }
  }
  return out;
}

export function parseEvidenceIndexItem(payload: unknown): EvidenceIndexItem {
  if (!isRecord(payload)) {
    throw new ApiError("Invalid evidence index item: expected object", 502, payload);
  }
  stripIgnoredFields(payload);

  const documentId = payload.documentId ?? payload.documentID ?? payload["DocumentId"];
  if (!isNonEmptyString(documentId)) {
    throw new ApiError("Invalid evidence index item: missing documentId", 502, payload);
  }

  const evidenceStatus = normalizeEvidenceStatus(
    optionalStringOrNull(payload.evidenceStatus ?? payload["EvidenceStatus"])
  );
  const category = normalizeDocumentCategory(optionalStringOrNull(payload.category ?? payload["Category"]));

  const createdAtUtc = optionalIsoOrNull(payload.createdAtUtc ?? payload["CreatedAtUtc"]);
  if (!isNonEmptyString(createdAtUtc)) {
    throw new ApiError("Invalid evidence index item: missing createdAtUtc", 502, payload);
  }

  const fileSizeBytes = payload.fileSizeBytes ?? payload["FileSizeBytes"];
  if (!isFiniteNumber(fileSizeBytes)) {
    throw new ApiError("Invalid evidence index item: missing fileSizeBytes", 502, payload);
  }

  return {
    documentId,
    title: optionalStringOrNull(payload.title ?? payload["Title"]),
    category,
    evidenceStatus,
    sourceAgency: optionalStringOrNull(payload.sourceAgency ?? payload["SourceAgency"]),
    documentDate: optionalIsoOrNull(payload.documentDate ?? payload["DocumentDate"]),
    description: optionalStringOrNull(payload.description ?? payload["Description"]),
    tags: parseTagsField(payload),
    planSectionId: optionalStringOrNull(payload.planSectionId ?? payload["PlanSectionId"]),
    planSectionKey: optionalStringOrNull(payload.planSectionKey ?? payload["PlanSectionKey"]),
    planSectionTitle: optionalStringOrNull(payload.planSectionTitle ?? payload["PlanSectionTitle"]),
    actionItemId: optionalStringOrNull(payload.actionItemId ?? payload["ActionItemId"]),
    actionTitle: optionalStringOrNull(payload.actionTitle ?? payload["ActionTitle"]),
    actionType: optionalStringOrNull(payload.actionType ?? payload["ActionType"]),
    actionSector: optionalStringOrNull(payload.actionSector ?? payload["ActionSector"]),
    originalFileName: optionalStringOrNull(payload.originalFileName ?? payload["OriginalFileName"]),
    contentType: optionalStringOrNull(payload.contentType ?? payload["ContentType"]),
    fileSizeBytes,
    sha256Hash: optionalStringOrNull(payload.sha256Hash ?? payload["Sha256Hash"]),
    uploadedByUserId: optionalStringOrNull(payload.uploadedByUserId ?? payload["UploadedByUserId"]),
    createdAtUtc
  };
}

export function parseEvidenceIndexResult(payload: unknown): EvidenceIndexResult {
  if (!isRecord(payload)) {
    throw new ApiError("Invalid evidence index response: expected object", 502, payload);
  }
  stripIgnoredFields(payload);

  const planId = payload.planId ?? payload["PlanId"];
  const generatedAtUtc = payload.generatedAtUtc ?? payload["GeneratedAtUtc"];
  const totalCount = payload.totalCount ?? payload["TotalCount"];
  const itemsRaw = payload.items ?? payload["Items"];

  if (!isNonEmptyString(planId) || !isNonEmptyString(generatedAtUtc) || !isFiniteNumber(totalCount) || !Array.isArray(itemsRaw)) {
    throw new ApiError("Invalid evidence index response: malformed fields", 502, payload);
  }

  const parsedItems: EvidenceIndexItem[] = [];
  for (const item of itemsRaw) {
    parsedItems.push(parseEvidenceIndexItem(item));
  }

  const countsByEvidenceStatusRaw = payload.countsByEvidenceStatus ?? payload["CountsByEvidenceStatus"];
  const countsByCategoryRaw = payload.countsByCategory ?? payload["CountsByCategory"];

  const countsByEvidenceStatus = parseCountMap(countsByEvidenceStatusRaw);
  const countsByCategory = parseCountMap(countsByCategoryRaw);

  return {
    planId,
    generatedAtUtc,
    items: parsedItems,
    countsByEvidenceStatus,
    countsByCategory,
    totalCount
  };
}
