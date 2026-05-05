"use client";

import type { AuditLogListItem } from "@/types/audit";
import { useState } from "react";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Button } from "@/components/ui/button";
import { AuditLogDetail } from "./audit-log-detail";
import { ChevronDown, ChevronUp } from "lucide-react";

interface Props {
  logs: AuditLogListItem[];
  isLoading?: boolean;
}

export function AuditLogList({ logs, isLoading }: Props) {
  const [expandedId, setExpandedId] = useState<string | null>(null);

  const toggleExpand = (id: string) => {
    setExpandedId(expandedId === id ? null : id);
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

  if (isLoading) {
    return (
      <div className="flex justify-center py-12">
        <p className="text-muted-foreground animate-pulse">Loading audit history...</p>
      </div>
    );
  }

  if (logs.length === 0) {
    return (
      <div className="text-center py-12 bg-slate-50 rounded-lg border border-dashed border-slate-300">
        <p className="text-muted-foreground">No audit records found.</p>
      </div>
    );
  }

  return (
    <div className="rounded-md border border-slate-200 overflow-hidden bg-white">
      <Table>
        <TableHeader className="bg-slate-50">
          <TableRow>
            <TableHead className="w-[180px]">Timestamp</TableHead>
            <TableHead>Action</TableHead>
            <TableHead>Entity</TableHead>
            <TableHead>Changed By</TableHead>
            <TableHead className="text-right">Details</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {logs.map((log) => (
            <React.Fragment key={log.id}>
              <TableRow className="hover:bg-slate-50/50 cursor-pointer" onClick={() => toggleExpand(log.id)}>
                <TableCell className="text-xs text-slate-600">
                  {formatDate(log.createdAtUtc)}
                </TableCell>
                <TableCell className="font-medium text-slate-900">
                  {log.action}
                </TableCell>
                <TableCell className="text-sm text-slate-700">
                  {log.entityName}
                </TableCell>
                <TableCell className="text-sm text-slate-700">
                  {log.userFullName || log.userEmail || "System"}
                </TableCell>
                <TableCell className="text-right">
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-8 w-8 p-0"
                  >
                    {expandedId === log.id ? (
                      <ChevronUp className="h-4 w-4" />
                    ) : (
                      <ChevronDown className="h-4 w-4" />
                    )}
                  </Button>
                </TableCell>
              </TableRow>
              {expandedId === log.id && (
                <TableRow className="bg-slate-50/30">
                  <TableCell colSpan={5} className="p-4">
                    <AuditLogDetail log={log} />
                  </TableCell>
                </TableRow>
              )}
            </React.Fragment>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}

// Add React import for Fragment if not already available via global or other means
import React from "react";
