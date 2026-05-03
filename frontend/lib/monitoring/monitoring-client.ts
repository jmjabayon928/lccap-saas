import { endpoints } from "@/lib/api/endpoints";
import { http } from "@/lib/api/http";
import {
  parseMonitoringIndicator,
  parseMonitoringIndicatorsList
} from "@/lib/monitoring/monitoring-parsers";
import type {
  CreateMonitoringIndicatorRequest,
  MonitoringIndicatorSummary,
  UpdateMonitoringIndicatorRequest
} from "@/types/monitoring";

export const monitoringClient = {
  async getIndicatorsByPlan(planId: string): Promise<MonitoringIndicatorSummary[]> {
    const data = await http.get(endpoints.indicatorsByPlan(planId));
    return parseMonitoringIndicatorsList(data);
  },

  async createIndicator(request: CreateMonitoringIndicatorRequest): Promise<MonitoringIndicatorSummary> {
    const data = await http.postJson(endpoints.monitoringIndicators(), request);
    return parseMonitoringIndicator(data);
  },

  async updateIndicator(
    indicatorId: string,
    request: UpdateMonitoringIndicatorRequest
  ): Promise<MonitoringIndicatorSummary> {
    const data = await http.putJson(endpoints.monitoringIndicatorById(indicatorId), request);
    return parseMonitoringIndicator(data);
  },

  async archiveIndicator(indicatorId: string): Promise<void> {
    await http.deleteVoid(endpoints.archiveMonitoringIndicator(indicatorId));
  }
} as const;
