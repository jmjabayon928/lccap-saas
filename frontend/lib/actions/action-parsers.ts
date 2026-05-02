import { ApiError } from "@/lib/api/api-error";
import type {
  ActionItemDetail,
  ActionItemSummary,
  ActionStatus,
  ActionType,
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

  if (!isNonEmptyString(idRaw) || !isNonEmptyString(planId) || !isNonEmptyString(title) || !isNonEmptyString(sector)) {
    return null;
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

  if (priorityScore !== null && priorityScore < 0) {
    return null;
  }

  return {
    id: idRaw,
    planId,
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
