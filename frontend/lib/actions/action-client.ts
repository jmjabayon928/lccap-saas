import { endpoints } from "@/lib/api/endpoints";
import { http } from "@/lib/api/http";
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
  FundingProgramsResult,
  FundingSourcesResult,
  SaveActionItemResult,
  UpdateActionItemRequest
} from "@/types/actions";

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
  }
} as const;
