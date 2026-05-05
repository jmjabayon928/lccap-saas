import { endpoints } from "@/lib/api/endpoints";
import { http } from "@/lib/api/http";
import type { AuditLogFilters, AuditLogPagedResult } from "@/types/audit";
import { parseAuditLogPagedResult } from "./audit-parsers";

export async function getAuditLogs(filters: AuditLogFilters): Promise<AuditLogPagedResult> {
  const queryParams = new URLSearchParams();
  
  if (filters.entityName) queryParams.set("entityName", filters.entityName);
  if (filters.action) queryParams.set("action", filters.action);
  if (filters.userId) queryParams.set("userId", filters.userId);
  if (filters.planId) queryParams.set("planId", filters.planId);
  if (filters.fromUtc) queryParams.set("fromUtc", filters.fromUtc);
  if (filters.toUtc) queryParams.set("toUtc", filters.toUtc);
  if (filters.page) queryParams.set("page", filters.page.toString());
  if (filters.pageSize) queryParams.set("pageSize", filters.pageSize.toString());

  const queryString = queryParams.toString();
  const url = `${endpoints.auditLogs()}${queryString ? `?${queryString}` : ""}`;

  const response = await http.get(url);
  return parseAuditLogPagedResult(response);
}
