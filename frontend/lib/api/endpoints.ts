/**
 * Central route helpers for the LCCAP HTTP API. Paths are relative to `config.apiBaseUrl`.
 * Dynamic segments are encoded with encodeURIComponent.
 */

const api = {
  authLogin: () => `/api/auth/login`,

  /** POST /api/plans */
  createPlan: () => `/api/plans`,
  /** GET /api/plans */
  plansList: () => `/api/plans`,
  planById: (planId: string) => `/api/plans/${encodeURIComponent(planId)}`,

  planSections: (planId: string) => `/api/plans/${encodeURIComponent(planId)}/sections`,
  planSectionByKey: (planId: string, sectionKey: string) =>
    `/api/plans/${encodeURIComponent(planId)}/sections/${encodeURIComponent(sectionKey)}`,

  /** POST /api/documents/upload (multipart/form-data) */
  uploadDocument: () => `/api/documents/upload`,
  documentsByPlan: (planId: string) => `/api/plans/${encodeURIComponent(planId)}/documents`,

  /** PUT /api/documents/{documentId}/metadata */
  updateDocumentMetadata: (documentId: string) =>
    `/api/documents/${encodeURIComponent(documentId)}/metadata`,
  /** DELETE /api/documents/{documentId} — archives (soft delete) document row */
  archiveDocument: (documentId: string) => `/api/documents/${encodeURIComponent(documentId)}`,

  /** GET/POST /api/plans/{planId}/actions */
  actionsByPlan: (planId: string) => `/api/plans/${encodeURIComponent(planId)}/actions`,
  /** GET/PUT /api/actions/{actionItemId} */
  actionById: (actionItemId: string) => `/api/actions/${encodeURIComponent(actionItemId)}`,

  monitoringIndicators: () => `/api/monitoring/indicators`,
  monitoringIndicatorById: (indicatorId: string) =>
    `/api/monitoring/indicators/${encodeURIComponent(indicatorId)}`,
  indicatorsByPlan: (planId: string) => `/api/monitoring/plans/${encodeURIComponent(planId)}/indicators`,

  /** POST /api/plans/{planId}/exports/pdf */
  createPdfExport: (planId: string) => `/api/plans/${encodeURIComponent(planId)}/exports/pdf`,
  exportById: (exportJobId: string) => `/api/exports/${encodeURIComponent(exportJobId)}`,
  exportDownload: (exportJobId: string) => `/api/exports/${encodeURIComponent(exportJobId)}/download`
} as const;

export const endpoints = api;
