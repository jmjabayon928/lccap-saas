import { endpoints } from "@/lib/api/endpoints";
import { http } from "@/lib/api/http";
import {
  parseCreatePlanResult,
  parsePlanSection,
  parsePlanSections,
  parsePlanSummary,
  parseSavePlanSectionResult
} from "@/lib/plans/plan-parsers";
import type {
  CreatePlanRequest,
  CreatePlanResult,
  PlanSectionDetail,
  PlanSectionSummary,
  PlanSummary,
  SavePlanSectionRequest,
  SavePlanSectionResult
} from "@/types/plans";

export const planClient = {
  async createPlan(request: CreatePlanRequest): Promise<CreatePlanResult> {
    const data = await http.postJson(endpoints.createPlan(), request);
    return parseCreatePlanResult(data);
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
  }
} as const;
