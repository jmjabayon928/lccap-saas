/**
 * HTTP-oriented monitoring indicator types for the LCCAP API client (no database DTOs).
 */

export type MonitoringStatus =
  | "NotStarted"
  | "InProgress"
  | "OnTrack"
  | "Delayed"
  | "Completed";

export interface MonitoringIndicatorSummary {
  readonly id: string;
  readonly planId: string;
  /** Optional link to a plan action item when the API provides it. */
  readonly actionItemId: string | null;
  /** Base64 row version from API (optimistic concurrency). */
  readonly rowVersion: string;
  readonly name: string;
  readonly description: string | null;
  readonly unit: string | null;
  readonly baselineValue: number | null;
  readonly targetValue: number | null;
  readonly currentValue: number | null;
  readonly progressPercent: number | null;
  readonly status: MonitoringStatus;
  readonly frequency: string | null;
  readonly responsibleOffice: string | null;
  readonly lastUpdatedAtUtc: string | null;
  readonly createdAtUtc: string | null;
  readonly updatedAtUtc: string | null;
}

export type MonitoringIndicatorDetail = MonitoringIndicatorSummary;

export interface CreateMonitoringIndicatorRequest {
  readonly planId: string;
  readonly name: string;
  readonly description: string | null;
  readonly unit: string | null;
  readonly baselineValue: number | null;
  readonly targetValue: number | null;
  readonly currentValue: number | null;
  readonly progressPercent: number | null;
  readonly status: MonitoringStatus;
  readonly frequency: string | null;
  readonly responsibleOffice: string | null;
}

export interface UpdateMonitoringIndicatorRequest {
  readonly name: string;
  readonly description: string | null;
  readonly unit: string | null;
  readonly baselineValue: number | null;
  readonly targetValue: number | null;
  readonly currentValue: number | null;
  readonly progressPercent: number | null;
  readonly status: MonitoringStatus;
  readonly frequency: string | null;
  readonly responsibleOffice: string | null;
  readonly rowVersion: string;
}

export interface SaveMonitoringIndicatorResult {
  readonly id: string;
  readonly rowVersion?: string;
  // Optional full indicator fields for backward compatibility
  readonly planId?: string;
  readonly name?: string;
  readonly description?: string | null;
  readonly unit?: string | null;
  readonly baselineValue?: number | null;
  readonly targetValue?: number | null;
  readonly currentValue?: number | null;
  readonly progressPercent?: number | null;
  readonly status?: MonitoringStatus;
  readonly frequency?: string | null;
  readonly responsibleOffice?: string | null;
  readonly lastUpdatedAtUtc?: string | null;
  readonly createdAtUtc?: string | null;
  readonly updatedAtUtc?: string | null;
}
