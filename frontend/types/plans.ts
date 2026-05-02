/**
 * HTTP contract types for plans (no database or server-internal DTOs).
 */

/** Values commonly used in forms; APIs may return additional status strings. */
export type PlanStatus =
  | "Draft"
  | "InProgress"
  | "ReadyForExport"
  | "Submitted"
  | "Approved"
  | "Archived"
  | (string & {});

export type TemplateMode = "New" | "Partial" | "Enhancement" | (string & {});

export interface PlanSummary {
  readonly id: string;
  readonly accountId: string;
  readonly title: string;
  readonly startYear: number;
  readonly endYear: number;
  readonly status: PlanStatus;
  readonly templateMode: TemplateMode;
  readonly versionNumber: number;
  readonly description: string | null;
  readonly createdAtUtc: string | null;
  readonly updatedAtUtc: string | null;
}

export interface CreatePlanRequest {
  readonly title: string;
  readonly startYear: number;
  readonly endYear: number;
  readonly status: PlanStatus;
  readonly templateMode: TemplateMode;
  readonly versionNumber: number;
  readonly description?: string;
}

/** Normalized create response — `planId` is always set after parsing. */
export interface CreatePlanResult {
  readonly planId: string;
  readonly title: string;
  readonly startYear: number;
  readonly endYear: number;
  readonly status: string;
}

export interface PlanSectionSummary {
  readonly id: string;
  readonly planId: string;
  readonly sectionKey: string;
  readonly title: string;
  readonly content: string;
  readonly sortOrder: number;
  readonly lastEditedAtUtc: string | null;
}

/** Full section payload from GET-by-key; structurally matches summary for this API. */
export type PlanSectionDetail = PlanSectionSummary;

export interface SavePlanSectionRequest {
  readonly title: string;
  readonly content: string;
  readonly sortOrder: number;
}

/** Normalized PUT response for a section. */
export interface SavePlanSectionResult {
  readonly sectionId: string;
  readonly lastEditedAtUtc: string;
  readonly lastEditedByUserId?: string;
  // Optional full object fields for backward compatibility
  readonly id?: string;
  readonly planId?: string;
  readonly sectionKey?: string;
  readonly title?: string;
  readonly content?: string;
  readonly sortOrder?: number;
}
