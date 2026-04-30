import { demoDashboardData } from "@/lib/demo-data";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { MetricCard } from "@/components/dashboard/metric-card";
import { PlanProgressCard } from "@/components/dashboard/plan-progress-card";
import { ActionStatusCard } from "@/components/dashboard/action-status-card";
import { RiskOverviewCard } from "@/components/dashboard/risk-overview-card";
import { RecentActivityCard } from "@/components/dashboard/recent-activity-card";
import { Progress } from "@/components/ui/progress";

export default function DashboardPage() {
  const { plan, sections, actionStatuses, risks, recentActivity, exportReadiness } = demoDashboardData;

  return (
    <div className="space-y-6">
      <div>
        <div className="flex flex-wrap items-center gap-2">
          <h1 className="page-title">Dashboard</h1>
          <Badge variant="secondary">MVP preview</Badge>
        </div>
        <p className="page-description">Enterprise overview of plan progress, actions, monitoring, risks, and export readiness.</p>
      </div>

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
