import { ApiError } from "@/lib/api/api-error";
import type {
  CreatePlanResult,
  PlanSectionSummary,
  PlanSummary,
  SavePlanSectionResult
} from "@/types/plans";

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function isFiniteNumber(value: unknown): value is number {
  return typeof value === "number" && Number.isFinite(value);
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

export function parseCreatePlanResult(payload: unknown): CreatePlanResult {
  let record: Record<string, unknown> | null = null;

  if (isRecord(payload)) {
    if (isRecord(payload.plan)) {
      record = payload.plan;
    } else {
      record = payload;
    }
  }

  if (!record) {
    throw new ApiError("Invalid create plan response: expected object", 502, payload);
  }

  const planIdRaw = record.planId ?? record.id;
  const title = record.title;
  const startYear = record.startYear;
  const endYear = record.endYear;
  const status = record.status;

  if (!isNonEmptyString(planIdRaw)) {
    throw new ApiError("Invalid create plan response: missing plan identifier", 502, payload);
  }
  if (!isNonEmptyString(title)) {
    throw new ApiError("Invalid create plan response: missing title", 502, payload);
  }
  if (!isFiniteNumber(startYear) || !isFiniteNumber(endYear)) {
    throw new ApiError("Invalid create plan response: invalid year fields", 502, payload);
  }
  if (!isNonEmptyString(status)) {
    throw new ApiError("Invalid create plan response: missing status", 502, payload);
  }

  return {
    planId: planIdRaw,
    title,
    startYear,
    endYear,
    status
  };
}

export function parsePlanSummary(payload: unknown): PlanSummary {
  let record: Record<string, unknown> | null = null;

  if (isRecord(payload)) {
    if (isRecord(payload.plan)) {
      record = payload.plan;
    } else {
      record = payload;
    }
  }

  if (!record) {
    throw new ApiError("Invalid plan response: expected object", 502, payload);
  }

  const idRaw = record.id ?? record.planId;
  const title = record.title;
  const startYear = record.startYear;
  const endYear = record.endYear;
  const status = record.status;

  if (!isNonEmptyString(idRaw)) {
    throw new ApiError("Invalid plan response: missing plan identifier", 502, payload);
  }
  if (!isNonEmptyString(title)) {
    throw new ApiError("Invalid plan response: missing title", 502, payload);
  }
  if (!isFiniteNumber(startYear) || !isFiniteNumber(endYear)) {
    throw new ApiError("Invalid plan response: invalid year fields", 502, payload);
  }
  if (!isNonEmptyString(status)) {
    throw new ApiError("Invalid plan response: missing status", 502, payload);
  }

  return {
    id: idRaw,
    title,
    startYear,
    endYear,
    status
  };
}

function parseSectionRecord(raw: Record<string, unknown>): PlanSectionSummary | null {
  const id = raw.id;
  const planId = raw.planId;
  const sectionKey = raw.sectionKey;
  const title = raw.title;
  const content = raw.content;
  const sortOrder = raw.sortOrder;
  const lastEditedAtUtc = optionalIsoOrNull(raw.lastEditedAtUtc);

  if (
    !isNonEmptyString(id) ||
    !isNonEmptyString(planId) ||
    !isNonEmptyString(sectionKey) ||
    !isNonEmptyString(title) ||
    typeof content !== "string" ||
    !isFiniteNumber(sortOrder)
  ) {
    return null;
  }

  return {
    id,
    planId,
    sectionKey,
    title,
    content,
    sortOrder,
    lastEditedAtUtc
  };
}

function parseOneSection(raw: unknown): PlanSectionSummary | null {
  if (!isRecord(raw)) {
    return null;
  }
  return parseSectionRecord(raw);
}

function unwrapSectionPayload(payload: unknown): Record<string, unknown> | null {
  if (!isRecord(payload)) {
    return null;
  }
  if (isRecord(payload.section)) {
    return payload.section;
  }
  return payload;
}

export function parsePlanSection(payload: unknown): PlanSectionSummary {
  const record = unwrapSectionPayload(payload);
  if (!record) {
    throw new ApiError("Invalid section response: expected object", 502, payload);
  }
  const parsed = parseSectionRecord(record);
  if (!parsed) {
    throw new ApiError("Invalid section response: malformed fields", 502, payload);
  }
  return parsed;
}

export function parseSavePlanSectionResult(payload: unknown): SavePlanSectionResult {
  return parsePlanSection(payload);
}

export function parsePlanSections(payload: unknown): PlanSectionSummary[] {
  let list: unknown[] = [];

  if (Array.isArray(payload)) {
    list = payload;
  } else if (isRecord(payload) && Array.isArray(payload.sections)) {
    list = payload.sections;
  } else {
    throw new ApiError("Invalid plan sections response: expected array or sections wrapper", 502, payload);
  }

  const parsed: PlanSectionSummary[] = [];
  for (const item of list) {
    const section = parseOneSection(item);
    if (!section) {
      throw new ApiError("Invalid plan sections response: malformed section entry", 502, payload);
    }
    parsed.push(section);
  }

  return [...parsed].sort((a, b) => a.sortOrder - b.sortOrder);
}
