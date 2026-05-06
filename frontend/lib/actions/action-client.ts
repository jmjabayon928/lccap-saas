import { ApiError } from "@/lib/api/api-error";
import { endpoints } from "@/lib/api/endpoints";
import { http } from "@/lib/api/http";
import { config } from "@/lib/config";
import { getAccessToken } from "@/lib/auth/auth-storage";
import {
  parseActionFundingAllocationSummary,
  parseActionFundingAllocationsResult,
  parseActionItem,
  parseActionItemsList,
  parseClimateExpenditureTagsResult,
  parseFundingProgramsResult,
  parseFundingSourcesResult,
  parseSaveActionItemResult
} from "@/lib/actions/action-parsers";
import type {
  ActionFundingAllocationSummary,
  ActionFundingAllocationsResult,
  ActionItemDetail,
  ActionItemSummary,
  ClimateExpenditureTagsResult,
  CreateActionFundingAllocationRequest,
  CreateActionItemRequest,
  ExportPackageManifest,
  FundingProgramsResult,
  FundingSourcesResult,
  SaveActionItemResult,
  UpdateActionItemRequest
} from "@/types/actions";

function buildAbsoluteUrl(path: string): string {
  const base = config.apiBaseUrl;
  const prefix = path.startsWith("/") ? path : `/${path}`;
  return `${base}${prefix}`;
}

function authHeaders(): HeadersInit {
  const token = getAccessToken();
  return token ? { Authorization: `Bearer ${token}` } : {};
}

function extractFilenameFromContentDisposition(header: string | null): string | null {
  if (!header) {
    return null;
  }
  const parts = header.split(";").map((p) => p.trim());
  const filenamePart = parts.find((p) => p.toLowerCase().startsWith("filename="));
  if (!filenamePart) {
    return null;
  }
  const raw = filenamePart.slice("filename=".length).trim();
  const unquoted = raw.startsWith("\"") && raw.endsWith("\"") ? raw.slice(1, -1) : raw;
  const cleaned = unquoted.replaceAll("\\", "").trim();
  return cleaned.length > 0 ? cleaned : null;
}

async function fetchExportCsv(path: string, fallbackFileName: string): Promise<{ readonly blob: Blob; readonly fileName: string }> {
  const response = await fetch(buildAbsoluteUrl(path), {
    method: "GET",
    headers: {
      ...authHeaders(),
      Accept: "text/csv, text/plain, */*"
    },
    credentials: "omit"
  });
  if (!response.ok) {
    throw await ApiError.fromResponse(response);
  }
  const blob = await response.blob();
  const cd = response.headers.get("content-disposition");
  return { blob, fileName: extractFilenameFromContentDisposition(cd) ?? fallbackFileName };
}

function isRecord(v: unknown): v is Record<string, unknown> {
  return typeof v === "object" && v !== null && !Array.isArray(v);
}

function requireFiniteNumber(v: unknown, field: string): number {
  if (typeof v !== "number" || !Number.isFinite(v)) {
    throw new Error(`Invalid export manifest: ${field}`);
  }
  return v;
}

function requireBool(v: unknown, field: string): boolean {
  if (typeof v !== "boolean") {
    throw new Error(`Invalid export manifest: ${field}`);
  }
  return v;
}

function parseExportPackageManifest(data: unknown): ExportPackageManifest {
  if (!isRecord(data)) {
    throw new Error("Invalid export manifest: root");
  }
  const planIdParsed = typeof data.planId === "string" ? data.planId.trim() : "";
  if (!planIdParsed) {
    throw new Error("Invalid export manifest: planId");
  }
  const countsRaw = data.counts;
  const readinessRaw = data.readiness;
  const downloadsRaw = data.availableDownloads;
  if (!isRecord(countsRaw) || !isRecord(readinessRaw) || !isRecord(downloadsRaw)) {
    throw new Error("Invalid export manifest: nested objects");
  }
  const notesRaw = data.notes;
  if (!Array.isArray(notesRaw) || !notesRaw.every((n) => typeof n === "string")) {
    throw new Error("Invalid export manifest: notes");
  }
  return {
    planId: planIdParsed,
    planTitle: typeof data.planTitle === "string" ? data.planTitle : "",
    planningPeriodStart: requireFiniteNumber(data.planningPeriodStart, "planningPeriodStart"),
    planningPeriodEnd: requireFiniteNumber(data.planningPeriodEnd, "planningPeriodEnd"),
    status: typeof data.status === "string" ? data.status : "",
    generatedAtUtc: typeof data.generatedAtUtc === "string" ? data.generatedAtUtc : "",
    counts: {
      documents: requireFiniteNumber(countsRaw.documents, "counts.documents"),
      officialEvidence: requireFiniteNumber(countsRaw.officialEvidence, "counts.officialEvidence"),
      publicEvidence: requireFiniteNumber(countsRaw.publicEvidence, "counts.publicEvidence"),
      actions: requireFiniteNumber(countsRaw.actions, "counts.actions"),
      monitoringIndicators: requireFiniteNumber(countsRaw.monitoringIndicators, "counts.monitoringIndicators"),
      monitoringUpdates: requireFiniteNumber(countsRaw.monitoringUpdates, "counts.monitoringUpdates"),
      unresolvedSectionComments: requireFiniteNumber(countsRaw.unresolvedSectionComments, "counts.unresolvedSectionComments"),
      fundingAllocations: requireFiniteNumber(countsRaw.fundingAllocations, "counts.fundingAllocations"),
      ccetTaggedAllocations: requireFiniteNumber(countsRaw.ccetTaggedAllocations, "counts.ccetTaggedAllocations")
    },
    readiness: {
      hasOfficialEvidence: requireBool(readinessRaw.hasOfficialEvidence, "readiness.hasOfficialEvidence"),
      hasActions: requireBool(readinessRaw.hasActions, "readiness.hasActions"),
      hasMonitoring: requireBool(readinessRaw.hasMonitoring, "readiness.hasMonitoring"),
      hasFundingAllocations: requireBool(readinessRaw.hasFundingAllocations, "readiness.hasFundingAllocations"),
      hasUnresolvedComments: requireBool(readinessRaw.hasUnresolvedComments, "readiness.hasUnresolvedComments")
    },
    availableDownloads: {
      evidenceIndexCsv:
        typeof downloadsRaw.evidenceIndexCsv === "string" ? downloadsRaw.evidenceIndexCsv : "",
      actionMatrixCsv:
        typeof downloadsRaw.actionMatrixCsv === "string" ? downloadsRaw.actionMatrixCsv : "",
      monitoringMatrixCsv:
        typeof downloadsRaw.monitoringMatrixCsv === "string" ? downloadsRaw.monitoringMatrixCsv : "",
      fundingReadinessCsv:
        typeof downloadsRaw.fundingReadinessCsv === "string" ? downloadsRaw.fundingReadinessCsv : ""
    },
    notes: notesRaw
  };
}

export const actionClient = {
  async getActionsByPlan(planId: string): Promise<ActionItemSummary[]> {
    const data = await http.get(endpoints.actionsByPlan(planId));
    return parseActionItemsList(data);
  },

  async getActionById(actionItemId: string): Promise<ActionItemDetail> {
    const data = await http.get(endpoints.actionById(actionItemId));
    return parseActionItem(data);
  },

  async createActionItem(planId: string, request: CreateActionItemRequest): Promise<SaveActionItemResult> {
    const data = await http.postJson(endpoints.actionsByPlan(planId), request);
    return parseSaveActionItemResult(data);
  },

  async updateActionItem(actionItemId: string, request: UpdateActionItemRequest): Promise<SaveActionItemResult> {
    const data = await http.putJson(endpoints.actionById(actionItemId), request);
    return parseSaveActionItemResult(data);
  },

  async archiveActionItem(actionItemId: string): Promise<void> {
    await http.deleteVoid(endpoints.archiveAction(actionItemId));
  },

  async getFundingSources(): Promise<FundingSourcesResult> {
    const data = await http.get("/api/funding/sources");
    return parseFundingSourcesResult(data);
  },

  async getFundingPrograms(options?: {
    fundingSourceId?: string;
    includeInactiveOrClosed?: boolean;
  }): Promise<FundingProgramsResult> {
    const params = new URLSearchParams();
    const fid = options?.fundingSourceId?.trim();
    if (fid) {
      params.set("fundingSourceId", fid);
    }
    if (options?.includeInactiveOrClosed === true) {
      params.set("includeInactiveOrClosed", "true");
    }
    const qs = params.toString();
    const path = qs.length > 0 ? `/api/funding/programs?${qs}` : "/api/funding/programs";
    const data = await http.get(path);
    return parseFundingProgramsResult(data);
  },

  async getClimateExpenditureTags(options?: { includeInactive?: boolean }): Promise<ClimateExpenditureTagsResult> {
    const params = new URLSearchParams();
    if (options?.includeInactive === true) {
      params.set("includeInactive", "true");
    }
    const query = params.toString();
    const path =
      query.length > 0
        ? `/api/funding/climate-expenditure-tags?${query}`
        : `/api/funding/climate-expenditure-tags`;
    const data = await http.get(path);
    return parseClimateExpenditureTagsResult(data);
  },

  async getActionFundingAllocationsByPlan(planId: string): Promise<ActionFundingAllocationsResult> {
    const data = await http.get(`/api/plans/${encodeURIComponent(planId)}/funding-allocations`);
    return parseActionFundingAllocationsResult(data);
  },

  async getActionFundingAllocationsByAction(actionItemId: string): Promise<ActionFundingAllocationsResult> {
    const data = await http.get(`/api/actions/${encodeURIComponent(actionItemId)}/funding-allocations`);
    return parseActionFundingAllocationsResult(data);
  },

  async createActionFundingAllocation(
    planId: string,
    request: CreateActionFundingAllocationRequest
  ): Promise<ActionFundingAllocationSummary> {
    const body = {
      actionItemId: request.actionItemId,
      fundingSourceId: request.fundingSourceId,
      fundingProgramId: request.fundingProgramId ?? null,
      climateExpenditureTagId: request.climateExpenditureTagId ?? null,
      fiscalYear: request.fiscalYear,
      allocatedAmount: request.allocatedAmount,
      currencyCode: request.currencyCode ?? null,
      allocationStatus: request.allocationStatus ?? null,
      notes: request.notes ?? null
    };
    const data = await http.postJson(`/api/plans/${encodeURIComponent(planId)}/funding-allocations`, body);
    return parseActionFundingAllocationSummary(data);
  },

  async archiveActionFundingAllocation(allocationId: string): Promise<void> {
    await http.deleteVoid(`/api/funding-allocations/${encodeURIComponent(allocationId)}`);
  },

  async getExportPackageManifest(planId: string): Promise<ExportPackageManifest> {
    const data = await http.get(`/api/plans/${encodeURIComponent(planId)}/exports/package-manifest`);
    return parseExportPackageManifest(data);
  },

  async downloadActionMatrixCsv(
    planId: string
  ): Promise<{ readonly blob: Blob; readonly fileName: string }> {
    const path = `/api/plans/${encodeURIComponent(planId)}/exports/action-matrix.csv`;
    return fetchExportCsv(path, `action-matrix-${planId}.csv`);
  },

  async downloadMonitoringMatrixCsv(
    planId: string
  ): Promise<{ readonly blob: Blob; readonly fileName: string }> {
    const path = `/api/plans/${encodeURIComponent(planId)}/exports/monitoring-matrix.csv`;
    return fetchExportCsv(path, `monitoring-matrix-${planId}.csv`);
  },

  async downloadFundingReadinessCsv(
    planId: string
  ): Promise<{ readonly blob: Blob; readonly fileName: string }> {
    const path = `/api/plans/${encodeURIComponent(planId)}/exports/funding-readiness.csv`;
    return fetchExportCsv(path, `funding-readiness-${planId}.csv`);
  }
} as const;
