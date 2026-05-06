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

export type FundingSourceType =
  | "LGUInternal"
  | "NationalGovernment"
  | "ProvincialGovernment"
  | "Donor"
  | "NGO"
  | "PrivateSector"
  | "BankLoan"
  | "ClimateFund"
  | "Other";

export interface FundingSourceSummary {
  readonly id: string;
  readonly name: string;
  readonly sourceType: FundingSourceType;
  readonly description: string | null;
  readonly contactName: string | null;
  readonly contactEmail: string | null;
  readonly websiteUrl: string | null;
  readonly createdAtUtc: string | null;
}

export interface FundingSourcesResult {
  readonly items: readonly FundingSourceSummary[];
  readonly totalCount: number;
}

export type FundingProgramStatus = "Draft" | "Active" | "Closed" | "Archived";

export interface FundingProgramSummary {
  readonly id: string;
  readonly fundingSourceId: string;
  readonly fundingSourceName: string;
  readonly name: string;
  readonly programCode: string | null;
  readonly description: string | null;
  readonly eligibleUses: string | null;
  readonly applicationUrl: string | null;
  readonly opensAtUtc: string | null;
  readonly closesAtUtc: string | null;
  readonly maxAwardAmount: number | null;
  readonly currencyCode: string;
  readonly status: FundingProgramStatus;
  readonly createdAtUtc: string | null;
}

export interface FundingProgramsResult {
  readonly items: readonly FundingProgramSummary[];
  readonly totalCount: number;
  readonly fundingSourceId: string | null;
  readonly includeInactiveOrClosed: boolean;
}

export type FundingSourcesLoadState =
  | { readonly status: "idle" }
  | { readonly status: "loading" }
  | { readonly status: "ready"; readonly data: FundingSourcesResult }
  | { readonly status: "error"; readonly message: string };

export type FundingProgramsLoadState =
  | { readonly status: "idle" }
  | { readonly status: "loading" }
  | { readonly status: "ready"; readonly data: FundingProgramsResult }
  | { readonly status: "error"; readonly message: string };

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

/** JSON from GET /api/plans/{planId}/exports/package-manifest */
export interface ExportPackageCounts {
  readonly documents: number;
  readonly officialEvidence: number;
  readonly publicEvidence: number;
  readonly actions: number;
  readonly monitoringIndicators: number;
  readonly monitoringUpdates: number;
  readonly unresolvedSectionComments: number;
  readonly fundingAllocations: number;
  readonly ccetTaggedAllocations: number;
}

export interface ExportPackageReadiness {
  readonly hasOfficialEvidence: boolean;
  readonly hasActions: boolean;
  readonly hasMonitoring: boolean;
  readonly hasFundingAllocations: boolean;
  readonly hasUnresolvedComments: boolean;
}

export interface ExportPackageDownloads {
  readonly evidenceIndexCsv: string;
  readonly actionMatrixCsv: string;
  readonly monitoringMatrixCsv: string;
  readonly fundingReadinessCsv: string;
}

export interface ExportPackageManifest {
  readonly planId: string;
  readonly planTitle: string;
  readonly planningPeriodStart: number;
  readonly planningPeriodEnd: number;
  readonly status: string;
  readonly generatedAtUtc: string;
  readonly counts: ExportPackageCounts;
  readonly readiness: ExportPackageReadiness;
  readonly availableDownloads: ExportPackageDownloads;
  readonly notes: readonly string[];
}
