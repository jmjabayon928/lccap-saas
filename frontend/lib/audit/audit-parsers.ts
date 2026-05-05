import { ApiError } from "@/lib/api/api-error";
import type { AuditLogListItem, AuditLogPagedResult } from "@/types/audit";

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function isNonEmptyString(value: unknown): value is string {
  return typeof value === "string" && value.trim().length > 0;
}

function isFiniteNumber(value: unknown): value is number {
  return typeof value === "number" && Number.isFinite(value);
}

export function parseAuditLogListItem(payload: unknown): AuditLogListItem {
  if (!isRecord(payload)) {
    throw new ApiError("Invalid audit log item: expected object", 502, payload);
  }

  const {
    id,
    accountId,
    userId,
    userEmail,
    userFullName,
    entityName,
    entityId,
    action,
    oldValuesJson,
    newValuesJson,
    metadataJson,
    ipAddress,
    createdAtUtc
  } = payload;

  if (!isNonEmptyString(id)) {
    throw new ApiError("Invalid audit log item: missing id", 502, payload);
  }
  if (!isNonEmptyString(entityName)) {
    throw new ApiError("Invalid audit log item: missing entityName", 502, payload);
  }
  if (!isNonEmptyString(action)) {
    throw new ApiError("Invalid audit log item: missing action", 502, payload);
  }
  if (!isNonEmptyString(createdAtUtc)) {
    throw new ApiError("Invalid audit log item: missing createdAtUtc", 502, payload);
  }

  return {
    id,
    accountId: typeof accountId === "string" ? accountId : null,
    userId: typeof userId === "string" ? userId : null,
    userEmail: typeof userEmail === "string" ? userEmail : null,
    userFullName: typeof userFullName === "string" ? userFullName : null,
    entityName,
    entityId: typeof entityId === "string" ? entityId : null,
    action,
    oldValuesJson: isRecord(oldValuesJson) ? oldValuesJson : null,
    newValuesJson: isRecord(newValuesJson) ? newValuesJson : null,
    metadataJson: isRecord(metadataJson) ? metadataJson : {},
    ipAddress: typeof ipAddress === "string" ? ipAddress : null,
    createdAtUtc
  };
}

export function parseAuditLogPagedResult(payload: unknown): AuditLogPagedResult {
  if (!isRecord(payload)) {
    throw new ApiError("Invalid audit log paged result: expected object", 502, payload);
  }

  const { items, page, pageSize, totalCount } = payload;

  if (!Array.isArray(items)) {
    throw new ApiError("Invalid audit log paged result: missing items array", 502, payload);
  }
  if (!isFiniteNumber(page)) {
    throw new ApiError("Invalid audit log paged result: missing page", 502, payload);
  }
  if (!isFiniteNumber(pageSize)) {
    throw new ApiError("Invalid audit log paged result: missing pageSize", 502, payload);
  }
  if (!isFiniteNumber(totalCount)) {
    throw new ApiError("Invalid audit log paged result: missing totalCount", 502, payload);
  }

  return {
    items: items.map((item, i) => {
      try {
        return parseAuditLogListItem(item);
      } catch {
        throw new ApiError(`Invalid audit log item at index ${i}`, 502, payload);
      }
    }),
    page,
    pageSize,
    totalCount
  };
}
