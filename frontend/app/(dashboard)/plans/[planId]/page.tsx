"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { ArrowLeft, RefreshCw } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button, buttonVariants } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { DocumentUploadForm } from "@/components/documents/document-upload-form";
import { DocumentsList } from "@/components/documents/documents-list";
import { PlanSectionEditor } from "@/components/plans/plan-section-editor";
import { PlanSectionsPreview } from "@/components/plans/plan-sections-preview";
import { PlanSummaryCard } from "@/components/plans/plan-summary-card";
import { SectionCommentsPanel } from "@/components/plans/section-comments-panel";
import { ActionForm } from "@/components/actions/action-form";
import { ActionFundingReadinessPanel } from "@/components/actions/action-funding-readiness-panel";
import { ActionItemsList } from "@/components/actions/action-items-list";
import { CcetCatalogPanel } from "@/components/actions/ccet-catalog-panel";
import { FundingProgramCatalogPanel } from "@/components/actions/funding-program-catalog-panel";
import { FundingSourceCatalogPanel } from "@/components/actions/funding-source-catalog-panel";
import { ActionFundingAllocationPanel } from "@/components/actions/action-funding-allocation-panel";
import { IndicatorForm } from "@/components/monitoring/indicator-form";
import { IndicatorsList } from "@/components/monitoring/indicators-list";
import { MonitoringUpdateForm } from "@/components/monitoring/monitoring-update-form";
import { MonitoringUpdateHistory } from "@/components/monitoring/monitoring-update-history";
import { ExportPanel } from "@/components/exports/export-panel";
import { EvidenceIndexPanel } from "@/components/exports/evidence-index-panel";
import { isApiError } from "@/lib/api/api-error";
import { actionClient } from "@/lib/actions/action-client";
import { documentClient } from "@/lib/documents/document-client";
import { monitoringClient } from "@/lib/monitoring/monitoring-client";
import { planClient } from "@/lib/plans/plan-client";
import type { DocumentSummary } from "@/types/documents";
import type {
  ActionFundingAllocationSummary,
  ActionItemSummary,
  ClimateExpenditureTagsResult,
  FundingProgramsLoadState,
  FundingSourcesLoadState,
  SaveActionItemResult
} from "@/types/actions";
import type {
  MonitoringIndicatorSummary,
  MonitoringUpdateSummary
} from "@/types/monitoring";
import type { PlanSectionSummary, PlanSummary } from "@/types/plans";

type LoadState =
  | { status: "loading" }
  | { status: "ready"; plan: PlanSummary; sections: PlanSectionSummary[] }
  | { status: "error"; message: string; notFound: boolean; retryable: boolean };

type DocsState =
  | { status: "idle" }
  | { status: "loading" }
  | { status: "ready"; items: DocumentSummary[] }
  | { status: "error"; message: string };

type ActionsState =
  | { status: "idle" }
  | { status: "loading" }
  | { status: "ready"; items: ActionItemSummary[] }
  | { status: "error"; message: string };

type CcetState =
  | { status: "idle" }
  | { status: "loading" }
  | { status: "ready"; data: ClimateExpenditureTagsResult }
  | { status: "error"; message: string };

type FundingSourcesState = FundingSourcesLoadState;
type FundingProgramsState = FundingProgramsLoadState;

type MonitoringState =
  | { status: "idle" }
  | { status: "loading" }
  | { status: "ready"; items: MonitoringIndicatorSummary[] }
  | { status: "error"; message: string };

type MonitoringUpdatesState =
  | { status: "idle" }
  | { status: "loading" }
  | { status: "ready"; items: MonitoringUpdateSummary[] }
  | { status: "error"; message: string };

function describeError(err: unknown): { message: string; notFound: boolean; retryable: boolean } {
  if (isApiError(err)) {
    const notFound = err.status === 404;
    const forbidden = err.status === 403;
    const message = notFound
      ? "This plan could not be found, or you do not have access to it in your current tenant session."
      : forbidden
        ? "You are not allowed to view this plan with the current credentials."
        : err.message;
    return {
      message,
      notFound: notFound || forbidden,
      retryable: !notFound && !forbidden
    };
  }
  if (err instanceof Error) {
    return { message: err.message, notFound: false, retryable: true };
  }
  return { message: "Something went wrong while loading this plan.", notFound: false, retryable: true };
}

function describeDocsError(err: unknown): string {
  if (isApiError(err)) {
    return err.message;
  }
  if (err instanceof Error) {
    return err.message;
  }
  return "Could not load documents.";
}

function describeActionsError(err: unknown): string {
  if (isApiError(err)) {
    const notFound = err.status === 404;
    const forbidden = err.status === 403;
    if (notFound || forbidden) {
      return "Actions for this plan could not be loaded, or you do not have access in your current session.";
    }
    return err.message;
  }
  if (err instanceof Error) {
    return err.message;
  }
  return "Could not load action items.";
}

function describeCcetError(err: unknown): string {
  if (isApiError(err)) {
    const notFound = err.status === 404;
    const forbidden = err.status === 403;
    if (notFound || forbidden) {
      return "Climate expenditure tags could not be loaded for your tenant session.";
    }
    return err.message;
  }
  if (err instanceof Error) {
    return err.message;
  }
  return "Could not load climate expenditure tags.";
}

function describeFundingSourcesError(err: unknown): string {
  if (isApiError(err)) {
    const notFound = err.status === 404;
    const forbidden = err.status === 403;
    if (notFound || forbidden) {
      return "Funding sources could not be loaded for your tenant session.";
    }
    return err.message;
  }
  if (err instanceof Error) {
    return err.message;
  }
  return "Could not load funding sources.";
}

function describeFundingProgramsError(err: unknown): string {
  if (isApiError(err)) {
    const notFound = err.status === 404;
    const forbidden = err.status === 403;
    if (notFound || forbidden) {
      return "Funding programs could not be loaded for your tenant session.";
    }
    return err.message;
  }
  if (err instanceof Error) {
    return err.message;
  }
  return "Could not load funding programs.";
}

function describeMonitoringError(err: unknown): string {
  if (isApiError(err)) {
    const notFound = err.status === 404;
    const forbidden = err.status === 403;
    if (notFound || forbidden) {
      return "Monitoring indicators for this plan could not be loaded, or you do not have access in your current session.";
    }
    return err.message;
  }
  if (err instanceof Error) {
    return err.message;
  }
  return "Could not load monitoring indicators.";
}

function sortIndicatorsNewestFirst(items: MonitoringIndicatorSummary[]): MonitoringIndicatorSummary[] {
  return [...items].sort((a, b) => {
    const ka = Date.parse(a.updatedAtUtc ?? a.lastUpdatedAtUtc ?? a.createdAtUtc ?? "") || 0;
    const kb = Date.parse(b.updatedAtUtc ?? b.lastUpdatedAtUtc ?? b.createdAtUtc ?? "") || 0;
    return kb - ka;
  });
}

function sortDocumentsNewestFirst(items: DocumentSummary[]): DocumentSummary[] {
  return [...items].sort((a, b) => {
    const ka = Date.parse(a.uploadedAtUtc ?? a.createdAtUtc ?? "") || 0;
    const kb = Date.parse(b.uploadedAtUtc ?? b.createdAtUtc ?? "") || 0;
    return kb - ka;
  });
}

export default function PlanWorkspacePage() {
  const params = useParams();
  const planIdParam = params.planId;
  const planId = typeof planIdParam === "string" ? planIdParam : Array.isArray(planIdParam) ? planIdParam[0] : "";

  const [state, setState] = useState<LoadState>({ status: "loading" });
  const [selectedSectionKey, setSelectedSectionKey] = useState<string | null>(null);
  const [docsState, setDocsState] = useState<DocsState>({ status: "idle" });
  const [actionsState, setActionsState] = useState<ActionsState>({ status: "idle" });
  const [ccetState, setCcetState] = useState<CcetState>({ status: "idle" });
  const [fundingSourcesState, setFundingSourcesState] = useState<FundingSourcesState>({ status: "idle" });
  const [fundingProgramsState, setFundingProgramsState] = useState<FundingProgramsState>({ status: "idle" });
  const [monitoringState, setMonitoringState] = useState<MonitoringState>({ status: "idle" });
  const [monitoringUpdatesState, setMonitoringUpdatesState] =
    useState<MonitoringUpdatesState>({ status: "idle" });
  const [selectedActionId, setSelectedActionId] = useState<string | null>(null);
  const [planFundingAllocations, setPlanFundingAllocations] = useState<ActionFundingAllocationSummary[]>([]);
  const [selectedIndicatorId, setSelectedIndicatorId] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (!planId) {
      setState({
        status: "error",
        message: "Missing plan identifier in the URL.",
        notFound: true,
        retryable: false
      });
      return;
    }

    setState({ status: "loading" });
    try {
      const [plan, sections] = await Promise.all([
        planClient.getPlanById(planId),
        planClient.getPlanSections(planId)
      ]);
      setState({ status: "ready", plan, sections });
    } catch (err) {
      const { message, notFound, retryable } = describeError(err);
      setState({ status: "error", message, notFound, retryable });
    }
  }, [planId]);

  const loadDocuments = useCallback(async () => {
    if (!planId) {
      return;
    }
    setDocsState({ status: "loading" });
    try {
      const items = await documentClient.getDocumentsByPlan(planId);
      setDocsState({ status: "ready", items });
    } catch (err) {
      setDocsState({ status: "error", message: describeDocsError(err) });
    }
  }, [planId]);

  const loadActions = useCallback(async () => {
    if (!planId) {
      return;
    }
    setActionsState({ status: "loading" });
    try {
      const items = await actionClient.getActionsByPlan(planId);
      setActionsState({ status: "ready", items });
    } catch (err) {
      setActionsState({ status: "error", message: describeActionsError(err) });
    }
  }, [planId]);

  const loadCcet = useCallback(async () => {
    setCcetState({ status: "loading" });
    try {
      const data = await actionClient.getClimateExpenditureTags();
      setCcetState({ status: "ready", data });
    } catch (err) {
      setCcetState({ status: "error", message: describeCcetError(err) });
    }
  }, []);

  const loadFundingSources = useCallback(async () => {
    setFundingSourcesState({ status: "loading" });
    try {
      const data = await actionClient.getFundingSources();
      setFundingSourcesState({ status: "ready", data });
    } catch (err) {
      setFundingSourcesState({ status: "error", message: describeFundingSourcesError(err) });
    }
  }, []);

  const loadFundingPrograms = useCallback(async () => {
    setFundingProgramsState({ status: "loading" });
    try {
      const data = await actionClient.getFundingPrograms();
      setFundingProgramsState({ status: "ready", data });
    } catch (err) {
      setFundingProgramsState({ status: "error", message: describeFundingProgramsError(err) });
    }
  }, []);

  const loadMonitoring = useCallback(async () => {
    if (!planId) {
      return;
    }
    setMonitoringState({ status: "loading" });
    try {
      const items = await monitoringClient.getIndicatorsByPlan(planId);
      setMonitoringState({ status: "ready", items });
    } catch (err) {
      setMonitoringState({ status: "error", message: describeMonitoringError(err) });
    }
  }, [planId]);

  const handlePlanAllocationsChange = useCallback((items: readonly ActionFundingAllocationSummary[]) => {
    setPlanFundingAllocations([...items]);
  }, []);

  const loadMonitoringUpdates = useCallback(async () => {
    if (!selectedIndicatorId) {
      setMonitoringUpdatesState({ status: "idle" });
      return;
    }

    setMonitoringUpdatesState({ status: "loading" });
    try {
      const items = await monitoringClient.getUpdatesByIndicator(selectedIndicatorId);
      setMonitoringUpdatesState({ status: "ready", items });
    } catch (err) {
      setMonitoringUpdatesState({
        status: "error",
        message: isApiError(err) ? err.message : err instanceof Error ? err.message : "Could not load updates."
      });
    }
  }, [selectedIndicatorId]);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    setSelectedActionId(null);
    setPlanFundingAllocations([]);
    setSelectedIndicatorId(null);
    setMonitoringUpdatesState({ status: "idle" });
    setFundingSourcesState({ status: "idle" });
    setFundingProgramsState({ status: "idle" });
  }, [planId]);

  useEffect(() => {
    if (state.status !== "ready") {
      setDocsState({ status: "idle" });
      setActionsState({ status: "idle" });
      setCcetState({ status: "idle" });
      setFundingSourcesState({ status: "idle" });
      setFundingProgramsState({ status: "idle" });
      setMonitoringState({ status: "idle" });
      return;
    }
    void loadDocuments();
    void loadActions();
    void loadMonitoring();
    void loadCcet();
    void loadFundingSources();
    void loadFundingPrograms();
  }, [
    state.status,
    loadDocuments,
    loadActions,
    loadMonitoring,
    loadCcet,
    loadFundingSources,
    loadFundingPrograms
  ]);

  useEffect(() => {
    void loadMonitoringUpdates();
  }, [loadMonitoringUpdates]);

  useEffect(() => {
    if (state.status !== "ready") {
      return;
    }
    setSelectedSectionKey((prev) => {
      if (state.sections.length === 0) {
        return null;
      }
      if (prev && state.sections.some((s) => s.sectionKey === prev)) {
        return prev;
      }
      return state.sections[0].sectionKey;
    });
  }, [state]);

  const selectedSection =
    state.status === "ready" && selectedSectionKey
      ? state.sections.find((s) => s.sectionKey === selectedSectionKey)
      : undefined;

  function handlePlanUpdated(updated: PlanSummary): void {
    setState((s) => {
      if (s.status !== "ready") {
        return s;
      }
      return {
        ...s,
        plan: updated
      };
    });
  }

  function handleSectionSaved(updated: PlanSectionSummary): void {
    setState((s) => {
      if (s.status !== "ready") {
        return s;
      }
      return {
        status: "ready",
        plan: s.plan,
        sections: s.sections.map((x) => (x.sectionKey === updated.sectionKey ? updated : x))
      };
    });
    setSelectedSectionKey(updated.sectionKey);
  }

  function handleDocumentUploaded(summary: DocumentSummary): void {
    setDocsState((d) => {
      if (d.status === "error" || d.status === "idle" || d.status === "loading") {
        return { status: "ready", items: sortDocumentsNewestFirst([summary]) };
      }
      const without = d.items.filter((x) => x.id !== summary.id);
      return { status: "ready", items: sortDocumentsNewestFirst([summary, ...without]) };
    });
  }

  function handleDocumentUpdated(updated: DocumentSummary): void {
    setDocsState((d) => {
      if (d.status !== "ready") {
        return d;
      }
      const idx = d.items.findIndex((x) => x.id === updated.id);
      if (idx < 0) {
        return { status: "ready", items: sortDocumentsNewestFirst([updated, ...d.items]) };
      }
      const next = [...d.items];
      next[idx] = updated;
      return { status: "ready", items: sortDocumentsNewestFirst(next) };
    });
  }

  function handleDocumentArchived(documentId: string): void {
    setDocsState((d) => {
      if (d.status !== "ready") {
        return d;
      }
      return { status: "ready", items: d.items.filter((x) => x.id !== documentId) };
    });
  }

  function handleActionSaved(saved: SaveActionItemResult): void {
    setActionsState((s) => {
      if (s.status !== "ready") {
        return { status: "ready", items: [saved] };
      }
      const idx = s.items.findIndex((x) => x.id === saved.id);
      if (idx >= 0) {
        const next = [...s.items];
        next[idx] = saved;
        return { status: "ready", items: next };
      }
      return { status: "ready", items: [saved, ...s.items] };
    });
  }

  function handleActionArchived(actionItemId: string): void {
    setActionsState((s) => {
      if (s.status !== "ready") {
        return s;
      }
      return { status: "ready", items: s.items.filter((x) => x.id !== actionItemId) };
    });
    setSelectedActionId((prev) => (prev === actionItemId ? null : prev));
  }

  function handleIndicatorSaved(saved: MonitoringIndicatorSummary): void {
    setMonitoringState((s) => {
      if (s.status !== "ready") {
        return { status: "ready", items: sortIndicatorsNewestFirst([saved]) };
      }
      const idx = s.items.findIndex((x) => x.id === saved.id);
      if (idx >= 0) {
        const next = [...s.items];
        next[idx] = saved;
        return { status: "ready", items: sortIndicatorsNewestFirst(next) };
      }
      return { status: "ready", items: sortIndicatorsNewestFirst([saved, ...s.items]) };
    });
  }

  function handleIndicatorArchived(indicatorId: string): void {
    setMonitoringState((s) => {
      if (s.status !== "ready") {
        return s;
      }
      return { status: "ready", items: s.items.filter((x) => x.id !== indicatorId) };
    });
    setSelectedIndicatorId((prev) => (prev === indicatorId ? null : prev));
  }

  function handleMonitoringUpdateCreated(created: MonitoringUpdateSummary): void {
    setMonitoringUpdatesState((s) => {
      if (s.status !== "ready") {
        return { status: "ready", items: [created] };
      }
      const without = s.items.filter((x) => x.id !== created.id);
      return { status: "ready", items: [created, ...without] };
    });
    void loadMonitoring();
    void loadMonitoringUpdates();
  }

  const selectedActionForForm =
    actionsState.status === "ready" ? actionsState.items.find((a) => a.id === selectedActionId) ?? null : null;

  const selectedIndicatorForForm =
    monitoringState.status === "ready"
      ? monitoringState.items.find((i) => i.id === selectedIndicatorId) ?? null
      : null;

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <Link
              href="/plans"
              className={cn(
                buttonVariants({ variant: "ghost", size: "sm" }),
                "-ml-2 inline-flex gap-1 px-2 text-muted-foreground"
              )}
            >
              <ArrowLeft className="h-4 w-4" aria-hidden />
              Plans
            </Link>
          </div>
          <h1 className="page-title mt-2">Plan workspace</h1>
          <p className="page-description">
            Edit sections, attach evidence files, and organize adaptation and mitigation actions—scoped to this plan and
            tenant session.
          </p>
        </div>
        <Badge variant="secondary" className="shrink-0 self-start sm:mt-8">
          Plan ID · <span className="font-mono text-xs">{planId || "—"}</span>
        </Badge>
      </div>

      {state.status === "loading" ? (
        <Card>
          <CardContent className="flex flex-col items-center justify-center gap-3 py-16 text-center">
            <RefreshCw className="h-8 w-8 animate-spin text-emerald-700" aria-hidden />
            <p className="text-sm text-muted-foreground">Loading plan and sections…</p>
          </CardContent>
        </Card>
      ) : null}

      {state.status === "error" ? (
        <Card className="border-red-200 bg-red-50/40">
          <CardHeader>
            <CardTitle className="text-lg text-red-900">
              {state.notFound ? "Plan unavailable" : "Could not load plan"}
            </CardTitle>
            <CardDescription className="text-base text-red-900/80">{state.message}</CardDescription>
          </CardHeader>
          <CardContent className="flex flex-wrap gap-2">
            {state.retryable ? (
              <Button type="button" variant="outline" className="gap-2" onClick={() => void load()}>
                <RefreshCw className="h-4 w-4" aria-hidden />
                Retry
              </Button>
            ) : null}
            <Link href="/plans" className={cn(buttonVariants({ variant: "secondary" }))}>
              Back to plans
            </Link>
          </CardContent>
        </Card>
      ) : null}

      {state.status === "ready" ? (
        <>
          <PlanSummaryCard plan={state.plan} onPlanUpdated={handlePlanUpdated} onRefresh={() => void load()} />

          <div className="grid gap-6 lg:grid-cols-2 lg:items-start">
            <PlanSectionsPreview
              sections={state.sections}
              selectedSectionKey={selectedSectionKey}
              onSelectSection={setSelectedSectionKey}
            />
            <div className="min-w-0 space-y-4">
              {selectedSection ? (
                <>
                  <PlanSectionEditor planId={planId} section={selectedSection} onSaved={handleSectionSaved} />
                  <SectionCommentsPanel planId={planId} sectionKey={selectedSection.sectionKey} />
                </>
              ) : (
                <Card className="border-dashed border-border">
                  <CardHeader>
                    <CardTitle className="text-base">No section selected</CardTitle>
                    <CardDescription>
                      {state.sections.length === 0
                        ? "This plan has no sections yet. Refresh after the API finishes seeding, or verify tenant access."
                        : "Choose a section from the list to edit its title and content."}
                    </CardDescription>
                  </CardHeader>
                </Card>
              )}
            </div>
          </div>

          <section className="space-y-4" aria-labelledby="plan-documents-heading">
            <h2 id="plan-documents-heading" className="text-lg font-semibold tracking-tight text-slate-900">
              Documents
            </h2>

            {docsState.status === "loading" || docsState.status === "idle" ? (
              <Card>
                <CardContent className="flex items-center gap-3 py-8 text-sm text-muted-foreground">
                  <RefreshCw className="h-5 w-5 shrink-0 animate-spin text-emerald-700" aria-hidden />
                  Loading documents for this plan…
                </CardContent>
              </Card>
            ) : null}

            {docsState.status === "error" ? (
              <Card className="border-amber-200 bg-amber-50/50">
                <CardHeader className="pb-2">
                  <CardTitle className="text-base text-amber-950">Documents could not be loaded</CardTitle>
                  <CardDescription className="text-amber-950/85">{docsState.message}</CardDescription>
                </CardHeader>
                <CardContent className="flex flex-wrap gap-2">
                  <Button type="button" variant="outline" size="sm" className="gap-2" onClick={() => void loadDocuments()}>
                    <RefreshCw className="h-4 w-4" aria-hidden />
                    Retry documents
                  </Button>
                </CardContent>
              </Card>
            ) : null}

            <DocumentUploadForm
              planId={planId}
              planSections={state.sections}
              actionItems={actionsState.status === "ready" ? actionsState.items : []}
              onUploaded={handleDocumentUploaded}
            />

            {docsState.status === "ready" ? (
              <DocumentsList
                documents={docsState.items}
                planSections={state.sections}
                actionItems={actionsState.status === "ready" ? actionsState.items : []}
                onDocumentUpdated={handleDocumentUpdated}
                onDocumentArchived={handleDocumentArchived}
              />
            ) : null}
          </section>

          <section className="space-y-4" aria-labelledby="plan-actions-heading">
            <h2 id="plan-actions-heading" className="text-lg font-semibold tracking-tight text-slate-900">
              Actions
            </h2>
            <p className="text-sm text-muted-foreground">
              Define and track draft adaptation and mitigation measures for export-ready packages—this workspace supports
              LGU preparation and complements existing official systems.
            </p>

            <div className="grid gap-4 md:grid-cols-2 lg:gap-6">
              <FundingSourceCatalogPanel
                status={fundingSourcesState.status}
                result={fundingSourcesState.status === "ready" ? fundingSourcesState.data : undefined}
                errorMessage={fundingSourcesState.status === "error" ? fundingSourcesState.message : undefined}
                onRetry={() => void loadFundingSources()}
              />
              <FundingProgramCatalogPanel
                status={fundingProgramsState.status}
                result={fundingProgramsState.status === "ready" ? fundingProgramsState.data : undefined}
                errorMessage={fundingProgramsState.status === "error" ? fundingProgramsState.message : undefined}
                onRetry={() => void loadFundingPrograms()}
              />
              <CcetCatalogPanel
                status={ccetState.status}
                result={ccetState.status === "ready" ? ccetState.data : undefined}
                errorMessage={ccetState.status === "error" ? ccetState.message : undefined}
                onRetry={() => void loadCcet()}
              />
              <ActionFundingReadinessPanel
                actions={actionsState.status === "ready" ? actionsState.items : []}
                tags={ccetState.status === "ready" ? ccetState.data.items : []}
                planAllocations={planFundingAllocations}
                actionsLoading={actionsState.status === "loading" || actionsState.status === "idle"}
                actionsError={actionsState.status === "error" ? actionsState.message : null}
                ccetLoading={ccetState.status === "loading" || ccetState.status === "idle"}
                ccetError={ccetState.status === "error" ? ccetState.message : null}
                fundingSourcesState={fundingSourcesState}
                fundingProgramsState={fundingProgramsState}
              />
            </div>

            <ActionFundingAllocationPanel
              planId={planId}
              actionItems={actionsState.status === "ready" ? actionsState.items : []}
              ccetTags={ccetState.status === "ready" ? ccetState.data.items : []}
              fundingSourcesState={fundingSourcesState}
              fundingProgramsState={fundingProgramsState}
              selectedActionId={selectedActionId}
              onPlanAllocationsChange={handlePlanAllocationsChange}
            />

            {actionsState.status === "loading" || actionsState.status === "idle" ? (
              <Card>
                <CardContent className="flex items-center gap-3 py-8 text-sm text-muted-foreground">
                  <RefreshCw className="h-5 w-5 shrink-0 animate-spin text-emerald-700" aria-hidden />
                  Loading action items…
                </CardContent>
              </Card>
            ) : null}

            {actionsState.status === "error" ? (
              <Card className="border-amber-200 bg-amber-50/50">
                <CardHeader className="pb-2">
                  <CardTitle className="text-base text-amber-950">Actions could not be loaded</CardTitle>
                  <CardDescription className="text-amber-950/85">{actionsState.message}</CardDescription>
                </CardHeader>
                <CardContent className="flex flex-wrap gap-2">
                  <Button type="button" variant="outline" size="sm" className="gap-2" onClick={() => void loadActions()}>
                    <RefreshCw className="h-4 w-4" aria-hidden />
                    Retry actions
                  </Button>
                </CardContent>
              </Card>
            ) : null}

            {actionsState.status === "ready" ? (
              <div className="grid gap-6 xl:grid-cols-2 xl:items-start">
                <ActionForm
                  planId={planId}
                  selectedAction={selectedActionForForm}
                  onSaved={handleActionSaved}
                  onCancelEdit={selectedActionId ? () => setSelectedActionId(null) : undefined}
                />
                <ActionItemsList
                  items={actionsState.items}
                  selectedId={selectedActionId}
                  onSelect={(id) => setSelectedActionId(id)}
                  onActionUpdated={handleActionSaved}
                  onActionArchived={handleActionArchived}
                />
              </div>
            ) : null}
          </section>

          <section className="space-y-4" aria-labelledby="plan-monitoring-heading">
            <h2 id="plan-monitoring-heading" className="text-lg font-semibold tracking-tight text-slate-900">
              Monitoring indicators
            </h2>
            <p className="text-sm text-muted-foreground">
              Maintain an internal indicator workspace for LGU implementation tracking and working progress—plan-scoped
              drafts that support export-ready packages and complement existing official systems.
            </p>

            {monitoringState.status === "loading" || monitoringState.status === "idle" ? (
              <Card>
                <CardContent className="flex items-center gap-3 py-8 text-sm text-muted-foreground">
                  <RefreshCw className="h-5 w-5 shrink-0 animate-spin text-emerald-700" aria-hidden />
                  Loading monitoring indicators…
                </CardContent>
              </Card>
            ) : null}

            {monitoringState.status === "error" ? (
              <Card className="border-amber-200 bg-amber-50/50">
                <CardHeader className="pb-2">
                  <CardTitle className="text-base text-amber-950">Monitoring could not be loaded</CardTitle>
                  <CardDescription className="text-amber-950/85">{monitoringState.message}</CardDescription>
                </CardHeader>
                <CardContent className="flex flex-wrap gap-2">
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    className="gap-2"
                    onClick={() => void loadMonitoring()}
                  >
                    <RefreshCw className="h-4 w-4" aria-hidden />
                    Retry monitoring
                  </Button>
                </CardContent>
              </Card>
            ) : null}

            {monitoringState.status === "ready" ? (
              <div className="space-y-6">
                <div className="grid gap-6 xl:grid-cols-2 xl:items-start">
                  <IndicatorForm
                    planId={planId}
                    selectedIndicator={selectedIndicatorForForm}
                    onSaved={handleIndicatorSaved}
                    onCancelEdit={selectedIndicatorId ? () => setSelectedIndicatorId(null) : undefined}
                  />
                  <IndicatorsList
                    indicators={monitoringState.items}
                    selectedId={selectedIndicatorId}
                    onSelect={(id) => setSelectedIndicatorId(id)}
                    onIndicatorUpdated={handleIndicatorSaved}
                    onIndicatorArchived={handleIndicatorArchived}
                  />
                </div>

                {selectedIndicatorForForm ? (
                  <div className="grid gap-6 xl:grid-cols-2 xl:items-start">
                    <MonitoringUpdateForm
                      indicatorId={selectedIndicatorForForm.id}
                      defaultStatus={selectedIndicatorForForm.status}
                      onCreated={handleMonitoringUpdateCreated}
                    />
                    <MonitoringUpdateHistory
                      updates={monitoringUpdatesState.status === "ready" ? monitoringUpdatesState.items : []}
                      loading={monitoringUpdatesState.status === "loading"}
                      error={monitoringUpdatesState.status === "error" ? monitoringUpdatesState.message : null}
                    />
                  </div>
                ) : null}
              </div>
            ) : null}
          </section>

          <section className="space-y-4" aria-labelledby="plan-export-heading">
            <h2 id="plan-export-heading" className="text-lg font-semibold tracking-tight text-slate-900">
              Exports and evidence
            </h2>
            <p className="text-sm text-muted-foreground">
              Pull CSV working packages and structured evidence summaries, then generate an optional PDF draft via the shared
              API—draft LGU preparation only, not an official submission channel.
            </p>
            <EvidenceIndexPanel planId={planId} />
            <ExportPanel planId={planId} />
          </section>
        </>
      ) : null}
    </div>
  );
}
