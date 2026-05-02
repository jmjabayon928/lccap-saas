/**
 * Shared types for JSON payloads and API client usage (no database or backend-internal DTOs).
 */

export type JsonPrimitive = string | number | boolean | null;

export type JsonValue = JsonPrimitive | JsonValue[] | { readonly [key: string]: JsonValue };

export type {
  CreatePlanRequest,
  CreatePlanResult,
  PlanSectionDetail,
  PlanSectionSummary,
  PlanStatus,
  PlanSummary,
  SavePlanSectionRequest,
  SavePlanSectionResult,
  TemplateMode
} from "@/types/plans";

export type {
  DocumentCategory,
  DocumentSummary,
  UploadDocumentRequest,
  UploadDocumentResult
} from "@/types/documents";

export type {
  ActionItemDetail,
  ActionItemSummary,
  ActionStatus,
  ActionType,
  CreateActionItemRequest,
  SaveActionItemResult,
  UpdateActionItemRequest
} from "@/types/actions";

export type {
  CreateMonitoringIndicatorRequest,
  MonitoringIndicatorDetail,
  MonitoringIndicatorSummary,
  MonitoringStatus,
  SaveMonitoringIndicatorResult,
  UpdateMonitoringIndicatorRequest
} from "@/types/monitoring";

export type {
  CreatePdfExportResult,
  ExportJobSummary,
  ExportStatus,
  ExportType
} from "@/types/exports";
