"use client";

import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import type { AuditLogFilters } from "@/types/audit";
import { useState } from "react";

interface Props {
  onFilter: (filters: AuditLogFilters) => void;
  isLoading?: boolean;
}

export function AuditLogFilters({ onFilter, isLoading }: Props) {
  const [entityName, setEntityName] = useState("");
  const [action, setAction] = useState("");
  const [fromUtc, setFromUtc] = useState("");
  const [toUtc, setToUtc] = useState("");

  const handleApply = () => {
    onFilter({
      entityName: entityName || undefined,
      action: action || undefined,
      fromUtc: fromUtc || undefined,
      toUtc: toUtc || undefined,
      page: 1, // Reset to first page on filter change
    });
  };

  const handleReset = () => {
    setEntityName("");
    setAction("");
    setFromUtc("");
    setToUtc("");
    onFilter({ page: 1 });
  };

  return (
    <div className="grid grid-cols-1 md:grid-cols-4 gap-4 bg-slate-50 p-4 rounded-lg border border-slate-200 mb-6">
      <div className="space-y-2">
        <Label htmlFor="entityName">Entity Name</Label>
        <Input
          id="entityName"
          placeholder="e.g. Plan, Document"
          value={entityName}
          onChange={(e) => setEntityName(e.target.value)}
        />
      </div>
      <div className="space-y-2">
        <Label htmlFor="action">Action</Label>
        <Input
          id="action"
          placeholder="e.g. PlanMetadataUpdated"
          value={action}
          onChange={(e) => setAction(e.target.value)}
        />
      </div>
      <div className="space-y-2">
        <Label htmlFor="fromUtc">From Date</Label>
        <Input
          id="fromUtc"
          type="date"
          value={fromUtc}
          onChange={(e) => setFromUtc(e.target.value)}
        />
      </div>
      <div className="space-y-2">
        <Label htmlFor="toUtc">To Date</Label>
        <Input
          id="toUtc"
          type="date"
          value={toUtc}
          onChange={(e) => setToUtc(e.target.value)}
        />
      </div>
      <div className="md:col-span-4 flex justify-end gap-2 pt-2">
        <Button variant="outline" onClick={handleReset} disabled={isLoading}>
          Reset
        </Button>
        <Button onClick={handleApply} disabled={isLoading}>
          Apply Filters
        </Button>
      </div>
    </div>
  );
}
