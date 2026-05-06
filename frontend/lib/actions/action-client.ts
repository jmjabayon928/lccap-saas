import { endpoints } from "@/lib/api/endpoints";
import { http } from "@/lib/api/http";
import {
  parseActionItem,
  parseActionItemsList,
  parseClimateExpenditureTagsResult,
  parseSaveActionItemResult
} from "@/lib/actions/action-parsers";
import type {
  ActionItemDetail,
  ActionItemSummary,
  ClimateExpenditureTagsResult,
  CreateActionItemRequest,
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
  }
} as const;
