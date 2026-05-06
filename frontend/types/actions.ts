/**
 * HTTP-oriented action item types for the LCCAP API client (no database DTOs).
 */

export type ActionType = "Adaptation" | "Mitigation";

export type ActionStatus =
  | "Planned"
  | "InProgress"
  | "OnTrack"
  | "Delayed"
  | "Completed"
  | "Cancelled";

export interface ActionItemSummary {
  readonly id: string;
  readonly planId: string;
  /** Base64 row version from API (optimistic concurrency). */
  readonly rowVersion: string | null;
  readonly title: string;
  readonly description: string | null;
  readonly actionType: ActionType;
  readonly sector: string;
  readonly responsibleOffice: string | null;
  readonly budgetAmount: number;
  readonly fundingSource: string | null;
  readonly timelineStartUtc: string | null;
  readonly timelineEndUtc: string | null;
  readonly kpi: string | null;
  readonly priorityScore: number | null;
  readonly status: ActionStatus;
  readonly createdAtUtc: string | null;
  readonly updatedAtUtc: string | null;
}

export type ActionItemDetail = ActionItemSummary;

export interface CreateActionItemRequest {
  readonly title: string;
  readonly description: string | null;
  readonly actionType: ActionType;
  readonly sector: string;
  readonly responsibleOffice: string | null;
  readonly budgetAmount: number;
  readonly fundingSource: string | null;
  readonly timelineStartUtc: string | null;
  readonly timelineEndUtc: string | null;
  readonly kpi: string | null;
  readonly priorityScore: number | null;
  readonly status: ActionStatus;
}

export type UpdateActionItemRequest = CreateActionItemRequest & {
  readonly rowVersion: string | null;
};

export type SaveActionItemResult = ActionItemDetail;

export type ClimateExpenditureTagCategory =
  | "Adaptation"
  | "Mitigation"
  | "CrossCutting"
  | "DisasterRiskReduction"
  | "CapacityDevelopment"
  | "Other";

export interface ClimateExpenditureTagSummary {
  readonly id: string;
  readonly tagCode: string;
  readonly tagName: string;
  readonly tagCategory: ClimateExpenditureTagCategory;
  readonly weightPercent: number | null;
  readonly description: string | null;
  readonly isActive: boolean;
  readonly createdAtUtc: string | null;
}

export interface ClimateExpenditureTagsResult {
  readonly items: readonly ClimateExpenditureTagSummary[];
  readonly totalCount: number;
  readonly includeInactive: boolean;
}

export type ActionFundingAllocationStatus = "Planned";

export interface ActionFundingAllocationSummary {
  readonly id: string;
  readonly planId: string;
  readonly actionItemId: string;
  readonly actionTitle: string;
  readonly fundingSourceId: string;
  readonly fundingSourceName: string;
  readonly fundingProgramId: string | null;
  readonly fundingProgramName: string | null;
  readonly climateExpenditureTagId: string | null;
  readonly climateExpenditureTagCode: string | null;
  readonly climateExpenditureTagName: string | null;
  readonly climateExpenditureTagCategory: string | null;
  readonly fiscalYear: number;
  readonly allocatedAmount: number;
  readonly currencyCode: string;
  /** Display status; unknown API values are normalized to Planned in parsers. */
  readonly allocationStatus: ActionFundingAllocationStatus;
  readonly notes: string | null;
  readonly createdAtUtc: string | null;
}

export interface ActionFundingAllocationsResult {
  readonly items: readonly ActionFundingAllocationSummary[];
  readonly totalCount: number;
}

export interface CreateActionFundingAllocationRequest {
  readonly actionItemId: string;
  readonly fundingSourceId: string;
  readonly fundingProgramId?: string | null;
  readonly climateExpenditureTagId?: string | null;
  readonly fiscalYear: number;
  readonly allocatedAmount: number;
  readonly currencyCode?: string | null;
  readonly allocationStatus?: "Planned" | null;
  readonly notes?: string | null;
}
