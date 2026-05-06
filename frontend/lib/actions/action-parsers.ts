import { ApiError } from "@/lib/api/api-error";
import type {
  ActionItemDetail,
  ActionItemSummary,
  ActionStatus,
  ActionType,
  ClimateExpenditureTagCategory,
  ClimateExpenditureTagSummary,
  ClimateExpenditureTagsResult,
  SaveActionItemResult
} from "@/types/actions";

const ACTION_TYPES: ReadonlySet<ActionType> = new Set(["Adaptation", "Mitigation"]);

const ACTION_STATUSES: ReadonlySet<ActionStatus> = new Set([
  "Planned",
  "InProgress",
  "OnTrack",
  "Delayed",
  "Completed",
  "Cancelled"
]);

const CCET_CATEGORIES: ReadonlySet<ClimateExpenditureTagCategory> = new Set([
  "Adaptation",
  "Mitigation",
  "CrossCutting",
  "DisasterRiskReduction",
  "CapacityDevelopment",
  "Other"
]);

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function isFiniteNumber(value: unknown): value is number {
  return typeof value === "number" && Number.isFinite(value);
}

function isNonEmptyString(value: unknown): value is string {
  return typeof value === "string" && value.trim().length > 0;
}

function nullableString(value: unknown): string | null {
  if (value === null || value === undefined) {
    return null;
  }
  if (typeof value === "string") {
    return value;
  }
  return null;
}

function optionalNumberOrNull(value: unknown): number | null {
  if (value === null || value === undefined) {
    return null;
  }
  if (isFiniteNumber(value)) {
    return value;
  }
  return null;
}

function parseActionType(raw: unknown): ActionType {
  if (typeof raw === "string" && ACTION_TYPES.has(raw as ActionType)) {
    return raw as ActionType;
  }
  throw new ApiError("Invalid action item: actionType must be Adaptation or Mitigation", 502, raw);
}

function parseActionStatus(raw: unknown): ActionStatus {
  if (typeof raw === "string" && ACTION_STATUSES.has(raw as ActionStatus)) {
    return raw as ActionStatus;
  }
  throw new ApiError("Invalid action item: status is not a supported value", 502, raw);
}

function parseClimateExpenditureTagCategory(raw: unknown): ClimateExpenditureTagCategory {
  if (typeof raw === "string" && CCET_CATEGORIES.has(raw as ClimateExpenditureTagCategory)) {
    return raw as ClimateExpenditureTagCategory;
  }
  return "Other";
}

function activitySortMs(updatedAtUtc: string | null, createdAtUtc: string | null): number {
  const u = updatedAtUtc?.trim() ? Date.parse(updatedAtUtc) : NaN;
  if (!Number.isNaN(u)) {
    return u;
  }
  const c = createdAtUtc?.trim() ? Date.parse(createdAtUtc) : NaN;
  return Number.isNaN(c) ? 0 : c;
}

function parseActionRecord(raw: Record<string, unknown>): ActionItemDetail | null {
  const idRaw = raw.id ?? raw.actionItemId;
  const planId = raw.planId;
  const title = raw.title;
  const description = nullableString(raw.description);
  const actionTypeRaw = raw.actionType;
  const sector = raw.sector;
  const responsibleOffice = nullableString(raw.responsibleOffice);
  const budgetAmount = raw.budgetAmount;
  const fundingSource = nullableString(raw.fundingSource);
  const timelineStartUtc = nullableString(raw.timelineStartUtc);
  const timelineEndUtc = nullableString(raw.timelineEndUtc);
  const kpi = nullableString(raw.kpi);
  const priorityScore = optionalNumberOrNull(raw.priorityScore);
  const statusRaw = raw.status;
  const createdAtUtc = nullableString(raw.createdAtUtc);
  const updatedAtUtc = nullableString(raw.updatedAtUtc);
  const rowVersionRaw = raw.rowVersion;

  if (
    !isNonEmptyString(idRaw) ||
    !isNonEmptyString(planId) ||
    !isNonEmptyString(title) ||
    !isNonEmptyString(sector)
  ) {
    return null;
  }

  let rowVersion: string | null = null;
  if (typeof rowVersionRaw === "string" && rowVersionRaw.trim()) {
    rowVersion = rowVersionRaw.trim();
  }

  if (!isFiniteNumber(budgetAmount) || budgetAmount < 0) {
    return null;
  }

  let actionType: ActionType;
  let status: ActionStatus;
  try {
    actionType = parseActionType(actionTypeRaw);
    status = parseActionStatus(statusRaw);
  } catch {
    return null;
  }

  if (priorityScore !== null && (priorityScore < 0 || priorityScore > 100)) {
    return null;
  }

  return {
    id: idRaw,
    planId,
    rowVersion,
    title,
    description,
    actionType,
    sector,
    responsibleOffice,
    budgetAmount,
    fundingSource,
    timelineStartUtc,
    timelineEndUtc,
    kpi,
    priorityScore,
    status,
    createdAtUtc,
    updatedAtUtc
  };
}

function unwrapActionPayload(payload: unknown): Record<string, unknown> | null {
  if (!isRecord(payload)) {
    return null;
  }
  if (isRecord(payload.action)) {
    return payload.action;
  }
  return payload;
}

export function parseActionItem(payload: unknown): ActionItemDetail {
  const record = unwrapActionPayload(payload);
  if (!record) {
    throw new ApiError("Invalid action response: expected object", 502, payload);
  }
  const parsed = parseActionRecord(record);
  if (!parsed) {
    throw new ApiError("Invalid action response: malformed fields", 502, payload);
  }
  return parsed;
}

export function parseSaveActionItemResult(payload: unknown): SaveActionItemResult {
  return parseActionItem(payload);
}

export function parseActionItemsList(payload: unknown): ActionItemSummary[] {
  let list: unknown[] = [];

  if (Array.isArray(payload)) {
    list = payload;
  } else if (isRecord(payload) && Array.isArray(payload.actions)) {
    list = payload.actions;
  } else if (isRecord(payload) && Array.isArray(payload.items)) {
    list = payload.items;
  } else {
    throw new ApiError(
      "Invalid actions list response: expected array, actions, or items wrapper",
      502,
      payload
    );
  }

  const parsed: ActionItemSummary[] = [];
  for (const item of list) {
    if (!isRecord(item)) {
      throw new ApiError("Invalid actions list response: malformed entry", 502, payload);
    }
    const row = parseActionRecord(item);
    if (!row) {
      throw new ApiError("Invalid actions list response: malformed entry fields", 502, payload);
    }
    parsed.push(row);
  }

  return [...parsed].sort(
    (a, b) => activitySortMs(b.updatedAtUtc, b.createdAtUtc) - activitySortMs(a.updatedAtUtc, a.createdAtUtc)
  );
}

function parseClimateExpenditureTagRecord(raw: Record<string, unknown>): ClimateExpenditureTagSummary | null {
  const idRaw = raw.id;
  const tagCode = raw.tagCode;
  const tagName = raw.tagName;
  const tagCategory = parseClimateExpenditureTagCategory(raw.tagCategory);
  const weightPercent = optionalNumberOrNull(raw.weightPercent);
  const description = nullableString(raw.description);
  const isActiveRaw = raw.isActive;
  const createdAtUtc = nullableString(raw.createdAtUtc);

  if (!isNonEmptyString(idRaw) || !isNonEmptyString(tagCode) || !isNonEmptyString(tagName)) {
    return null;
  }

  if (typeof isActiveRaw !== "boolean") {
    return null;
  }

  if (weightPercent !== null && (!Number.isFinite(weightPercent) || weightPercent < 0 || weightPercent > 100)) {
    return null;
  }

  return {
    id: idRaw.trim(),
    tagCode: tagCode.trim(),
    tagName: tagName.trim(),
    tagCategory,
    weightPercent,
    description,
    isActive: isActiveRaw,
    createdAtUtc
  };
}

export function parseClimateExpenditureTagSummary(payload: unknown): ClimateExpenditureTagSummary {
  if (!isRecord(payload)) {
    throw new ApiError("Invalid CCET tag response: expected object", 502, payload);
  }
  const parsed = parseClimateExpenditureTagRecord(payload);
  if (!parsed) {
    throw new ApiError("Invalid CCET tag response: malformed fields", 502, payload);
  }
  return parsed;
}

export function parseClimateExpenditureTagsResult(payload: unknown): ClimateExpenditureTagsResult {
  if (!isRecord(payload)) {
    throw new ApiError("Invalid CCET catalog response: expected object", 502, payload);
  }

  const itemsRaw = payload.items;
  if (!Array.isArray(itemsRaw)) {
    throw new ApiError("Invalid CCET catalog response: items must be an array", 502, payload);
  }

  const totalCountRaw = payload.totalCount;
  if (!isFiniteNumber(totalCountRaw) || totalCountRaw < 0 || !Number.isInteger(totalCountRaw)) {
    throw new ApiError("Invalid CCET catalog response: totalCount invalid", 502, payload);
  }

  const includeInactiveRaw = payload.includeInactive;
  if (typeof includeInactiveRaw !== "boolean") {
    throw new ApiError("Invalid CCET catalog response: includeInactive must be boolean", 502, payload);
  }

  const items: ClimateExpenditureTagSummary[] = [];
  for (const entry of itemsRaw) {
    if (!isRecord(entry)) {
      throw new ApiError("Invalid CCET catalog response: malformed item", 502, payload);
    }
    const row = parseClimateExpenditureTagRecord(entry);
    if (!row) {
      throw new ApiError("Invalid CCET catalog response: malformed item fields", 502, payload);
    }
    items.push(row);
  }

  return {
    items,
    totalCount: totalCountRaw,
    includeInactive: includeInactiveRaw
  };
}
