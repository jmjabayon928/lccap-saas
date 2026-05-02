"use client";

import Link from "next/link";
import { FolderKanban, LogIn } from "lucide-react";
import { demoDashboardData } from "@/lib/demo-data";
import { Badge } from "@/components/ui/badge";
import { buttonVariants } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { MetricCard } from "@/components/dashboard/metric-card";
import { PlanProgressCard } from "@/components/dashboard/plan-progress-card";
import { ActionStatusCard } from "@/components/dashboard/action-status-card";
import { RiskOverviewCard } from "@/components/dashboard/risk-overview-card";
import { RecentActivityCard } from "@/components/dashboard/recent-activity-card";
import { Progress } from "@/components/ui/progress";
import { useAuthSession } from "@/lib/auth/use-auth-session";

export default function DashboardPage() {
  const { plan, sections, actionStatuses, risks, recentActivity, exportReadiness } = demoDashboardData;
  const { isAuthenticated, isLoading } = useAuthSession();

  return (
    <div className="space-y-6">
      <div>
        <div className="flex flex-wrap items-center gap-2">
          <h1 className="page-title">Dashboard</h1>
          <Badge variant="secondary">MVP preview</Badge>
        </div>
        <p className="page-description">Enterprise overview of plan progress, actions, monitoring, risks, and export readiness.</p>
      </div>

      {!isLoading && !isAuthenticated ? (
        <Card className="border-amber-200/90 bg-gradient-to-r from-amber-50/90 to-white shadow-sm">
          <CardHeader className="pb-3 pt-5">
            <div className="flex flex-wrap items-start justify-between gap-4">
              <div className="space-y-1">
                <CardTitle className="text-lg text-slate-900">Demo shell — sign in recommended</CardTitle>
                <CardDescription className="text-base text-slate-600">
                  You are viewing sample metrics and cards. Sign in to connect this workspace to your tenant API and persist
                  real plan data when those modules are enabled.
                </CardDescription>
              </div>
              <Link
                href="/login"
                className={cn(buttonVariants({ variant: "default" }), "inline-flex shrink-0 gap-2")}
              >
                <LogIn className="h-4 w-4" aria-hidden />
                Go to login
              </Link>
            </div>
          </CardHeader>
        </Card>
      ) : null}

      <Card className="border-emerald-200/80 bg-emerald-50/40">
        <CardHeader className="pb-2 pt-4">
          <div className="flex flex-wrap items-center gap-2">
            <CardTitle className="text-base">Auth foundation ready</CardTitle>
            <Badge variant="success">API-ready frontend shell</Badge>
          </div>
          <CardDescription>
            Typed HTTP client, JWT bearer injection, and MVP session storage are wired. Domain modules can call the API
            client incrementally.
          </CardDescription>
        </CardHeader>
      </Card>

      <Card className="border-border">
        <CardHeader className="flex flex-col gap-3 pb-2 pt-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <CardTitle className="text-base">Climate plans</CardTitle>
            <CardDescription>Create and open tenant-backed LCCAP plan workspaces.</CardDescription>
          </div>
          <Link
            href="/plans"
            className={cn(buttonVariants({ variant: "default" }), "inline-flex w-full shrink-0 gap-2 sm:w-auto")}
          >
            <FolderKanban className="h-4 w-4" aria-hidden />
            Go to plans
          </Link>
        </CardHeader>
      </Card>

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <MetricCard
          title="Active Plan"
          value={plan.title}
          description={`${plan.sectionsCompleted}/${plan.totalSections} sections currently completed`}
        />
        <MetricCard
          title="Uploaded Documents"
          value={String(plan.uploadedDocuments)}
          description="Total references and attachments available for plan sections"
        />
        <MetricCard
          title="Monitoring Indicators"
          value={String(plan.monitoringIndicators)}
          description="Indicators tracked for adaptation and mitigation outcomes"
        />
        <MetricCard
          title="Plan Completion"
          value={`${plan.completionPercent}%`}
          description="Overall draft maturity for the current planning cycle"
          progress={plan.completionPercent}
        />
      </div>

      <div className="grid gap-4 xl:grid-cols-[1.5fr_1fr]">
        <PlanProgressCard plan={plan} sections={sections} />
        <div className="space-y-4">
          <ActionStatusCard statuses={actionStatuses} />
          <Card>
            <CardHeader>
              <CardTitle>Export Readiness</CardTitle>
              <CardDescription>PDF submission readiness for next review checkpoint.</CardDescription>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex items-center justify-between text-sm">
                <span className="text-muted-foreground">Readiness Score</span>
                <span className="font-semibold text-slate-900">{exportReadiness.scorePercent}%</span>
              </div>
              <Progress value={exportReadiness.scorePercent} />
              <p className="text-sm text-muted-foreground">{exportReadiness.notes}</p>
              <Badge variant={exportReadiness.blockers > 0 ? "warning" : "success"}>
                {exportReadiness.blockers} blocker{exportReadiness.blockers === 1 ? "" : "s"}
              </Badge>
            </CardContent>
          </Card>
        </div>
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        <RiskOverviewCard risks={risks} />
        <RecentActivityCard items={recentActivity} />
      </div>
    </div>
  );
}
