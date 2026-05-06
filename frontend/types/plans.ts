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

export type MapType =
  | "Flood"
  | "Landslide"
  | "StormSurge"
  | "Boundary"
  | "LandUse"
  | "Hazard"
  | "Other"
  | (string & {});

export type MapFormat = "Image" | "Pdf" | "GeoJson" | (string & {});

export interface MapAssetSummary {
  readonly id: string;
  readonly name: string;
  readonly mapType: MapType;
  readonly mapFormat: MapFormat;
  readonly description: string | null;
  readonly boundsJson: unknown | null;
  readonly defaultStyleJson: unknown;
  readonly originalFileName: string;
  readonly contentType: string;
  readonly fileSizeBytes: number;
  readonly createdAtUtc: string | null;
  readonly featureCount: number;
}

export interface GeoJsonLayerFeatureSummary {
  readonly id: string;
  readonly mapAssetId: string;
  readonly featureId: string | null;
  readonly featureType: string | null;
  readonly displayName: string | null;
  readonly propertiesJson: unknown;
  readonly geometryJson: unknown;
  readonly styleJson: unknown;
  readonly createdAtUtc: string | null;
}

export interface BarangaySummary {
  readonly id: string;
  readonly name: string;
  readonly code: string | null;
  readonly latitude: number | null;
  readonly longitude: number | null;
  readonly landAreaHectares: number | null;
  readonly population: number | null;
  readonly households: number | null;
  readonly classification: string | null;
}

export interface CriticalFacilitySummary {
  readonly id: string;
  readonly name: string;
  readonly facilityType: string;
  readonly barangayId: string | null;
  readonly barangayName: string | null;
  readonly latitude: number | null;
  readonly longitude: number | null;
  readonly capacity: number | null;
  readonly isEvacuationSite: boolean;
  readonly description: string | null;
}

export interface PlanMapWorkspaceCounts {
  readonly mapAssets: number;
  readonly geoJsonLayers: number;
  readonly barangays: number;
  readonly criticalFacilities: number;
  readonly evacuationSites: number;
}

export interface PlanMapWorkspaceResult {
  readonly planId: string;
  readonly mapAssets: readonly MapAssetSummary[];
  readonly hazardLayerMapAssetIds: readonly string[];
  readonly barangays: readonly BarangaySummary[];
  readonly criticalFacilities: readonly CriticalFacilitySummary[];
  readonly counts: PlanMapWorkspaceCounts;
}

export interface HazardLayerSummary {
  readonly id: string;
  readonly planId: string;
  readonly mapAssetId: string | null;
  readonly name: string;
  readonly hazardType: string;
  readonly severity: string;
  readonly source: string | null;
  readonly description: string | null;
  readonly isActive: boolean;
  readonly createdAtUtc: string;
}

export type RegisterHazardLayerPayload = {
  readonly mapAssetId: string;
  readonly name: string;
  readonly hazardType: string;
  readonly severity: "Low" | "Moderate" | "High" | "VeryHigh";
  readonly source: string | null;
  readonly description: string | null;
};

export interface ExposureAnalysisJobSummary {
  readonly id: string;
  readonly planId: string;
  readonly status: string;
  readonly hazardLayerId: string | null;
  readonly errorMessage: string | null;
  readonly createdAtUtc: string;
  readonly startedAtUtc: string | null;
  readonly completedAtUtc: string | null;
}

export type CreateExposureAnalysisJobPayload = {
  readonly hazardLayerId: string;
};

export interface ExposureSummary {
  readonly id: string;
  readonly planId: string;
  readonly exposureAnalysisJobId: string | null;
  readonly barangayId: string | null;
  readonly criticalFacilityId: string | null;
  readonly hazardLayerId: string | null;
  readonly hazardType: string;
  readonly severity: string | null;
  readonly exposedAreaHectares: number | null;
  readonly exposedFacilityCount: number;
  readonly exposedPopulation: number | null;
  readonly riskScore: number | null;
  readonly summaryJson: unknown;
  readonly createdAtUtc: string;
}

export interface GeoJsonFeatureGeometryPayload {
  readonly type?: string;
}

export interface GeoJsonFeaturePayload {
  readonly type?: string;
  readonly id?: unknown;
  readonly properties?: Readonly<Record<string, unknown>>;
  readonly geometry?: GeoJsonFeatureGeometryPayload & Readonly<Record<string, unknown>>;
}

export interface GeoJsonFeatureCollectionPayload {
  readonly type: "FeatureCollection";
  readonly features: readonly GeoJsonFeaturePayload[];
}

export interface CreateGeoJsonLayerRequest {
  readonly fileAssetId: string;
  readonly name: string;
  readonly mapType: MapType;
  readonly description?: string | null;
  readonly geoJson: GeoJsonFeatureCollectionPayload;
  readonly defaultStyleJson?: unknown | null;
  readonly boundsJson?: unknown | null;
}

export interface CreatedGeoJsonMapAssetSummary {
  readonly id: string;
  readonly name: string;
  readonly mapType: MapType;
  readonly mapFormat: MapFormat;
  readonly description: string | null;
  readonly featureCount: number;
  readonly originalFileName: string;
  readonly contentType: string;
  readonly fileSizeBytes: number;
  readonly createdAtUtc: string | null;
}

export type NotificationEventType =
  | "SectionCommentCreated"
  | "SectionCommentResolved"
  | "SectionCommentReopened"
  | "SectionCommentArchived"
  | "MonitoringUpdateCreated"
  | "ActionFundingAllocationCreated"
  | "ActionFundingAllocationArchived"
  | "GeoJsonLayerCreated"
  | "MapAssetArchived"
  | "ExportPackageGenerated"
  | "PlanUpdated"
  | "General"
  | (string & {});

export interface MyNotificationSummary {
  readonly id: string;
  readonly notificationEventId: string;
  readonly eventType: NotificationEventType;
  readonly title: string;
  readonly message: string;
  readonly entityType: string | null;
  readonly entityId: string | null;
  readonly planId: string | null;
  readonly isRead: boolean;
  readonly readAtUtc: string | null;
  readonly createdAtUtc: string;
}

export interface MyNotificationsResult {
  readonly items: readonly MyNotificationSummary[];
  readonly unreadCount: number;
  readonly totalCount: number;
  readonly limit: number;
  readonly unreadOnly: boolean;
}

export interface CollaborationMemberSummary {
  readonly userId: string;
  readonly fullName: string;
  readonly email: string;
  readonly role: string;
}

export interface CollaborationGroupSummary {
  readonly id: string;
  readonly name: string;
  readonly createdAtUtc: string;
  readonly memberCount: number;
  readonly members: readonly CollaborationMemberSummary[];
}

export interface CollaborationSummaryResult {
  readonly groups: readonly CollaborationGroupSummary[];
  readonly totalGroups: number;
  readonly totalMembers: number;
}
