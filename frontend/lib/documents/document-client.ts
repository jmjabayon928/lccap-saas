import { endpoints } from "@/lib/api/endpoints";
import { http } from "@/lib/api/http";
import { parseDocumentsList, parseUploadDocumentResult } from "@/lib/documents/document-parsers";
import type { DocumentSummary, UploadDocumentRequest, UploadDocumentResult } from "@/types/documents";

/** Maps upload API result into list row shape (timestamps approximated client-side until next GET). */
export function uploadResultToSummary(
  result: UploadDocumentResult,
  request: UploadDocumentRequest
): DocumentSummary {
  return {
    id: result.id,
    planId: result.planId ?? request.planId,
    fileAssetId: result.fileAssetId ?? null,
    title: result.title ?? request.title,
    category: result.category ?? request.category,
    description: request.description ?? null,
    originalFileName: result.originalFileName ?? request.file.name,
    contentType: result.contentType ?? (request.file.type || "application/octet-stream"),
    sizeBytes: result.sizeBytes ?? request.file.size,
    uploadedAtUtc: new Date().toISOString(),
    createdAtUtc: new Date().toISOString()
  };
}

export const documentClient = {
  async getDocumentsByPlan(planId: string): Promise<DocumentSummary[]> {
    const data = await http.get(endpoints.documentsByPlan(planId));
    return parseDocumentsList(data);
  },

  async uploadDocument(request: UploadDocumentRequest): Promise<UploadDocumentResult> {
    const body = new FormData();
    body.append("planId", request.planId);
    body.append("category", request.category);
    body.append("title", request.title);
    const desc = request.description?.trim();
    if (desc) {
      body.append("description", desc);
    }
    body.append("file", request.file);

    const data = await http.postFormData(endpoints.uploadDocument(), body);
    return parseUploadDocumentResult(data);
  }
} as const;
