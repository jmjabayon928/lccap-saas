/**
 * HTTP contract types for plans (no database or server-internal DTOs).
 */

/** Values commonly used in forms; APIs may return additional status strings. */
export type PlanStatus =
  | "Draft"
  | "InProgress"
  | "ReadyForExport"
  | "Submitted"
  | "Approved"
  | "Archived"
  | (string & {});

export type TemplateMode = "New" | "Partial" | "Enhancement" | (string & {});

export interface PlanSummary {
  readonly id: string;
  readonly accountId: string;
  readonly title: string;
  readonly startYear: number;
  readonly endYear: number;
  readonly status: PlanStatus;
  readonly templateMode: TemplateMode;
  readonly versionNumber: number;
  readonly description: string | null;
  readonly createdAtUtc: string | null;
  readonly updatedAtUtc: string | null;
  readonly rowVersion: string | null;
}

export interface CreatePlanRequest {
  readonly title: string;
  readonly startYear: number;
  readonly endYear: number;
  readonly status: PlanStatus;
  readonly templateMode: TemplateMode;
  readonly versionNumber: number;
  readonly description?: string;
}

/** Normalized create response — `planId` is always set after parsing. */
export interface CreatePlanResult {
  readonly planId: string;
  readonly title: string;
  readonly startYear: number;
  readonly endYear: number;
  readonly status: string;
}

export interface UpdatePlanMetadataRequest {
  readonly title: string;
  readonly startYear: number;
  readonly endYear: number;
  readonly status: PlanStatus;
  readonly templateMode: TemplateMode;
  readonly versionNumber: number;
  readonly description: string | null;
  readonly rowVersion: string | null;
}

export interface PlanSectionSummary {
  readonly id: string;
  readonly planId: string;
  readonly sectionKey: string;
  readonly title: string;
  readonly content: string;
  readonly sortOrder: number;
  readonly lastEditedAtUtc: string | null;
}

/** Full section payload from GET-by-key; structurally matches summary for this API. */
export type PlanSectionDetail = PlanSectionSummary;

export interface SavePlanSectionRequest {
  readonly title: string;
  readonly content: string;
  readonly sortOrder: number;
}

/** Normalized PUT response for a section. */
export interface SavePlanSectionResult {
  readonly sectionId: string;
  readonly lastEditedAtUtc: string;
  readonly lastEditedByUserId?: string;
  // Optional full object fields for backward compatibility
  readonly id?: string;
  readonly planId?: string;
  readonly sectionKey?: string;
  readonly title?: string;
  readonly content?: string;
  readonly sortOrder?: number;
}

export interface PlanSectionHistoryEntry {
  readonly auditLogId: string;
  readonly sectionId: string;
  readonly planId: string;
  readonly sectionKey: string;
  readonly action: "PlanSectionUpdated" | "PlanSectionRestored";
  readonly title: string;
  readonly content: string;
  readonly createdAtUtc: string;
  readonly userId: string | null;
  readonly canRestore: boolean;
}

export interface RestorePlanSectionRequest {
  readonly auditLogId: string;
  readonly restoreReason?: string;
}

export type SectionCommentType = "General" | "DataGap" | "Validation" | "RevisionRequest";

export interface SectionCommentSummary {
  readonly id: string;
  readonly planId: string;
  readonly sectionKey: string;
  readonly commentType: SectionCommentType;
  readonly commentText: string;
  readonly createdByUserId: string;
  readonly createdAtUtc: string;
  readonly isResolved: boolean;
  readonly resolvedAtUtc: string | null;
  readonly resolvedByUserId: string | null;
  readonly updatedAtUtc: string | null;
  readonly rowVersion: string | null;
}

export interface CreateSectionCommentRequest {
  readonly commentType: SectionCommentType;
  readonly commentText: string;
}

export interface FundingCurrencyTotal {
  readonly currencyCode: string;
  readonly totalAllocatedAmount: number;
}

export interface EvidenceDashboardSummary {
  readonly totalDocuments: number;
  readonly officialEvidenceCount: number;
  readonly publicEvidenceCount: number;
  readonly draftEvidenceCount: number;
  readonly internalEvidenceCount: number;
  readonly linkedToSectionCount: number;
  readonly linkedToActionCount: number;
}

export interface ActionDashboardSummary {
  readonly totalActions: number;
  readonly plannedCount: number;
  readonly inProgressCount: number;
  readonly onTrackCount: number;
  readonly delayedCount: number;
  readonly completedCount: number;
  readonly cancelledCount: number;
  readonly actionsWithBudgetCount: number;
  readonly actionsWithFundingSourceCount: number;
  readonly missingFundingSourceCount: number;
}

export interface MonitoringDashboardSummary {
  readonly totalIndicators: number;
  readonly notStartedCount: number;
  readonly inProgressCount: number;
  readonly onTrackCount: number;
  readonly delayedCount: number;
  readonly completedCount: number;
  readonly totalMonitoringUpdates: number;
  readonly indicatorsWithUpdatesCount: number;
  readonly latestMonitoringUpdateAtUtc: string | null;
}

export interface ReviewDashboardSummary {
  readonly totalComments: number;
  readonly unresolvedComments: number;
  readonly resolvedComments: number;
  readonly dataGapComments: number;
  readonly validationComments: number;
  readonly revisionRequestComments: number;
}

export interface FundingDashboardSummary {
  readonly totalAllocations: number;
  readonly ccetTaggedAllocations: number;
  readonly untaggedAllocations: number;
  readonly allocationTotalsByCurrency: readonly FundingCurrencyTotal[];
}

export interface ExportReadinessDashboardSummary {
  readonly hasOfficialEvidence: boolean;
  readonly hasActions: boolean;
  readonly hasMonitoring: boolean;
  readonly hasFundingAllocations: boolean;
  readonly hasUnresolvedComments: boolean;
  readonly suggestedNextSteps: readonly string[];
}

export interface PlanActivityItem {
  readonly id: string;
  readonly action: string;
  readonly entityType: string;
  readonly entityId: string | null;
  readonly createdAtUtc: string;
  readonly summary: string;
}

export interface PlanOperationalDashboard {
  readonly planId: string;
  readonly planTitle: string;
  readonly planningPeriodStart: number;
  readonly planningPeriodEnd: number;
  readonly status: string;
  readonly generatedAtUtc: string;
  readonly evidence: EvidenceDashboardSummary;
  readonly actions: ActionDashboardSummary;
  readonly monitoring: MonitoringDashboardSummary;
  readonly review: ReviewDashboardSummary;
  readonly funding: FundingDashboardSummary;
  readonly exportReadiness: ExportReadinessDashboardSummary;
  readonly recentActivity: readonly PlanActivityItem[];
}
