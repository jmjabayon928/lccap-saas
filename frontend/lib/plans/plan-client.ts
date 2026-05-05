import { endpoints } from "@/lib/api/endpoints";
import { http } from "@/lib/api/http";
import {
  parseCreatePlanResult,
  parsePlanSection,
  parsePlanSectionHistoryList,
  parsePlanSections,
  parsePlanSummary,
  parsePlansList,
  parseSavePlanSectionResult
} from "@/lib/plans/plan-parsers";
import type {
  CreatePlanRequest,
  CreatePlanResult,
  PlanSectionDetail,
  PlanSectionHistoryEntry,
  PlanSectionSummary,
  PlanSummary,
  RestorePlanSectionRequest,
  SavePlanSectionRequest,
  SavePlanSectionResult,
  UpdatePlanMetadataRequest
} from "@/types/plans";

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
  }
} as const;
