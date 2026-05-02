import { ApiError } from "@/lib/api/api-error";
import { endpoints } from "@/lib/api/endpoints";
import { http } from "@/lib/api/http";
import { config } from "@/lib/config";
import { parseCreatePdfExportResult, parseExportJobSummary } from "@/lib/exports/export-parsers";
import { getAccessToken } from "@/lib/auth/auth-storage";
import type { CreatePdfExportResult, ExportJobSummary } from "@/types/exports";

function buildAbsoluteUrl(path: string): string {
  const base = config.apiBaseUrl;
  const prefix = path.startsWith("/") ? path : `/${path}`;
  return `${base}${prefix}`;
}

function authHeaders(): HeadersInit {
  const token = getAccessToken();
  if (!token) {
    return {};
  }
  return { Authorization: `Bearer ${token}` };
}

async function fetchAuthorizedBlob(path: string, signal?: AbortSignal): Promise<Blob> {
  const headers: HeadersInit = {
    ...authHeaders(),
    Accept: "application/pdf,application/octet-stream,*/*"
  };

  const response = await fetch(buildAbsoluteUrl(path), {
    method: "GET",
    headers,
    credentials: "omit",
    signal
  });

  if (!response.ok) {
    throw await ApiError.fromResponse(response);
  }

  return response.blob();
}

export const exportClient = {
  async createPdfExport(planId: string): Promise<CreatePdfExportResult> {
    const data = await http.postJson(endpoints.createPdfExport(planId), {});
    return parseCreatePdfExportResult(data);
  },

  async getExportJob(exportJobId: string): Promise<ExportJobSummary> {
    const data = await http.get(endpoints.exportById(exportJobId));
    return parseExportJobSummary(data);
  },

  async downloadExport(exportJobId: string, signal?: AbortSignal): Promise<Blob> {
    return fetchAuthorizedBlob(endpoints.exportDownload(exportJobId), signal);
  }
} as const;
