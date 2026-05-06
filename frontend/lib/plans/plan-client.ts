import { endpoints } from "@/lib/api/endpoints";
import { http } from "@/lib/api/http";
import {
  parseCreatePlanResult,
  parseCreatedGeoJsonMapSummary,
  parseGeoJsonLayerFeatureSummaries,
  parsePlanMapWorkspace,
  parsePlanOperationalDashboard,
  parsePlanSection,
  parsePlanSectionHistoryList,
  parsePlanSections,
  parseCollaborationSummaryResult,
  parseMyNotificationsResult,
  parsePlanSummary,
  parsePlansList,
  parseSectionCommentsResponse,
  parseSavePlanSectionResult,
  parseHazardLayerSummary,
  parseHazardLayerSummaries,
  parseExposureAnalysisJobSummary,
  parseExposureAnalysisJobSummaries,
  parseCreatedExposureAnalysisJob,
  parseExposureSummary,
  parseExposureSummaries
} from "@/lib/plans/plan-parsers";
import type {
  CreateGeoJsonLayerRequest,
  CreatePlanRequest,
  CreatePlanResult,
  CreateSectionCommentRequest,
  CreatedGeoJsonMapAssetSummary,
  HazardLayerSummary,
  RegisterHazardLayerPayload,
  ExposureAnalysisJobSummary,
  CreateExposureAnalysisJobPayload,
  ExposureSummary,
  GeoJsonLayerFeatureSummary,
  PlanMapWorkspaceResult,
  PlanOperationalDashboard,
  PlanSectionDetail,
  PlanSectionHistoryEntry,
  PlanSectionSummary,
  PlanSummary,
  CollaborationSummaryResult,
  MyNotificationsResult,
  RestorePlanSectionRequest,
  SectionCommentSummary,
  SavePlanSectionRequest,
  SavePlanSectionResult,
  UpdatePlanMetadataRequest
} from "@/types/plans";

function sectionCommentsUrl(planId: string, sectionKey: string): string {
  return `/api/plans/${encodeURIComponent(planId)}/sections/${encodeURIComponent(sectionKey)}/comments`;
}

function resolveCommentUrl(commentId: string): string {
  return `/api/section-comments/${encodeURIComponent(commentId)}/resolve`;
}

function reopenCommentUrl(commentId: string): string {
  return `/api/section-comments/${encodeURIComponent(commentId)}/reopen`;
}

function archiveCommentUrl(commentId: string): string {
  return `/api/section-comments/${encodeURIComponent(commentId)}`;
}

export const planClient = {
  async createPlan(request: CreatePlanRequest): Promise<CreatePlanResult> {
    const data = await http.postJson(endpoints.createPlan(), request);
    return parseCreatePlanResult(data);
  },

  async updatePlanMetadata(
    planId: string,
    request: UpdatePlanMetadataRequest
  ): Promise<PlanSummary> {
    const data = await http.putJson(endpoints.planById(planId), request);
    return parsePlanSummary(data);
  },

  async archivePlan(planId: string): Promise<void> {
    await http.deleteVoid(endpoints.archivePlan(planId));
  },

  async getPlans(): Promise<PlanSummary[]> {
    const data = await http.get(endpoints.plansList());
    return parsePlansList(data);
  },

  async getPlanById(planId: string): Promise<PlanSummary> {
    const data = await http.get(endpoints.planById(planId));
    return parsePlanSummary(data);
  },

  async getPlanOperationalDashboard(
    planId: string,
    options?: { recentActivityLimit?: number }
  ): Promise<PlanOperationalDashboard> {
    const suffix =
      options?.recentActivityLimit != null
        ? `?recentActivityLimit=${encodeURIComponent(String(options.recentActivityLimit))}`
        : "";
    const data = await http.get(`/api/plans/${encodeURIComponent(planId)}/operational-dashboard${suffix}`);
    return parsePlanOperationalDashboard(data);
  },

  async getPlanMapWorkspace(planId: string): Promise<PlanMapWorkspaceResult> {
    const data = await http.get(`/api/plans/${encodeURIComponent(planId)}/map-workspace`);
    return parsePlanMapWorkspace(data);
  },

  async createGeoJsonLayer(
    planId: string,
    request: CreateGeoJsonLayerRequest
  ): Promise<CreatedGeoJsonMapAssetSummary> {
    const body = {
      fileAssetId: request.fileAssetId,
      name: request.name,
      mapType: request.mapType,
      description: request.description ?? null,
      geoJson: request.geoJson,
      defaultStyleJson: request.defaultStyleJson ?? undefined,
      boundsJson: request.boundsJson ?? undefined
    };
    const data = await http.postJson(`/api/plans/${encodeURIComponent(planId)}/geojson-layers`, body);
    return parseCreatedGeoJsonMapSummary(data);
  },

  async getGeoJsonLayerFeatures(
    mapAssetId: string,
    options?: { limit?: number }
  ): Promise<readonly GeoJsonLayerFeatureSummary[]> {
    const qs =
      options?.limit != null
        ? `?limit=${encodeURIComponent(String(options.limit))}`
        : "";
    const data = await http.get(`/api/map-assets/${encodeURIComponent(mapAssetId)}/features${qs}`);
    return parseGeoJsonLayerFeatureSummaries(data);
  },

  async archiveMapAsset(mapAssetId: string): Promise<void> {
    await http.deleteVoid(`/api/map-assets/${encodeURIComponent(mapAssetId)}`);
  },

  async getPlanSections(planId: string): Promise<PlanSectionSummary[]> {
    const data = await http.get(endpoints.planSections(planId));
    return parsePlanSections(data);
  },

  async getPlanSectionByKey(planId: string, sectionKey: string): Promise<PlanSectionDetail> {
    const data = await http.get(endpoints.planSectionByKey(planId, sectionKey));
    return parsePlanSection(data);
  },

  async savePlanSection(
    planId: string,
    sectionKey: string,
    request: SavePlanSectionRequest
  ): Promise<SavePlanSectionResult> {
    const data = await http.putJson(endpoints.planSectionByKey(planId, sectionKey), request);
    return parseSavePlanSectionResult(data);
  },

  async getPlanSectionHistory(planId: string, sectionKey: string): Promise<PlanSectionHistoryEntry[]> {
    const data = await http.get(endpoints.planSectionHistory(planId, sectionKey));
    return parsePlanSectionHistoryList(data);
  },

  async restorePlanSection(
    planId: string,
    sectionKey: string,
    request: RestorePlanSectionRequest
  ): Promise<SavePlanSectionResult> {
    const data = await http.postJson(endpoints.restorePlanSection(planId, sectionKey), request);
    return parseSavePlanSectionResult(data);
  },

  async getSectionComments(planId: string, sectionKey: string): Promise<SectionCommentSummary[]> {
    const data = await http.get(sectionCommentsUrl(planId, sectionKey));
    return parseSectionCommentsResponse(data);
  },

  async createSectionComment(
    planId: string,
    sectionKey: string,
    request: CreateSectionCommentRequest
  ): Promise<SectionCommentSummary> {
    const data = await http.postJson(sectionCommentsUrl(planId, sectionKey), request);
    // create returns single comment DTO
    return parseSectionCommentsResponse([data])[0];
  },

  async resolveSectionComment(commentId: string): Promise<void> {
    await http.postJson(resolveCommentUrl(commentId), {});
  },

  async reopenSectionComment(commentId: string): Promise<void> {
    await http.postJson(reopenCommentUrl(commentId), {});
  },

  async archiveSectionComment(commentId: string): Promise<void> {
    await http.deleteVoid(archiveCommentUrl(commentId));
  },

  async getMyNotifications(options?: { limit?: number; unreadOnly?: boolean }): Promise<MyNotificationsResult> {
    const limit = options?.limit;
    const unreadOnly = options?.unreadOnly;
    const qs = [
      limit != null ? `limit=${encodeURIComponent(String(limit))}` : null,
      unreadOnly != null ? `unreadOnly=${encodeURIComponent(String(unreadOnly))}` : null
    ]
      .filter((x): x is string => x !== null)
      .join("&");
    const suffix = qs ? `?${qs}` : "";
    const data = await http.get(`/api/notifications${suffix}`);
    return parseMyNotificationsResult(data);
  },

  async markNotificationRead(notificationId: string): Promise<void> {
    await http.postJson(`/api/notifications/${encodeURIComponent(notificationId)}/read`, {});
  },

  async markAllNotificationsRead(): Promise<number> {
    const data = await http.postJson(`/api/notifications/read-all`, {});
    if (!data || typeof data !== "object") {
      // parsing helpers are optional here; keep defensive to avoid exposing raw payload
      throw new Error("Invalid mark-all response.");
    }
    const updatedCount = (data as { updatedCount?: unknown }).updatedCount;
    if (typeof updatedCount !== "number" || !Number.isFinite(updatedCount)) {
      throw new Error("Invalid updatedCount value.");
    }
    return updatedCount;
  },

  async getCollaborationSummary(): Promise<CollaborationSummaryResult> {
    const data = await http.get(`/api/collaboration/summary`);
    return parseCollaborationSummaryResult(data);
  },

  async getHazardLayers(planId: string): Promise<readonly HazardLayerSummary[]> {
    const data = await http.get(`/api/plans/${encodeURIComponent(planId)}/hazard-layers`);
    return parseHazardLayerSummaries(data);
  },

  async registerHazardLayer(
    planId: string,
    payload: RegisterHazardLayerPayload
  ): Promise<HazardLayerSummary> {
    const body = {
      mapAssetId: payload.mapAssetId,
      name: payload.name,
      hazardType: payload.hazardType,
      severity: payload.severity,
      source: payload.source,
      description: payload.description
    };
    const data = await http.postJson(`/api/plans/${encodeURIComponent(planId)}/hazard-layers`, body);
    return parseHazardLayerSummary(data);
  },

  async getExposureAnalysisJobs(planId: string): Promise<readonly ExposureAnalysisJobSummary[]> {
    const data = await http.get(`/api/plans/${encodeURIComponent(planId)}/exposure-analysis-jobs`);
    return parseExposureAnalysisJobSummaries(data);
  },

  async getExposureAnalysisJob(
    planId: string,
    jobId: string
  ): Promise<ExposureAnalysisJobSummary> {
    const data = await http.get(
      `/api/plans/${encodeURIComponent(planId)}/exposure-analysis-jobs/${encodeURIComponent(jobId)}`
    );
    return parseExposureAnalysisJobSummary(data);
  },

  async createExposureAnalysisJob(
    planId: string,
    payload: CreateExposureAnalysisJobPayload
  ): Promise<ExposureAnalysisJobSummary> {
    const body = { hazardLayerId: payload.hazardLayerId };
    const data = await http.postJson(`/api/plans/${encodeURIComponent(planId)}/exposure-analysis-jobs`, body);
    return parseCreatedExposureAnalysisJob(data);
  },

  async getExposureSummaries(planId: string): Promise<readonly ExposureSummary[]> {
    const data = await http.get(`/api/plans/${encodeURIComponent(planId)}/exposure-summaries`);
    return parseExposureSummaries(data);
  },

  async getExposureSummariesForJob(
    planId: string,
    jobId: string
  ): Promise<readonly ExposureSummary[]> {
    const data = await http.get(
      `/api/plans/${encodeURIComponent(planId)}/exposure-analysis-jobs/${encodeURIComponent(jobId)}/exposure-summaries`
    );
    return parseExposureSummaries(data);
  },

  async getExposureSummary(planId: string, summaryId: string): Promise<ExposureSummary> {
    const data = await http.get(
      `/api/plans/${encodeURIComponent(planId)}/exposure-summaries/${encodeURIComponent(summaryId)}`
    );
    return parseExposureSummary(data);
  }
} as const;
