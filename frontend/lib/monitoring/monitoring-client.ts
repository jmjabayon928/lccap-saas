import { endpoints } from "@/lib/api/endpoints";
import { http } from "@/lib/api/http";
import {
  parseMonitoringIndicatorsList,
  parseSaveMonitoringIndicatorResult
} from "@/lib/monitoring/monitoring-parsers";
import type {
  CreateMonitoringIndicatorRequest,
  MonitoringIndicatorSummary,
  SaveMonitoringIndicatorResult,
  UpdateMonitoringIndicatorRequest
} from "@/types/monitoring";

export const monitoringClient = {
  async getIndicatorsByPlan(planId: string): Promise<MonitoringIndicatorSummary[]> {
    const data = await http.get(endpoints.indicatorsByPlan(planId));
    return parseMonitoringIndicatorsList(data);
  },

  async createIndicator(request: CreateMonitoringIndicatorRequest): Promise<SaveMonitoringIndicatorResult> {
    const data = await http.postJson(endpoints.monitoringIndicators(), request);
    return parseSaveMonitoringIndicatorResult(data);
  },

  async updateIndicator(
    indicatorId: string,
    request: UpdateMonitoringIndicatorRequest
  ): Promise<SaveMonitoringIndicatorResult> {
    const data = await http.putJson(endpoints.monitoringIndicatorById(indicatorId), request);
    return parseSaveMonitoringIndicatorResult(data);
  }
} as const;
