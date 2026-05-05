"use client";

import { useEffect, useState, useCallback } from "react";
import { useAuthSession } from "@/lib/auth/use-auth-session";
import { getAuditLogs } from "@/lib/audit/audit-client";
import type { AuditLogFilters, AuditLogPagedResult } from "@/types/audit";
import { AuditLogList } from "@/components/audit/audit-log-list";
import { AuditLogFilters as AuditLogFiltersComponent } from "@/components/audit/audit-log-filters";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { AlertCircle, ChevronLeft, ChevronRight } from "lucide-react";
import { Button } from "@/components/ui/button";

interface ApiError {
  status?: number;
  message?: string;
}

export default function AuditPage() {
  const { isAuthenticated, isLoading: sessionLoading } = useAuthSession();
  const [data, setData] = useState<AuditLogPagedResult | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filters, setFilters] = useState<AuditLogFilters>({
    page: 1,
    pageSize: 25,
  });

  const loadAuditLogs = useCallback(async (currentFilters: AuditLogFilters) => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await getAuditLogs(currentFilters);
      setData(result);
    } catch (err: unknown) {
      const apiError = err as ApiError;
      if (apiError.status === 403) {
        setError("You do not have permission to view audit history.");
      } else {
        setError("Failed to load audit history. Please try again later.");
      }
      console.error("Audit load error:", err);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    if (isAuthenticated) {
      loadAuditLogs(filters);
    }
  }, [isAuthenticated, filters, loadAuditLogs]);

  if (sessionLoading) {
    return <div className="p-8">Checking authorization...</div>;
  }

  if (!isAuthenticated) {
    return <div className="p-8">Please log in to view this page.</div>;
  }

  const handleFilterChange = (newFilters: AuditLogFilters) => {
    setFilters((prev) => ({ ...prev, ...newFilters, page: newFilters.page ?? 1 }));
  };

  const handlePageChange = (newPage: number) => {
    setFilters((prev) => ({ ...prev, page: newPage }));
  };

  const totalPages = data ? Math.ceil(data.totalCount / data.pageSize) : 0;

  return (
    <div className="container mx-auto py-8 px-4 max-w-6xl">
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-slate-900">Audit History</h1>
        <p className="text-muted-foreground">
          Review accountability records and system changes for your LGU workspace.
        </p>
      </div>

      {error ? (
        <Alert variant="destructive" className="mb-6">
          <AlertCircle className="h-4 w-4" />
          <AlertTitle>Access Denied</AlertTitle>
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      ) : (
        <>
          <AuditLogFiltersComponent onFilter={handleFilterChange} isLoading={isLoading} />
          
          <AuditLogList logs={data?.items ?? []} isLoading={isLoading} />

          {data && data.totalCount > data.pageSize && (
            <div className="mt-6 flex items-center justify-between bg-white border border-slate-200 p-4 rounded-md">
              <p className="text-sm text-slate-600">
                Showing <span className="font-medium">{(data.page - 1) * data.pageSize + 1}</span> to{" "}
                <span className="font-medium">
                  {Math.min(data.page * data.pageSize, data.totalCount)}
                </span> of{" "}
                <span className="font-medium">{data.totalCount}</span> results
              </p>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => handlePageChange(data.page - 1)}
                  disabled={data.page <= 1 || isLoading}
                >
                  <ChevronLeft className="h-4 w-4 mr-1" />
                  Previous
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => handlePageChange(data.page + 1)}
                  disabled={data.page >= totalPages || isLoading}
                >
                  Next
                  <ChevronRight className="h-4 w-4 ml-1" />
                </Button>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
}
