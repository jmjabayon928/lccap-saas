import { ApiError } from "@/lib/api/api-error";
import type {
  ActionDashboardSummary,
  BarangaySummary,
  CreatedGeoJsonMapAssetSummary,
  CriticalFacilitySummary,
  CreatePlanResult,
  CreateSectionCommentRequest,
  EvidenceDashboardSummary,
  ExportReadinessDashboardSummary,
  FundingCurrencyTotal,
  FundingDashboardSummary,
  GeoJsonLayerFeatureSummary,
  MapAssetSummary,
  MapFormat,
  MapType,
  MonitoringDashboardSummary,
  PlanActivityItem,
  PlanMapWorkspaceCounts,
  PlanMapWorkspaceResult,
  PlanOperationalDashboard,
  PlanSectionHistoryEntry,
  PlanSectionSummary,
  CollaborationSummaryResult,
  CollaborationGroupSummary,
  CollaborationMemberSummary,
  PlanStatus,
  PlanSummary,
  NotificationEventType,
  MyNotificationSummary,
  MyNotificationsResult,
  ReviewDashboardSummary,
  SectionCommentSummary,
  SectionCommentType,
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

  let rowVersion: string | null = null;
  if (typeof record.rowVersion === "string" && record.rowVersion.trim()) {
    rowVersion = record.rowVersion.trim();
  }

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
    updatedAtUtc,
    rowVersion
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

export function parsePlanSectionHistoryEntry(payload: unknown): PlanSectionHistoryEntry {
  if (!isRecord(payload)) {
    throw new ApiError("Invalid history entry: expected object", 502, payload);
  }

  const auditLogId = payload.auditLogId;
  const sectionId = payload.sectionId;
  const planId = payload.planId;
  const sectionKey = payload.sectionKey;
  const action = payload.action;
  const title = payload.title;
  const content = payload.content;
  const createdAtUtc = payload.createdAtUtc;
  const userId = payload.userId;
  const canRestore = payload.canRestore;

  if (
    !isNonEmptyString(auditLogId) ||
    !isNonEmptyString(sectionId) ||
    !isNonEmptyString(planId) ||
    !isNonEmptyString(sectionKey) ||
    (action !== "PlanSectionUpdated" && action !== "PlanSectionRestored") ||
    typeof title !== "string" ||
    typeof content !== "string" ||
    !isNonEmptyString(createdAtUtc) ||
    typeof canRestore !== "boolean"
  ) {
    throw new ApiError("Invalid history entry: malformed fields", 502, payload);
  }

  return {
    auditLogId,
    sectionId,
    planId,
    sectionKey,
    action: action as "PlanSectionUpdated" | "PlanSectionRestored",
    title,
    content,
    createdAtUtc,
    userId: typeof userId === "string" ? userId : null,
    canRestore
  };
}

export function parsePlanSectionHistoryList(payload: unknown): PlanSectionHistoryEntry[] {
  let list: unknown[] = [];

  if (isRecord(payload) && Array.isArray(payload.history)) {
    list = payload.history;
  } else if (Array.isArray(payload)) {
    list = payload;
  } else {
    throw new ApiError("Invalid history list response: expected array or history wrapper", 502, payload);
  }

  return list.map((item, i) => {
    try {
      return parsePlanSectionHistoryEntry(item);
    } catch {
      throw new ApiError(`Invalid history list: entry ${i} is malformed`, 502, payload);
    }
  });
}

const ALLOWED_SECTION_COMMENT_TYPES = new Set<string>([
  "General",
  "DataGap",
  "Validation",
  "RevisionRequest"
]);

function isAllowedSectionCommentType(value: string): value is SectionCommentType {
  return ALLOWED_SECTION_COMMENT_TYPES.has(value);
}

function optionalBase64OrNull(value: unknown): string | null {
  if (value === null || value === undefined) {
    return null;
  }
  if (typeof value === "string" && value.trim()) {
    return value.trim();
  }
  return null;
}

export function parseSectionCommentSummary(payload: unknown): SectionCommentSummary {
  if (!isRecord(payload)) {
    throw new ApiError("Invalid section comment: expected object", 502, payload);
  }

  const id = payload.id;
  const planId = payload.planId;
  const sectionKey = payload.sectionKey;
  const commentType = payload.commentType;
  const commentText = payload.commentText;
  const createdByUserId = payload.createdByUserId;
  const createdAtUtc = payload.createdAtUtc;
  const isResolved = payload.isResolved;
  const resolvedAtUtc = optionalIsoOrNull(payload.resolvedAtUtc);
  const resolvedByUserId = payload.resolvedByUserId;
  const updatedAtUtc = optionalIsoOrNull(payload.updatedAtUtc);
  const rowVersion = optionalBase64OrNull(payload.rowVersion);

  if (
    !isNonEmptyString(id) ||
    !isNonEmptyString(planId) ||
    !isNonEmptyString(sectionKey) ||
    !isNonEmptyString(commentType) ||
    !isAllowedSectionCommentType(commentType) ||
    typeof commentText !== "string" ||
    !isNonEmptyString(createdByUserId) ||
    !isNonEmptyString(createdAtUtc) ||
    typeof isResolved !== "boolean"
  ) {
    throw new ApiError("Invalid section comment: malformed fields", 502, payload);
  }

  return {
    id,
    planId,
    sectionKey,
    commentType,
    commentText,
    createdByUserId,
    createdAtUtc,
    isResolved,
    resolvedAtUtc,
    resolvedByUserId: typeof resolvedByUserId === "string" ? resolvedByUserId : null,
    updatedAtUtc,
    rowVersion
  };
}

export function parseSectionCommentsResponse(payload: unknown): SectionCommentSummary[] {
  let list: unknown[] = [];

  if (Array.isArray(payload)) {
    list = payload;
  } else if (isRecord(payload) && Array.isArray(payload.comments)) {
    list = payload.comments;
  } else {
    throw new ApiError("Invalid section comments response: expected array or comments wrapper", 502, payload);
  }

  return list.map((item, i) => {
    try {
      return parseSectionCommentSummary(item);
    } catch {
      throw new ApiError(`Invalid section comments response: entry ${i} is malformed`, 502, payload);
    }
  });
}

export function normalizeCreateSectionCommentRequest(
  request: CreateSectionCommentRequest
): CreateSectionCommentRequest {
  return {
    commentType: request.commentType,
    commentText: request.commentText
  };
}

function parseFundingCurrencyTotal(payload: unknown, index: number, parent: unknown): FundingCurrencyTotal {
  if (!isRecord(payload)) {
    throw new ApiError(`Invalid dashboard: currency total ${index} is not an object`, 502, parent);
  }
  const currencyCode = payload.currencyCode;
  const totalAllocatedAmount = payload.totalAllocatedAmount;
  if (!isNonEmptyString(currencyCode)) {
    throw new ApiError(`Invalid dashboard: currency total ${index} missing currencyCode`, 502, parent);
  }
  if (!isFiniteNumber(totalAllocatedAmount)) {
    throw new ApiError(`Invalid dashboard: currency total ${index} missing totalAllocatedAmount`, 502, parent);
  }
  return { currencyCode, totalAllocatedAmount };
}

function parseEvidenceDashboardSummary(record: Record<string, unknown>, parent: unknown): EvidenceDashboardSummary {
  const totalDocuments = record.totalDocuments;
  const officialEvidenceCount = record.officialEvidenceCount;
  const publicEvidenceCount = record.publicEvidenceCount;
  const draftEvidenceCount = record.draftEvidenceCount;
  const internalEvidenceCount = record.internalEvidenceCount;
  const linkedToSectionCount = record.linkedToSectionCount;
  const linkedToActionCount = record.linkedToActionCount;
  if (
    !isFiniteNumber(totalDocuments) ||
    !isFiniteNumber(officialEvidenceCount) ||
    !isFiniteNumber(publicEvidenceCount) ||
    !isFiniteNumber(draftEvidenceCount) ||
    !isFiniteNumber(internalEvidenceCount) ||
    !isFiniteNumber(linkedToSectionCount) ||
    !isFiniteNumber(linkedToActionCount)
  ) {
    throw new ApiError("Invalid dashboard: malformed evidence summary", 502, parent);
  }
  return {
    totalDocuments,
    officialEvidenceCount,
    publicEvidenceCount,
    draftEvidenceCount,
    internalEvidenceCount,
    linkedToSectionCount,
    linkedToActionCount
  };
}

function parseActionDashboardSummary(record: Record<string, unknown>, parent: unknown): ActionDashboardSummary {
  const fields = [
    "totalActions",
    "plannedCount",
    "inProgressCount",
    "onTrackCount",
    "delayedCount",
    "completedCount",
    "cancelledCount",
    "actionsWithBudgetCount",
    "actionsWithFundingSourceCount",
    "missingFundingSourceCount"
  ] as const;
  const nums: Record<string, number> = {};
  for (const k of fields) {
    const v = record[k];
    if (!isFiniteNumber(v)) {
      throw new ApiError(`Invalid dashboard: action summary missing ${k}`, 502, parent);
    }
    nums[k] = v;
  }
  return {
    totalActions: nums.totalActions,
    plannedCount: nums.plannedCount,
    inProgressCount: nums.inProgressCount,
    onTrackCount: nums.onTrackCount,
    delayedCount: nums.delayedCount,
    completedCount: nums.completedCount,
    cancelledCount: nums.cancelledCount,
    actionsWithBudgetCount: nums.actionsWithBudgetCount,
    actionsWithFundingSourceCount: nums.actionsWithFundingSourceCount,
    missingFundingSourceCount: nums.missingFundingSourceCount
  };
}

function parseMonitoringDashboardSummary(
  record: Record<string, unknown>,
  parent: unknown
): MonitoringDashboardSummary {
  const totalIndicators = record.totalIndicators;
  const notStartedCount = record.notStartedCount;
  const inProgressCount = record.inProgressCount;
  const onTrackCount = record.onTrackCount;
  const delayedCount = record.delayedCount;
  const completedCount = record.completedCount;
  const totalMonitoringUpdates = record.totalMonitoringUpdates;
  const indicatorsWithUpdatesCount = record.indicatorsWithUpdatesCount;
  const latestRaw = record.latestMonitoringUpdateAtUtc;
  if (
    !isFiniteNumber(totalIndicators) ||
    !isFiniteNumber(notStartedCount) ||
    !isFiniteNumber(inProgressCount) ||
    !isFiniteNumber(onTrackCount) ||
    !isFiniteNumber(delayedCount) ||
    !isFiniteNumber(completedCount) ||
    !isFiniteNumber(totalMonitoringUpdates) ||
    !isFiniteNumber(indicatorsWithUpdatesCount)
  ) {
    throw new ApiError("Invalid dashboard: malformed monitoring summary", 502, parent);
  }
  const latestMonitoringUpdateAtUtc =
    latestRaw === null || latestRaw === undefined
      ? null
      : typeof latestRaw === "string" && latestRaw.trim()
        ? latestRaw
        : null;
  return {
    totalIndicators,
    notStartedCount,
    inProgressCount,
    onTrackCount,
    delayedCount,
    completedCount,
    totalMonitoringUpdates,
    indicatorsWithUpdatesCount,
    latestMonitoringUpdateAtUtc
  };
}

function parseReviewDashboardSummary(record: Record<string, unknown>, parent: unknown): ReviewDashboardSummary {
  const totalComments = record.totalComments;
  const unresolvedComments = record.unresolvedComments;
  const resolvedComments = record.resolvedComments;
  const dataGapComments = record.dataGapComments;
  const validationComments = record.validationComments;
  const revisionRequestComments = record.revisionRequestComments;
  if (
    !isFiniteNumber(totalComments) ||
    !isFiniteNumber(unresolvedComments) ||
    !isFiniteNumber(resolvedComments) ||
    !isFiniteNumber(dataGapComments) ||
    !isFiniteNumber(validationComments) ||
    !isFiniteNumber(revisionRequestComments)
  ) {
    throw new ApiError("Invalid dashboard: malformed review summary", 502, parent);
  }
  return {
    totalComments,
    unresolvedComments,
    resolvedComments,
    dataGapComments,
    validationComments,
    revisionRequestComments
  };
}

function parseFundingDashboardSummary(record: Record<string, unknown>, parent: unknown): FundingDashboardSummary {
  const totalAllocations = record.totalAllocations;
  const ccetTaggedAllocations = record.ccetTaggedAllocations;
  const untaggedAllocations = record.untaggedAllocations;
  const allocationTotalsByCurrency = record.allocationTotalsByCurrency;
  if (
    !isFiniteNumber(totalAllocations) ||
    !isFiniteNumber(ccetTaggedAllocations) ||
    !isFiniteNumber(untaggedAllocations) ||
    !Array.isArray(allocationTotalsByCurrency)
  ) {
    throw new ApiError("Invalid dashboard: malformed funding summary", 502, parent);
  }
  const totals = allocationTotalsByCurrency.map((x, i) => parseFundingCurrencyTotal(x, i, parent));
  return {
    totalAllocations,
    ccetTaggedAllocations,
    untaggedAllocations,
    allocationTotalsByCurrency: totals
  };
}

function parseExportReadinessDashboardSummary(
  record: Record<string, unknown>,
  parent: unknown
): ExportReadinessDashboardSummary {
  const hasOfficialEvidence = record.hasOfficialEvidence;
  const hasActions = record.hasActions;
  const hasMonitoring = record.hasMonitoring;
  const hasFundingAllocations = record.hasFundingAllocations;
  const hasUnresolvedComments = record.hasUnresolvedComments;
  const suggestedNextSteps = record.suggestedNextSteps;
  if (
    typeof hasOfficialEvidence !== "boolean" ||
    typeof hasActions !== "boolean" ||
    typeof hasMonitoring !== "boolean" ||
    typeof hasFundingAllocations !== "boolean" ||
    typeof hasUnresolvedComments !== "boolean" ||
    !Array.isArray(suggestedNextSteps) ||
    !suggestedNextSteps.every((s) => typeof s === "string")
  ) {
    throw new ApiError("Invalid dashboard: malformed export readiness", 502, parent);
  }
  return {
    hasOfficialEvidence,
    hasActions,
    hasMonitoring,
    hasFundingAllocations,
    hasUnresolvedComments,
    suggestedNextSteps: suggestedNextSteps as string[]
  };
}

function parsePlanActivityItem(payload: unknown, index: number, parent: unknown): PlanActivityItem {
  if (!isRecord(payload)) {
    throw new ApiError(`Invalid dashboard: activity ${index} is not an object`, 502, parent);
  }
  const id = payload.id;
  const action = payload.action;
  const entityType = payload.entityType;
  const entityId = payload.entityId;
  const createdAtUtc = payload.createdAtUtc;
  const summary = payload.summary;
  if (!isNonEmptyString(id)) {
    throw new ApiError(`Invalid dashboard: activity ${index} missing id`, 502, parent);
  }
  if (typeof action !== "string" || !action.trim()) {
    throw new ApiError(`Invalid dashboard: activity ${index} missing action`, 502, parent);
  }
  if (!isNonEmptyString(entityType)) {
    throw new ApiError(`Invalid dashboard: activity ${index} missing entityType`, 502, parent);
  }
  if (entityId !== null && typeof entityId !== "string") {
    throw new ApiError(`Invalid dashboard: activity ${index} invalid entityId`, 502, parent);
  }
  if (!isNonEmptyString(createdAtUtc)) {
    throw new ApiError(`Invalid dashboard: activity ${index} missing createdAtUtc`, 502, parent);
  }
  if (typeof summary !== "string") {
    throw new ApiError(`Invalid dashboard: activity ${index} missing summary`, 502, parent);
  }
  return {
    id,
    action,
    entityType,
    entityId: entityId === null ? null : entityId,
    createdAtUtc,
    summary
  };
}

export function parsePlanOperationalDashboard(payload: unknown): PlanOperationalDashboard {
  if (!isRecord(payload)) {
    throw new ApiError("Invalid dashboard response: expected object", 502, payload);
  }

  const planId = payload.planId;
  const planTitle = payload.planTitle;
  const planningPeriodStart = payload.planningPeriodStart;
  const planningPeriodEnd = payload.planningPeriodEnd;
  const status = payload.status;
  const generatedAtUtc = payload.generatedAtUtc;
  const evidence = payload.evidence;
  const actions = payload.actions;
  const monitoring = payload.monitoring;
  const review = payload.review;
  const funding = payload.funding;
  const exportReadiness = payload.exportReadiness;
  const recentActivity = payload.recentActivity;

  if (!isNonEmptyString(planId)) {
    throw new ApiError("Invalid dashboard: missing planId", 502, payload);
  }
  if (!isNonEmptyString(planTitle)) {
    throw new ApiError("Invalid dashboard: missing planTitle", 502, payload);
  }
  if (!isFiniteNumber(planningPeriodStart) || !isFiniteNumber(planningPeriodEnd)) {
    throw new ApiError("Invalid dashboard: invalid planning period", 502, payload);
  }
  if (typeof status !== "string" || !status.trim()) {
    throw new ApiError("Invalid dashboard: missing status", 502, payload);
  }
  if (!isNonEmptyString(generatedAtUtc)) {
    throw new ApiError("Invalid dashboard: missing generatedAtUtc", 502, payload);
  }
  if (!isRecord(evidence)) {
    throw new ApiError("Invalid dashboard: missing evidence", 502, payload);
  }
  if (!isRecord(actions)) {
    throw new ApiError("Invalid dashboard: missing actions", 502, payload);
  }
  if (!isRecord(monitoring)) {
    throw new ApiError("Invalid dashboard: missing monitoring", 502, payload);
  }
  if (!isRecord(review)) {
    throw new ApiError("Invalid dashboard: missing review", 502, payload);
  }
  if (!isRecord(funding)) {
    throw new ApiError("Invalid dashboard: missing funding", 502, payload);
  }
  if (!isRecord(exportReadiness)) {
    throw new ApiError("Invalid dashboard: missing exportReadiness", 502, payload);
  }
  if (!Array.isArray(recentActivity)) {
    throw new ApiError("Invalid dashboard: missing recentActivity", 502, payload);
  }

  return {
    planId,
    planTitle,
    planningPeriodStart,
    planningPeriodEnd,
    status: status.trim(),
    generatedAtUtc,
    evidence: parseEvidenceDashboardSummary(evidence, payload),
    actions: parseActionDashboardSummary(actions, payload),
    monitoring: parseMonitoringDashboardSummary(monitoring, payload),
    review: parseReviewDashboardSummary(review, payload),
    funding: parseFundingDashboardSummary(funding, payload),
    exportReadiness: parseExportReadinessDashboardSummary(exportReadiness, payload),
    recentActivity: recentActivity.map((item, i) => parsePlanActivityItem(item, i, payload))
  };
}

function parseNullableNumber(value: unknown): number | null {
  if (value === null || value === undefined) {
    return null;
  }
  if (typeof value === "number" && Number.isFinite(value)) {
    return value;
  }
  return null;
}

function parseUnknownJsonValue(value: unknown): unknown {
  return value;
}

export function parseBarangaySummary(raw: Record<string, unknown>, parent: unknown, index?: number): BarangaySummary {
  const id = raw.id;
  const name = raw.name;
  const ix = index != null ? ` entry ${index}` : "";
  if (!isNonEmptyString(id) || !isNonEmptyString(name)) {
    throw new ApiError(`Invalid map workspace: barangay${ix}`, 502, parent);
  }

  let code: string | null = null;
  const codeRaw = raw.code;
  if (codeRaw === null || codeRaw === undefined) {
    code = null;
  } else if (typeof codeRaw === "string") {
    code = codeRaw;
  } else {
    throw new ApiError(`Invalid map workspace: barangay code${ix}`, 502, parent);
  }

  const classificationRaw = raw.classification;
  let classification: string | null = null;
  if (classificationRaw === null || classificationRaw === undefined) {
    classification = null;
  } else if (typeof classificationRaw === "string") {
    classification = classificationRaw;
  } else {
    throw new ApiError(`Invalid map workspace: barangay classification${ix}`, 502, parent);
  }

  return {
    id,
    name,
    code,
    latitude: parseNullableNumber(raw.latitude),
    longitude: parseNullableNumber(raw.longitude),
    landAreaHectares: parseNullableNumber(raw.landAreaHectares),
    population: parseNullableNumber(raw.population),
    households: parseNullableNumber(raw.households),
    classification
  };
}

export function parseCriticalFacilitySummary(
  raw: Record<string, unknown>,
  parent: unknown,
  index?: number
): CriticalFacilitySummary {
  const id = raw.id;
  const name = raw.name;
  const facilityType = raw.facilityType;
  const isEvacuationSite = raw.isEvacuationSite;
  const ix = index != null ? ` entry ${index}` : "";
  if (!isNonEmptyString(id) || !isNonEmptyString(name) || typeof facilityType !== "string" || !facilityType.trim()) {
    throw new ApiError(`Invalid map workspace: facility${ix}`, 502, parent);
  }
  if (typeof isEvacuationSite !== "boolean") {
    throw new ApiError(`Invalid map workspace: facility evacuation flag${ix}`, 502, parent);
  }

  let barangayId: string | null = null;
  const bId = raw.barangayId;
  if (bId === null || bId === undefined) {
    barangayId = null;
  } else if (typeof bId === "string" && bId.trim()) {
    barangayId = bId;
  } else {
    throw new ApiError(`Invalid map workspace: facility barangay id${ix}`, 502, parent);
  }

  let barangayName: string | null = null;
  const bName = raw.barangayName;
  if (bName === null || bName === undefined) {
    barangayName = null;
  } else if (typeof bName === "string") {
    barangayName = bName;
  } else {
    throw new ApiError(`Invalid map workspace: facility barangay name${ix}`, 502, parent);
  }

  let description: string | null = null;
  const desc = raw.description;
  if (desc === null || desc === undefined) {
    description = null;
  } else if (typeof desc === "string") {
    description = desc;
  } else {
    throw new ApiError(`Invalid map workspace: facility description${ix}`, 502, parent);
  }

  return {
    id,
    name,
    facilityType: facilityType.trim(),
    barangayId,
    barangayName,
    latitude: parseNullableNumber(raw.latitude),
    longitude: parseNullableNumber(raw.longitude),
    capacity: parseNullableNumber(raw.capacity),
    isEvacuationSite,
    description
  };
}

export function parseMapAssetSummary(raw: Record<string, unknown>, parent: unknown, index?: number): MapAssetSummary {
  const id = raw.id;
  const name = raw.name;
  const mapType = raw.mapType;
  const mapFormat = raw.mapFormat;
  const originalFileName = raw.originalFileName;
  const contentType = raw.contentType;
  const fileSizeBytes = raw.fileSizeBytes;
  const featureCount = raw.featureCount;
  const ix = index != null ? ` entry ${index}` : "";
  if (
    !isNonEmptyString(id) ||
    !isNonEmptyString(name) ||
    typeof mapType !== "string" ||
    !mapType.trim() ||
    typeof mapFormat !== "string" ||
    !mapFormat.trim() ||
    !isNonEmptyString(originalFileName) ||
    typeof contentType !== "string" ||
    !isFiniteNumber(fileSizeBytes) ||
    !isFiniteNumber(featureCount) ||
    !Number.isInteger(featureCount)
  ) {
    throw new ApiError(`Invalid map workspace: map asset${ix}`, 502, parent);
  }

  let description: string | null = null;
  const desc = raw.description;
  if (desc === null || desc === undefined) {
    description = null;
  } else if (typeof desc === "string") {
    description = desc;
  } else {
    throw new ApiError(`Invalid map workspace: map description${ix}`, 502, parent);
  }

  const createdAtUtc = optionalIsoOrNull(raw.createdAtUtc);

  return {
    id,
    name,
    mapType: mapType.trim() as MapType,
    mapFormat: mapFormat.trim() as MapFormat,
    description,
    boundsJson: parseUnknownJsonValue(raw.boundsJson),
    defaultStyleJson: parseUnknownJsonValue(raw.defaultStyleJson),
    originalFileName,
    contentType,
    fileSizeBytes,
    createdAtUtc,
    featureCount
  };
}

export function parseGeoJsonLayerFeatureSummary(
  raw: Record<string, unknown>,
  parent: unknown,
  index: number
): GeoJsonLayerFeatureSummary {
  const id = raw.id;
  const mapAssetId = raw.mapAssetId;
  if (!isNonEmptyString(id) || !isNonEmptyString(mapAssetId)) {
    throw new ApiError(`Invalid features list: entry ${index}`, 502, parent);
  }

  let featureId: string | null = null;
  const fId = raw.featureId;
  if (fId === null || fId === undefined) {
    featureId = null;
  } else if (typeof fId === "string") {
    featureId = fId;
  } else {
    throw new ApiError(`Invalid features list: featureId ${index}`, 502, parent);
  }

  let featureType: string | null = null;
  const fType = raw.featureType;
  if (fType === null || fType === undefined) {
    featureType = null;
  } else if (typeof fType === "string") {
    featureType = fType;
  } else {
    throw new ApiError(`Invalid features list: featureType ${index}`, 502, parent);
  }

  let displayName: string | null = null;
  const dName = raw.displayName;
  if (dName === null || dName === undefined) {
    displayName = null;
  } else if (typeof dName === "string") {
    displayName = dName;
  } else {
    throw new ApiError(`Invalid features list: displayName ${index}`, 502, parent);
  }

  return {
    id,
    mapAssetId,
    featureId,
    featureType,
    displayName,
    propertiesJson: parseUnknownJsonValue(raw.propertiesJson),
    geometryJson: parseUnknownJsonValue(raw.geometryJson),
    styleJson: parseUnknownJsonValue(raw.styleJson),
    createdAtUtc: optionalIsoOrNull(raw.createdAtUtc)
  };
}

function parsePlanMapWorkspaceCounts(raw: Record<string, unknown>, parent: unknown): PlanMapWorkspaceCounts {
  const fields = [
    "mapAssets",
    "geoJsonLayers",
    "barangays",
    "criticalFacilities",
    "evacuationSites"
  ] as const;
  const nums: Record<string, number> = {};
  for (const k of fields) {
    const v = raw[k];
    if (!isFiniteNumber(v)) {
      throw new ApiError(`Invalid map workspace counts: missing ${k}`, 502, parent);
    }
    nums[k] = v;
  }
  return nums as unknown as PlanMapWorkspaceCounts;
}

export function parsePlanMapWorkspace(payload: unknown): PlanMapWorkspaceResult {
  if (!isRecord(payload)) {
    throw new ApiError("Invalid map workspace response", 502, payload);
  }

  const planIdRaw = payload.planId;
  if (!isNonEmptyString(planIdRaw)) {
    throw new ApiError("Invalid map workspace: planId", 502, payload);
  }

  const mapAssetsRaw = payload.mapAssets;
  const barangaysRaw = payload.barangays;
  const facilitiesRaw = payload.criticalFacilities;
  const countsRaw = payload.counts;
  if (
    !Array.isArray(mapAssetsRaw) ||
    !Array.isArray(barangaysRaw) ||
    !Array.isArray(facilitiesRaw) ||
    !isRecord(countsRaw)
  ) {
    throw new ApiError("Invalid map workspace arrays", 502, payload);
  }

  const mapAssets = mapAssetsRaw.map((x, i) => {
    if (!isRecord(x)) {
      throw new ApiError(`Invalid mapAssets[${i}]`, 502, payload);
    }
    return parseMapAssetSummary(x, payload, i);
  });
  const barangays = barangaysRaw.map((x, i) => {
    if (!isRecord(x)) {
      throw new ApiError(`Invalid barangays[${i}]`, 502, payload);
    }
    return parseBarangaySummary(x, payload, i);
  });
  const criticalFacilities = facilitiesRaw.map((x, i) => {
    if (!isRecord(x)) {
      throw new ApiError(`Invalid criticalFacilities[${i}]`, 502, payload);
    }
    return parseCriticalFacilitySummary(x, payload, i);
  });

  const counts = parsePlanMapWorkspaceCounts(countsRaw, payload);

  return {
    planId: planIdRaw,
    mapAssets,
    barangays,
    criticalFacilities,
    counts
  };
}

export function parseGeoJsonLayerFeatureSummaries(payload: unknown): GeoJsonLayerFeatureSummary[] {
  let items: unknown[] = [];
  if (isRecord(payload) && Array.isArray(payload.items)) {
    items = payload.items;
  } else if (Array.isArray(payload)) {
    items = payload;
  } else {
    throw new ApiError("Invalid features response", 502, payload);
  }

  return items.map((item, i) => {
    if (!isRecord(item)) {
      throw new ApiError(`Invalid features list: entry ${i}`, 502, payload);
    }
    return parseGeoJsonLayerFeatureSummary(item, payload, i);
  });
}

export function parseCreatedGeoJsonMapSummary(payload: unknown): CreatedGeoJsonMapAssetSummary {
  if (!isRecord(payload)) {
    throw new ApiError("Invalid create GeoJSON layer response", 502, payload);
  }

  const id = payload.id;
  const name = payload.name;
  const mapType = payload.mapType;
  const mapFormat = payload.mapFormat;
  const featureCount = payload.featureCount;
  const originalFileName = payload.originalFileName;
  const contentType = payload.contentType;
  const fileSizeBytes = payload.fileSizeBytes;

  if (
    !isNonEmptyString(id) ||
    !isNonEmptyString(name) ||
    typeof mapType !== "string" ||
    !mapType.trim() ||
    typeof mapFormat !== "string" ||
    !mapFormat.trim() ||
    !isFiniteNumber(featureCount) ||
    !Number.isInteger(featureCount) ||
    !isNonEmptyString(originalFileName) ||
    typeof contentType !== "string"
  ) {
    throw new ApiError("Invalid create GeoJSON layer response fields", 502, payload);
  }

  let description: string | null = null;
  const desc = payload.description;
  if (desc === null || desc === undefined) {
    description = null;
  } else if (typeof desc === "string") {
    description = desc;
  } else {
    throw new ApiError("Invalid create GeoJSON layer description", 502, payload);
  }

  const createdAtUtc = optionalIsoOrNull(payload.createdAtUtc);

  if (!isFiniteNumber(fileSizeBytes)) {
    throw new ApiError("Invalid create GeoJSON layer fileSizeBytes", 502, payload);
  }

  return {
    id,
    name,
    mapType: mapType.trim() as MapType,
    mapFormat: mapFormat.trim() as MapFormat,
    description,
    featureCount,
    originalFileName,
    contentType,
    fileSizeBytes,
    createdAtUtc
  };
}

const ALLOWED_NOTIFICATION_EVENT_TYPES = new Set<string>([
  "SectionCommentCreated",
  "SectionCommentResolved",
  "SectionCommentReopened",
  "SectionCommentArchived",
  "MonitoringUpdateCreated",
  "ActionFundingAllocationCreated",
  "ActionFundingAllocationArchived",
  "GeoJsonLayerCreated",
  "MapAssetArchived",
  "ExportPackageGenerated",
  "PlanUpdated",
  "General"
]);

function optionalNonEmptyStringOrNull(value: unknown): string | null {
  if (value === null || value === undefined) return null;
  if (typeof value === "string" && value.trim()) return value.trim();
  return null;
}

export function parseMyNotificationSummary(payload: unknown): MyNotificationSummary {
  if (!isRecord(payload)) {
    throw new ApiError("Invalid notification: expected object", 502, payload);
  }

  const id = payload.id;
  const notificationEventId = payload.notificationEventId;
  const eventType = payload.eventType;
  const title = payload.title;
  const message = payload.message;
  const entityType = payload.entityType;
  const entityId = payload.entityId;
  const planId = payload.planId;
  const isRead = payload.isRead;
  const readAtUtc = optionalIsoOrNull(payload.readAtUtc);
  const createdAtUtc = payload.createdAtUtc;

  if (
    !isNonEmptyString(id) ||
    !isNonEmptyString(notificationEventId) ||
    typeof eventType !== "string" ||
    !ALLOWED_NOTIFICATION_EVENT_TYPES.has(eventType) ||
    typeof title !== "string" ||
    !title.trim() ||
    typeof message !== "string" ||
    !message.trim() ||
    typeof isRead !== "boolean" ||
    !isNonEmptyString(createdAtUtc)
  ) {
    throw new ApiError("Invalid notification: malformed fields", 502, payload);
  }

  return {
    id,
    notificationEventId,
    eventType: eventType as NotificationEventType,
    title: title.trim(),
    message: message.trim(),
    entityType: optionalNonEmptyStringOrNull(entityType),
    entityId: optionalNonEmptyStringOrNull(entityId),
    planId: optionalNonEmptyStringOrNull(planId),
    isRead,
    readAtUtc,
    createdAtUtc
  };
}

export function parseMyNotificationsResult(payload: unknown): MyNotificationsResult {
  if (!isRecord(payload)) {
    throw new ApiError("Invalid notifications response: expected object", 502, payload);
  }

  const itemsRaw = payload.items;
  const unreadCount = payload.unreadCount;
  const totalCount = payload.totalCount;
  const limit = payload.limit;
  const unreadOnly = payload.unreadOnly;

  if (!Array.isArray(itemsRaw) || !isFiniteNumber(unreadCount) || !isFiniteNumber(totalCount) || !isFiniteNumber(limit) || typeof unreadOnly !== "boolean") {
    throw new ApiError("Invalid notifications response: malformed fields", 502, payload);
  }

  const items = itemsRaw.map((x, i) => {
    try {
      return parseMyNotificationSummary(x);
    } catch {
      throw new ApiError(`Invalid notifications list: entry ${i} is malformed`, 502, payload);
    }
  });

  return {
    items,
    unreadCount: unreadCount as number,
    totalCount: totalCount as number,
    limit: limit as number,
    unreadOnly
  };
}

export function parseCollaborationSummaryResult(payload: unknown): CollaborationSummaryResult {
  if (!isRecord(payload)) {
    throw new ApiError("Invalid collaboration summary response: expected object", 502, payload);
  }

  const groupsRaw = payload.groups;
  const totalGroups = payload.totalGroups;
  const totalMembers = payload.totalMembers;

  if (!Array.isArray(groupsRaw) || !isFiniteNumber(totalGroups) || !isFiniteNumber(totalMembers)) {
    throw new ApiError("Invalid collaboration summary response: malformed fields", 502, payload);
  }

  const groups = groupsRaw.map((g, i) => {
    if (!isRecord(g)) {
      throw new ApiError(`Invalid collaboration groups: entry ${i} expected object`, 502, payload);
    }

    const id = g.id;
    const name = g.name;
    const createdAtUtc = g.createdAtUtc;
    const memberCount = g.memberCount;
    const membersRaw2 = g.members;

    if (!isNonEmptyString(id) || typeof name !== "string" || !name.trim() || !isNonEmptyString(createdAtUtc) || !isFiniteNumber(memberCount) || !Number.isInteger(memberCount) || !Array.isArray(membersRaw2)) {
      throw new ApiError(`Invalid collaboration group entry ${i}: malformed fields`, 502, payload);
    }

    const members = membersRaw2.map((m, mi) => {
      if (!isRecord(m)) {
        throw new ApiError(`Invalid collaboration members: entry ${mi} expected object`, 502, payload);
      }

      const userId = m.userId;
      const fullName = m.fullName;
      const email = m.email;
      const role = m.role;

      if (!isNonEmptyString(userId) || typeof fullName !== "string" || !fullName.trim() || typeof email !== "string" || !email.trim() || typeof role !== "string" || !role.trim()) {
        throw new ApiError(`Invalid collaboration member entry ${mi}: malformed fields`, 502, payload);
      }

      return {
        userId,
        fullName: fullName.trim(),
        email: email.trim(),
        role: role.trim()
      } satisfies CollaborationMemberSummary;
    });

    return {
      id,
      name: name.trim(),
      createdAtUtc,
      memberCount,
      members
    } satisfies CollaborationGroupSummary;
  });

  return {
    groups,
    totalGroups: totalGroups as number,
    totalMembers: totalMembers as number
  };
}