"use client";

import { useEffect, useState, useCallback } from "react";
import { History, RefreshCw, User, Clock, FileText, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { planClient } from "@/lib/plans/plan-client";
import { isApiError } from "@/lib/api/api-error";
import { SectionRestoreButton } from "./section-restore-button";
import type { PlanSectionHistoryEntry, SavePlanSectionResult } from "@/types/plans";

export interface SectionHistoryPanelProps {
  readonly planId: string;
  readonly sectionKey: string;
  readonly onRestored: (result: SavePlanSectionResult, restoredTitle: string, restoredContent: string) => void;
}

export function SectionHistoryPanel({ planId, sectionKey, onRestored }: SectionHistoryPanelProps) {
  const [history, setHistory] = useState<PlanSectionHistoryEntry[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isOpen, setIsOpen] = useState(false);

  const loadHistory = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage(null);
    try {
      const data = await planClient.getPlanSectionHistory(planId, sectionKey);
      setHistory(data);
    } catch (err) {
      if (isApiError(err)) {
        setErrorMessage(err.message);
      } else {
        setErrorMessage("Failed to load history.");
      }
    } finally {
      setIsLoading(false);
    }
  }, [planId, sectionKey]);

  useEffect(() => {
    if (isOpen) {
      void loadHistory();
    }
  }, [isOpen, loadHistory]);

  return (
    <>
      <Button variant="ghost" size="sm" className="gap-2" onClick={() => setIsOpen(true)}>
        <History className="h-4 w-4" />
        Revision history
      </Button>

      {isOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4 sm:p-6">
          <div 
            className="fixed inset-0" 
            onClick={() => setIsOpen(false)} 
            aria-hidden="true"
          />
          <Card className="relative w-full max-w-lg border-border shadow-xl">
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <div className="space-y-1">
                <CardTitle className="flex items-center gap-2 text-lg">
                  <History className="h-5 w-5" />
                  Revision history
                </CardTitle>
                <CardDescription>
                  View and restore previous versions of this section.
                </CardDescription>
              </div>
              <Button 
                variant="ghost" 
                size="sm" 
                onClick={() => setIsOpen(false)} 
                className="h-8 w-8 rounded-full"
              >
                <X className="h-4 w-4" />
                <span className="sr-only">Close</span>
              </Button>
            </CardHeader>
            <CardContent>
              <div className="mt-2 max-h-[60vh] overflow-y-auto pr-2">
                {isLoading ? (
                  <div className="flex h-32 flex-col items-center justify-center gap-2 text-muted-foreground">
                    <RefreshCw className="h-6 w-6 animate-spin text-emerald-600" />
                    <p className="text-sm">Loading history...</p>
                  </div>
                ) : errorMessage ? (
                  <div className="flex h-32 flex-col items-center justify-center gap-2 text-center text-sm text-destructive">
                    <p>{errorMessage}</p>
                    <Button variant="outline" size="sm" onClick={() => void loadHistory()}>
                      Retry
                    </Button>
                  </div>
                ) : history.length === 0 ? (
                  <div className="flex h-32 flex-col items-center justify-center text-center text-sm text-muted-foreground">
                    <p>No revisions yet.</p>
                    <p className="mt-1">Save this section to create the first revision entry.</p>
                  </div>
                ) : (
                  <div className="space-y-6 py-2">
                    {history.map((entry) => (
                      <div key={entry.auditLogId} className="relative border-l-2 border-muted pl-4 pb-1">
                        <div className="absolute -left-[9px] top-0 h-4 w-4 rounded-full border-2 border-background bg-muted" />
                        
                        <div className="flex flex-col gap-2">
                          <div className="flex items-center justify-between gap-2">
                            <Badge variant={entry.action === "PlanSectionRestored" ? "secondary" : "outline"}>
                              {entry.action === "PlanSectionRestored" ? "Restored" : "Updated"}
                            </Badge>
                            <span className="flex items-center gap-1 text-xs text-muted-foreground">
                              <Clock className="h-3 w-3" />
                              {new Date(entry.createdAtUtc).toLocaleString()}
                            </span>
                          </div>

                          <div className="space-y-1">
                            <p className="text-sm font-medium leading-none">{entry.title}</p>
                            <div className="flex items-center gap-1 text-xs text-muted-foreground">
                              <User className="h-3 w-3" />
                              {entry.userId ? `User: ${entry.userId.substring(0, 8)}...` : "Unknown user"}
                            </div>
                          </div>

                          {entry.content && (
                            <div className="rounded bg-muted/50 p-2 text-xs text-muted-foreground">
                              <div className="flex items-center gap-1 mb-1 font-medium">
                                <FileText className="h-3 w-3" />
                                Content preview
                              </div>
                              <p className="line-clamp-3 whitespace-pre-wrap">{entry.content}</p>
                            </div>
                          )}

                          {entry.canRestore && (
                            <div className="mt-1">
                              <SectionRestoreButton
                                planId={planId}
                                sectionKey={sectionKey}
                                entry={entry}
                                onRestored={(result, title, content) => {
                                  onRestored(result, title, content);
                                  setIsOpen(false);
                                }}
                              />
                            </div>
                          )}
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
              <div className="mt-4 flex justify-end border-t pt-4">
                <Button variant="outline" onClick={() => setIsOpen(false)}>
                  Close
                </Button>
              </div>
            </CardContent>
          </Card>
        </div>
      )}
    </>
  );
}
