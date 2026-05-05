export interface AuditLogListItem {
  id: string;
  accountId: string | null;
  userId: string | null;
  userEmail: string | null;
  userFullName: string | null;
  entityName: string;
  entityId: string | null;
  action: string;
  oldValuesJson: Record<string, unknown> | null;
  newValuesJson: Record<string, unknown> | null;
  metadataJson: Record<string, unknown>;
  ipAddress: string | null;
  createdAtUtc: string;
}

export interface AuditLogPagedResult {
  items: AuditLogListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface AuditLogFilters {
  entityName?: string;
  action?: string;
  userId?: string;
  planId?: string;
  fromUtc?: string;
  toUtc?: string;
  page?: number;
  pageSize?: number;
}
