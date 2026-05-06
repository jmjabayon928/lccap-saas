import { ApiError } from "@/lib/api/api-error";
import type {
  MonitoringIndicatorDetail,
  MonitoringIndicatorSummary,
  MonitoringStatus,
  MonitoringUpdateSummary,
  SaveMonitoringIndicatorResult
} from "@/types/monitoring";

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

const ALLOWED_STATUSES: readonly MonitoringStatus[] = [
  "NotStarted",
  "InProgress",
  "OnTrack",
  "Delayed",
  "Completed"
];

function parseMonitoringStatus(raw: unknown): MonitoringStatus | null {
  if (typeof raw !== "string") {
    return null;
  }
  return ALLOWED_STATUSES.includes(raw as MonitoringStatus) ? (raw as MonitoringStatus) : null;
}

function parseOptionalNumberField(value: unknown): number | null {
  if (value === null || value === undefined) {
    return null;
  }
  if (isFiniteNumber(value)) {
    return value;
  }
  return null;
}

function parseProgressPercentField(value: unknown): number | null {
  if (value === null || value === undefined) {
    return null;
  }
  let n: number;
  if (isFiniteNumber(value)) {
    n = value;
  } else if (typeof value === "string" && value.trim()) {
    const parsed = Number(value.trim());
    if (!isFiniteNumber(parsed)) {
      return null;
    }
    n = parsed;
  } else {
    return null;
  }
  if (n < 0 || n > 100) {
    return null;
  }
  return n;
}

function sortNewestFirst(items: MonitoringIndicatorSummary[]): MonitoringIndicatorSummary[] {
  return [...items].sort((a, b) => {
    const ka = Date.parse(a.updatedAtUtc ?? a.lastUpdatedAtUtc ?? a.createdAtUtc ?? "") || 0;
    const kb = Date.parse(b.updatedAtUtc ?? b.lastUpdatedAtUtc ?? b.createdAtUtc ?? "") || 0;
    return kb - ka;
  });
}

function parseIndicatorRecord(raw: Record<string, unknown>): MonitoringIndicatorDetail {
  const idRaw = raw.id ?? raw.indicatorId;
  const planIdRaw = raw.planId;
  const actionItemIdRaw = raw.actionItemId;
  const nameRaw = raw.name ?? raw.title;
  const status = parseMonitoringStatus(raw.status);
  const description = raw.description;
  const unit = raw.unit;
  const baselineValue = parseOptionalNumberField(raw.baselineValue);
  const targetValue = parseOptionalNumberField(raw.targetValue);
  const currentValue = parseOptionalNumberField(raw.currentValue);
  const progressParsed = parseProgressPercentField(raw.progressPercent);
  const frequency = raw.frequency;
  const responsibleOffice = raw.responsibleOffice;
  const lastUpdatedAtUtc = optionalIsoOrNull(raw.lastUpdatedAtUtc);
  const createdAtUtc = optionalIsoOrNull(raw.createdAtUtc);
  const updatedAtUtc = optionalIsoOrNull(raw.updatedAtUtc);
  const rowVersionRaw = raw.rowVersion ?? raw.rowVersionBase64;

  if (!isNonEmptyString(idRaw)) {
    throw new ApiError("Invalid monitoring indicator: missing identifier", 502, raw);
  }
  if (!isNonEmptyString(planIdRaw)) {
    throw new ApiError("Invalid monitoring indicator: missing plan identifier", 502, raw);
  }
  if (!isNonEmptyString(nameRaw)) {
    throw new ApiError("Invalid monitoring indicator: missing name", 502, raw);
  }

  let rowVersion: string | null = null;
  if (typeof rowVersionRaw === "string" && rowVersionRaw.trim()) {
    rowVersion = rowVersionRaw.trim();
  }

  if (!status) {
    throw new ApiError("Invalid monitoring indicator: missing or invalid status", 502, raw);
  }

  const progressPresent =
    raw.progressPercent !== null &&
    raw.progressPercent !== undefined &&
    !(typeof raw.progressPercent === "string" && !raw.progressPercent.trim());
  if (progressPresent && progressParsed === null) {
    throw new ApiError("Invalid monitoring indicator: progressPercent must be between 0 and 100", 502, raw);
  }

  const descriptionOut = typeof description === "string" ? description : null;
  const unitOut = typeof unit === "string" ? unit : null;
  const frequencyOut = typeof frequency === "string" ? frequency : null;
  const responsibleOfficeOut =
    typeof responsibleOffice === "string" ? responsibleOffice : null;
  const actionItemIdOut =
    typeof actionItemIdRaw === "string" && isNonEmptyString(actionItemIdRaw) ? actionItemIdRaw : null;

  return {
    id: idRaw,
    planId: planIdRaw,
    actionItemId: actionItemIdOut,
    rowVersion,
    name: nameRaw,
    description: descriptionOut,
    unit: unitOut,
    baselineValue,
    targetValue,
    currentValue,
    progressPercent: progressParsed,
    status,
    frequency: frequencyOut,
    responsibleOffice: responsibleOfficeOut,
    lastUpdatedAtUtc,
    createdAtUtc,
    updatedAtUtc
  };
}

function unwrapIndicatorPayload(payload: unknown): Record<string, unknown> | null {
  if (!isRecord(payload)) {
    return null;
  }
  if (isRecord(payload.indicator)) {
    return payload.indicator;
  }
  return payload;
}

export function parseMonitoringIndicator(payload: unknown): MonitoringIndicatorDetail {
  const record = unwrapIndicatorPayload(payload);
  if (!record) {
    throw new ApiError("Invalid monitoring indicator response: expected object", 502, payload);
  }
  return parseIndicatorRecord(record);
}

export function parseMonitoringIndicatorsList(payload: unknown): MonitoringIndicatorSummary[] {
  let list: unknown[] = [];

  if (Array.isArray(payload)) {
    list = payload;
  } else if (isRecord(payload) && Array.isArray(payload.indicators)) {
    list = payload.indicators;
  } else if (isRecord(payload) && Array.isArray(payload.items)) {
    list = payload.items;
  } else {
    throw new ApiError(
      "Invalid monitoring indicators response: expected array, indicators, or items wrapper",
      502,
      payload
    );
  }

  const parsed: MonitoringIndicatorSummary[] = [];
  for (const item of list) {
    if (!isRecord(item)) {
      throw new ApiError("Invalid monitoring indicators response: malformed entry", 502, payload);
    }
    parsed.push(parseIndicatorRecord(item));
  }

  return sortNewestFirst(parsed);
}

export function parseSaveMonitoringIndicatorResult(payload: unknown): SaveMonitoringIndicatorResult {
  if (!isRecord(payload)) {
    throw new ApiError("Invalid monitoring indicator response: expected object", 502, payload);
  }

  if (isNonEmptyString(payload.id) && isNonEmptyString(payload.planId)) {
    try {
      return parseMonitoringIndicator(payload);
    } catch {
      // fall through to compact handling
    }
  }

  const id = payload.id;
  if (isNonEmptyString(id)) {
    return {
      id,
      rowVersion: typeof payload.rowVersion === "string" ? payload.rowVersion : undefined
    };
  }

  throw new ApiError("Invalid monitoring indicator response: malformed fields", 502, payload);
}

function parseMonitoringUpdateRecord(raw: Record<string, unknown>): MonitoringUpdateSummary {
  const idRaw = raw.id;
  const monitoringIndicatorIdRaw = raw.monitoringIndicatorId;
  const periodLabelRaw = raw.periodLabel;
  const status = parseMonitoringStatus(raw.status);
  const actualValue = parseOptionalNumberField(raw.actualValue);
  const progressParsed = parseProgressPercentField(raw.progressPercent);
  const notesRaw = raw.notes;
  const reportedAtUtcRaw = raw.reportedAtUtc;
  const reportedByUserIdRaw = raw.reportedByUserId;
  const createdAtUtcRaw = raw.createdAtUtc;
  const createdByUserIdRaw = raw.createdByUserId;
  const rowVersionRaw = raw.rowVersion ?? raw.rowVersionBase64;

  if (!isNonEmptyString(idRaw)) {
    throw new ApiError("Invalid monitoring update: missing identifier", 502, raw);
  }
  if (!isNonEmptyString(monitoringIndicatorIdRaw)) {
    throw new ApiError("Invalid monitoring update: missing indicator identifier", 502, raw);
  }
  if (!isNonEmptyString(periodLabelRaw)) {
    throw new ApiError("Invalid monitoring update: missing period label", 502, raw);
  }
  if (!status) {
    throw new ApiError("Invalid monitoring update: missing or invalid status", 502, raw);
  }
  if (typeof rowVersionRaw !== "string" || !rowVersionRaw.trim()) {
    throw new ApiError("Invalid monitoring update: missing rowVersion", 502, raw);
  }
  if (typeof reportedAtUtcRaw !== "string" || !reportedAtUtcRaw.trim()) {
    throw new ApiError("Invalid monitoring update: missing reportedAtUtc", 502, raw);
  }
  if (typeof createdAtUtcRaw !== "string" || !createdAtUtcRaw.trim()) {
    throw new ApiError("Invalid monitoring update: missing createdAtUtc", 502, raw);
  }

  const progressPresent =
    raw.progressPercent !== null &&
    raw.progressPercent !== undefined &&
    !(typeof raw.progressPercent === "string" && !raw.progressPercent.trim());
  if (progressPresent && progressParsed === null) {
    throw new ApiError("Invalid monitoring update: progressPercent must be between 0 and 100", 502, raw);
  }

  const notes = typeof notesRaw === "string" ? notesRaw : null;
  const reportedByUserId =
    typeof reportedByUserIdRaw === "string" && isNonEmptyString(reportedByUserIdRaw)
      ? reportedByUserIdRaw
      : null;
  const createdByUserId =
    typeof createdByUserIdRaw === "string" && isNonEmptyString(createdByUserIdRaw)
      ? createdByUserIdRaw
      : null;

  return {
    id: idRaw,
    monitoringIndicatorId: monitoringIndicatorIdRaw,
    periodLabel: periodLabelRaw,
    actualValue,
    progressPercent: progressParsed,
    status,
    notes,
    reportedAtUtc: reportedAtUtcRaw,
    reportedByUserId,
    createdAtUtc: createdAtUtcRaw,
    createdByUserId,
    rowVersion: rowVersionRaw.trim()
  };
}

export function parseMonitoringUpdate(payload: unknown): MonitoringUpdateSummary {
  if (!isRecord(payload)) {
    throw new ApiError("Invalid monitoring update response: expected object", 502, payload);
  }
  return parseMonitoringUpdateRecord(payload);
}

export function parseMonitoringUpdatesList(payload: unknown): MonitoringUpdateSummary[] {
  if (!Array.isArray(payload)) {
    throw new ApiError("Invalid monitoring updates response: expected array", 502, payload);
  }

  const parsed: MonitoringUpdateSummary[] = [];
  for (const item of payload) {
    if (!isRecord(item)) {
      throw new ApiError("Invalid monitoring updates response: malformed entry", 502, payload);
    }
    parsed.push(parseMonitoringUpdateRecord(item));
  }
  return parsed;
}
