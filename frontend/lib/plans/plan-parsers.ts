import { ApiError } from "@/lib/api/api-error";
import type {
  CreatePlanResult,
  PlanSectionSummary,
  PlanStatus,
  PlanSummary,
  SavePlanSectionResult,
  TemplateMode
} from "@/types/plans";

const ALLOWED_PLAN_STATUS = new Set<string>([
  "Draft",
  "InProgress",
  "ReadyForExport",
  "Submitted",
  "Approved",
  "Archived"
]);

const ALLOWED_TEMPLATE_MODE = new Set<string>(["New", "Partial", "Enhancement"]);

function isAllowedPlanStatus(value: string): value is PlanStatus {
  return ALLOWED_PLAN_STATUS.has(value);
}

function isAllowedTemplateMode(value: string): value is TemplateMode {
  return ALLOWED_TEMPLATE_MODE.has(value);
}

function sortPlansNewestFirst(plans: PlanSummary[]): PlanSummary[] {
  return [...plans].sort((a, b) => {
    const ta = Date.parse(a.updatedAtUtc ?? a.createdAtUtc ?? "") || 0;
    const tb = Date.parse(b.updatedAtUtc ?? b.createdAtUtc ?? "") || 0;
    return tb - ta;
  });
}

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

function parsePlanSummaryFromRecord(record: Record<string, unknown>, payloadForError: unknown): PlanSummary {
  const idRaw = record.id ?? record.planId;
  const accountId = record.accountId;
  const title = record.title;
  const startYear = record.startYear;
  const endYear = record.endYear;
  const statusRaw = record.status;
  const templateModeRaw = record.templateMode;
  const versionNumber = record.versionNumber;
  const descriptionRaw = record.description;
  const createdAtUtc = optionalIsoOrNull(record.createdAtUtc);
  const updatedAtUtc = optionalIsoOrNull(record.updatedAtUtc);

  if (!isNonEmptyString(idRaw)) {
    throw new ApiError("Invalid plan response: missing plan identifier", 502, payloadForError);
  }
  if (!isNonEmptyString(accountId)) {
    throw new ApiError("Invalid plan response: missing account id", 502, payloadForError);
  }
  if (!isNonEmptyString(title)) {
    throw new ApiError("Invalid plan response: missing title", 502, payloadForError);
  }
  if (!isFiniteNumber(startYear) || !isFiniteNumber(endYear)) {
    throw new ApiError("Invalid plan response: invalid year fields", 502, payloadForError);
  }
  if (!isNonEmptyString(statusRaw) || !isAllowedPlanStatus(statusRaw)) {
    throw new ApiError("Invalid plan response: invalid status", 502, payloadForError);
  }
  if (!isNonEmptyString(templateModeRaw) || !isAllowedTemplateMode(templateModeRaw)) {
    throw new ApiError("Invalid plan response: invalid templateMode", 502, payloadForError);
  }
  if (
    !isFiniteNumber(versionNumber) ||
    !Number.isInteger(versionNumber) ||
    versionNumber < 1
  ) {
    throw new ApiError("Invalid plan response: invalid version number", 502, payloadForError);
  }

  let description: string | null;
  if (descriptionRaw === undefined || descriptionRaw === null) {
    description = null;
  } else if (typeof descriptionRaw === "string") {
    description = descriptionRaw;
  } else {
    throw new ApiError("Invalid plan response: invalid description", 502, payloadForError);
  }

  return {
    id: idRaw,
    accountId,
    title,
    startYear,
    endYear,
    status: statusRaw,
    templateMode: templateModeRaw,
    versionNumber,
    description,
    createdAtUtc,
    updatedAtUtc
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

  return parsePlanSummaryFromRecord(record, payload);
}

export function parsePlansList(payload: unknown): PlanSummary[] {
  let list: unknown[] = [];

  if (Array.isArray(payload)) {
    list = payload;
  } else if (isRecord(payload) && Array.isArray(payload.plans)) {
    list = payload.plans;
  } else {
    throw new ApiError(
      "Invalid plans list response: expected array or plans wrapper",
      502,
      payload
    );
  }

  const parsed: PlanSummary[] = [];
  for (let i = 0; i < list.length; i++) {
    const item = list[i];
    if (!isRecord(item)) {
      throw new ApiError(`Invalid plans list: entry ${i} is not an object`, 502, payload);
    }
    parsed.push(parsePlanSummaryFromRecord(item, payload));
  }
  return sortPlansNewestFirst(parsed);
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
  if (!isRecord(payload)) {
    throw new ApiError("Invalid save section response: expected object", 502, payload);
  }

  // 1. Try compact response (actual backend shape)
  const sectionId = payload.sectionId;
  const lastEditedAtUtc = payload.lastEditedAtUtc;
  const lastEditedByUserId = payload.lastEditedByUserId;

  if (isNonEmptyString(sectionId) && isNonEmptyString(lastEditedAtUtc)) {
    return {
      sectionId,
      lastEditedAtUtc,
      lastEditedByUserId: typeof lastEditedByUserId === "string" ? lastEditedByUserId : undefined
    };
  }

  // 2. Try full section object (fallback/legacy)
  // We use parsePlanSection but wrap it to match SavePlanSectionResult
  try {
    const full = parsePlanSection(payload);
    return {
      ...full,
      sectionId: full.id,
      lastEditedAtUtc: full.lastEditedAtUtc ?? new Date().toISOString()
    };
  } catch {
    // If both fail, throw a specific error
    throw new ApiError("Invalid section response: malformed fields", 502, payload);
  }
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
