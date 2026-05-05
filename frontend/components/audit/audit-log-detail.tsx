"use client";

import type { AuditLogListItem } from "@/types/audit";

interface Props {
  log: AuditLogListItem;
}

export function AuditLogDetail({ log }: Props) {
  const renderJson = (title: string, data: Record<string, unknown> | null) => {
    if (!data || Object.keys(data).length === 0) return null;

    return (
      <div className="mt-4">
        <h4 className="text-sm font-semibold text-slate-900 mb-2">{title}</h4>
        <div className="bg-slate-50 border border-slate-200 rounded p-3 overflow-auto max-h-60">
          <pre className="text-xs text-slate-700 font-mono whitespace-pre-wrap">
            {JSON.stringify(data, null, 2)}
          </pre>
        </div>
      </div>
    );
  };

  const formatDate = (dateStr: string) => {
    try {
      return new Date(dateStr).toLocaleString(undefined, {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit'
      });
    } catch {
      return dateStr;
    }
  };

  return (
    <div className="space-y-4 p-4 bg-white rounded-lg border border-slate-200">
      <div className="grid grid-cols-1 md:grid-cols-2 gap-x-8 gap-y-4 text-sm">
        <div>
          <p className="text-muted-foreground text-xs uppercase tracking-wider font-semibold">Action</p>
          <p className="font-medium text-slate-900">{log.action}</p>
        </div>
        <div>
          <p className="text-muted-foreground text-xs uppercase tracking-wider font-semibold">Entity</p>
          <p className="font-medium text-slate-900">
            {log.entityName} ({log.entityId || "N/A"})
          </p>
        </div>
        <div>
          <p className="text-muted-foreground text-xs uppercase tracking-wider font-semibold">Changed By</p>
          <p className="font-medium text-slate-900">
            {log.userFullName || log.userEmail || "System/Unknown"}
          </p>
        </div>
        <div>
          <p className="text-muted-foreground text-xs uppercase tracking-wider font-semibold">Timestamp</p>
          <p className="font-medium text-slate-900">
            {formatDate(log.createdAtUtc)}
          </p>
        </div>
        {log.ipAddress && (
          <div>
            <p className="text-muted-foreground text-xs uppercase tracking-wider font-semibold">IP Address</p>
            <p className="font-medium text-slate-900">{log.ipAddress}</p>
          </div>
        )}
      </div>

      {renderJson("Metadata", log.metadataJson)}
      {renderJson("Old Values", log.oldValuesJson)}
      {renderJson("New Values", log.newValuesJson)}
    </div>
  );
}
