import { ApiError } from "@/lib/api/api-error";
import type {
  ActionFundingAllocationStatus,
  ActionFundingAllocationSummary,
  ActionFundingAllocationsResult,
  ActionItemDetail,
  ActionItemSummary,
  ActionStatus,
  ActionType,
  ClimateExpenditureTagCategory,
  ClimateExpenditureTagSummary,
  ClimateExpenditureTagsResult,
  FundingProgramStatus,
  FundingProgramSummary,
  FundingProgramsResult,
  FundingSourceSummary,
  FundingSourcesResult,
  FundingSourceType,
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

const FUNDING_SOURCE_TYPES: ReadonlySet<FundingSourceType> = new Set([
  "LGUInternal",
  "NationalGovernment",
  "ProvincialGovernment",
  "Donor",
  "NGO",
  "PrivateSector",
  "BankLoan",
  "ClimateFund",
  "Other"
]);

const FUNDING_PROGRAM_STATUSES: ReadonlySet<FundingProgramStatus> = new Set([
  "Draft",
  "Active",
  "Closed",
  "Archived"
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

function parseFundingSourceType(raw: unknown): FundingSourceType {
  if (typeof raw === "string" && FUNDING_SOURCE_TYPES.has(raw as FundingSourceType)) {
    return raw as FundingSourceType;
  }
  return "Other";
}

function parseFundingProgramStatus(raw: unknown): FundingProgramStatus {
  if (typeof raw === "string" && FUNDING_PROGRAM_STATUSES.has(raw as FundingProgramStatus)) {
    return raw as FundingProgramStatus;
  }
  return "Archived";
}

function parseActionFundingAllocationStatus(raw: unknown): ActionFundingAllocationStatus {
  if (raw === "Planned") {
    return "Planned";
  }
  return "Planned";
}

function trimmedNullableId(value: unknown): string | null {
  const s = nullableString(value);
  if (!s?.trim()) {
    return null;
  }
  return s.trim();
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

function parseFundingSourceSummaryRecord(raw: Record<string, unknown>): FundingSourceSummary | null {
  const idRaw = raw.id;
  const nameRaw = raw.name;
  const sourceTypeRaw = raw.sourceType;

  if (!isNonEmptyString(idRaw) || !isNonEmptyString(nameRaw)) {
    return null;
  }

  return {
    id: idRaw.trim(),
    name: nameRaw.trim(),
    sourceType: parseFundingSourceType(sourceTypeRaw),
    description: nullableString(raw.description),
    contactName: nullableString(raw.contactName),
    contactEmail: nullableString(raw.contactEmail),
    websiteUrl: nullableString(raw.websiteUrl),
    createdAtUtc: nullableString(raw.createdAtUtc)
  };
}

export function parseFundingSourceSummary(payload: unknown): FundingSourceSummary {
  if (!isRecord(payload)) {
    throw new ApiError("Invalid funding source response: expected object", 502, payload);
  }
  const parsed = parseFundingSourceSummaryRecord(payload);
  if (!parsed) {
    throw new ApiError("Invalid funding source response: malformed fields", 502, payload);
  }
  return parsed;
}

export function parseFundingSourcesResult(payload: unknown): FundingSourcesResult {
  if (!isRecord(payload)) {
    throw new ApiError("Invalid funding sources response: expected object", 502, payload);
  }

  const itemsRaw = payload.items;
  if (!Array.isArray(itemsRaw)) {
    throw new ApiError("Invalid funding sources response: items must be an array", 502, payload);
  }

  const totalCountRaw = payload.totalCount;
  if (!isFiniteNumber(totalCountRaw) || totalCountRaw < 0 || !Number.isInteger(totalCountRaw)) {
    throw new ApiError("Invalid funding sources response: totalCount invalid", 502, payload);
  }

  const items: FundingSourceSummary[] = [];
  for (const entry of itemsRaw) {
    if (!isRecord(entry)) {
      throw new ApiError("Invalid funding sources response: malformed item", 502, payload);
    }
    const row = parseFundingSourceSummaryRecord(entry);
    if (!row) {
      throw new ApiError("Invalid funding sources response: malformed item fields", 502, payload);
    }
    items.push(row);
  }

  return { items, totalCount: totalCountRaw };
}

function parseFundingProgramSummaryRecord(raw: Record<string, unknown>): FundingProgramSummary | null {
  const idRaw = raw.id;
  const fundingSourceIdRaw = raw.fundingSourceId;
  const fundingSourceNameRaw = raw.fundingSourceName;
  const nameRaw = raw.name;
  const currencyCodeRaw = raw.currencyCode;
  const statusRaw = raw.status;

  if (
    !isNonEmptyString(idRaw)
    || !isNonEmptyString(fundingSourceIdRaw)
    || !isNonEmptyString(fundingSourceNameRaw)
    || !isNonEmptyString(nameRaw)
    || typeof currencyCodeRaw !== "string"
    || currencyCodeRaw.trim().length === 0
  ) {
    return null;
  }

  const cur = currencyCodeRaw.trim().toUpperCase();
  if (!/^[A-Z]{3}$/.test(cur)) {
    return null;
  }

  const maxAward = optionalNumberOrNull(raw.maxAwardAmount);
  if (
    maxAward !== null
    && (!Number.isFinite(maxAward) || maxAward < 0)
  ) {
    return null;
  }

  return {
    id: idRaw.trim(),
    fundingSourceId: fundingSourceIdRaw.trim(),
    fundingSourceName: fundingSourceNameRaw.trim(),
    name: nameRaw.trim(),
    programCode: trimmedNullableId(raw.programCode),
    description: nullableString(raw.description),
    eligibleUses: nullableString(raw.eligibleUses),
    applicationUrl: nullableString(raw.applicationUrl),
    opensAtUtc: nullableString(raw.opensAtUtc),
    closesAtUtc: nullableString(raw.closesAtUtc),
    maxAwardAmount: maxAward,
    currencyCode: cur,
    status: parseFundingProgramStatus(statusRaw),
    createdAtUtc: nullableString(raw.createdAtUtc)
  };
}

export function parseFundingProgramSummary(payload: unknown): FundingProgramSummary {
  if (!isRecord(payload)) {
    throw new ApiError("Invalid funding program response: expected object", 502, payload);
  }
  const parsed = parseFundingProgramSummaryRecord(payload);
  if (!parsed) {
    throw new ApiError("Invalid funding program response: malformed fields", 502, payload);
  }
  return parsed;
}

export function parseFundingProgramsResult(payload: unknown): FundingProgramsResult {
  if (!isRecord(payload)) {
    throw new ApiError("Invalid funding programs response: expected object", 502, payload);
  }

  const itemsRaw = payload.items;
  if (!Array.isArray(itemsRaw)) {
    throw new ApiError("Invalid funding programs response: items must be an array", 502, payload);
  }

  const totalCountRaw = payload.totalCount;
  if (!isFiniteNumber(totalCountRaw) || totalCountRaw < 0 || !Number.isInteger(totalCountRaw)) {
    throw new ApiError("Invalid funding programs response: totalCount invalid", 502, payload);
  }

  const fundingSourceId = trimmedNullableId(payload.fundingSourceId);
  const includeRaw = payload.includeInactiveOrClosed;
  if (typeof includeRaw !== "boolean") {
    throw new ApiError("Invalid funding programs response: includeInactiveOrClosed invalid", 502, payload);
  }

  const items: FundingProgramSummary[] = [];
  for (const entry of itemsRaw) {
    if (!isRecord(entry)) {
      throw new ApiError("Invalid funding programs response: malformed item", 502, payload);
    }
    const row = parseFundingProgramSummaryRecord(entry);
    if (!row) {
      throw new ApiError("Invalid funding programs response: malformed item fields", 502, payload);
    }
    items.push(row);
  }

  return {
    items,
    totalCount: totalCountRaw,
    fundingSourceId,
    includeInactiveOrClosed: includeRaw
  };
}

function parseActionFundingAllocationRecord(raw: Record<string, unknown>): ActionFundingAllocationSummary | null {
  const idRaw = raw.id;
  const planId = raw.planId;
  const actionItemId = raw.actionItemId;
  const actionTitle = raw.actionTitle;
  const fundingSourceId = raw.fundingSourceId;
  const fundingSourceName = raw.fundingSourceName;
  const fiscalYearRaw = raw.fiscalYear;
  const allocatedRaw = raw.allocatedAmount;
  const currencyCode = raw.currencyCode;
  const allocationStatusRaw = raw.allocationStatus;
  const createdAtUtc = nullableString(raw.createdAtUtc);

  if (
    !isNonEmptyString(idRaw)
    || !isNonEmptyString(planId)
    || !isNonEmptyString(actionItemId)
    || !isNonEmptyString(actionTitle)
    || !isNonEmptyString(fundingSourceId)
    || !isNonEmptyString(fundingSourceName)
    || typeof currencyCode !== "string"
    || currencyCode.trim().length === 0
  ) {
    return null;
  }

  if (!isFiniteNumber(fiscalYearRaw) || !Number.isInteger(fiscalYearRaw)) {
    return null;
  }

  if (!isFiniteNumber(allocatedRaw) || allocatedRaw < 0) {
    return null;
  }

  const fundingProgramId = trimmedNullableId(raw.fundingProgramId);
  const fundingProgramName = trimmedNullableId(raw.fundingProgramName);

  const climateExpenditureTagId = trimmedNullableId(raw.climateExpenditureTagId);
  const climateExpenditureTagCode = trimmedNullableId(raw.climateExpenditureTagCode);
  const climateExpenditureTagName = trimmedNullableId(raw.climateExpenditureTagName);
  const climateExpenditureTagCategory = trimmedNullableId(raw.climateExpenditureTagCategory);

  return {
    id: idRaw.trim(),
    planId: planId.trim(),
    actionItemId: actionItemId.trim(),
    actionTitle: actionTitle.trim(),
    fundingSourceId: fundingSourceId.trim(),
    fundingSourceName: fundingSourceName.trim(),
    fundingProgramId,
    fundingProgramName,
    climateExpenditureTagId,
    climateExpenditureTagCode,
    climateExpenditureTagName,
    climateExpenditureTagCategory,
    fiscalYear: fiscalYearRaw,
    allocatedAmount: allocatedRaw,
    currencyCode: currencyCode.trim().toUpperCase(),
    allocationStatus: parseActionFundingAllocationStatus(allocationStatusRaw),
    notes: trimmedNullableId(raw.notes),
    createdAtUtc
  };
}

export function parseActionFundingAllocationSummary(payload: unknown): ActionFundingAllocationSummary {
  if (!isRecord(payload)) {
    throw new ApiError("Invalid funding allocation response: expected object", 502, payload);
  }
  const parsed = parseActionFundingAllocationRecord(payload);
  if (!parsed) {
    throw new ApiError("Invalid funding allocation response: malformed fields", 502, payload);
  }
  return parsed;
}

export function parseActionFundingAllocationsResult(payload: unknown): ActionFundingAllocationsResult {
  if (!isRecord(payload)) {
    throw new ApiError("Invalid funding allocations response: expected object", 502, payload);
  }

  const itemsRaw = payload.items;
  if (!Array.isArray(itemsRaw)) {
    throw new ApiError("Invalid funding allocations response: items must be an array", 502, payload);
  }

  const items: ActionFundingAllocationSummary[] = [];
  for (const entry of itemsRaw) {
    if (!isRecord(entry)) {
      throw new ApiError("Invalid funding allocations response: malformed item", 502, payload);
    }
    const row = parseActionFundingAllocationRecord(entry);
    if (!row) {
      throw new ApiError("Invalid funding allocations response: malformed item fields", 502, payload);
    }
    items.push(row);
  }

  const totalRaw = payload.totalCount;
  const totalCount =
    isFiniteNumber(totalRaw) && totalRaw >= 0 && Number.isInteger(totalRaw) ? totalRaw : items.length;

  return {
    items,
    totalCount
  };
}
