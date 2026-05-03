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
  readonly rowVersion: string;
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
  readonly rowVersion: string;
};

export type SaveActionItemResult = ActionItemDetail;
