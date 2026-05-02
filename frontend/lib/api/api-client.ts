import { endpoints } from "@/lib/api/endpoints";
import { http } from "@/lib/api/http";
import type { LoginRequest, LoginResponse } from "@/lib/auth/auth-types";
import { parseLoginResponse } from "@/lib/auth/auth-types";
import { actionClient } from "@/lib/actions/action-client";
import { documentClient } from "@/lib/documents/document-client";
import { exportClient } from "@/lib/exports/export-client";
import { monitoringClient } from "@/lib/monitoring/monitoring-client";
import { planClient } from "@/lib/plans/plan-client";

export const apiClient = {
  async login(request: LoginRequest): Promise<LoginResponse> {
    const data = await http.postJson(endpoints.authLogin(), request);
    return parseLoginResponse(data);
  },

  plans: planClient,
  documents: documentClient,
  actionClient,
  monitoringClient,
  exportClient
} as const;

export { actionClient, documentClient, exportClient, monitoringClient, planClient };
