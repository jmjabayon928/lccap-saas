import Link from "next/link";
import { BarChart3, ClipboardList, FileOutput } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button, buttonVariants } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { cn } from "@/lib/utils";

export default function MonitoringPage() {
  return (
    <div className="space-y-8">
      <div className="space-y-2">
        <div className="flex flex-wrap items-center gap-2">
          <Badge variant="secondary">Plan-scoped workspace</Badge>
        </div>
        <h1 className="page-title">Monitoring indicator workspace</h1>
        <p className="page-description max-w-3xl">
          Indicators are defined and tracked per plan inside the plan workspace. Use this area for high-level context:
          internal LGU implementation tracking, working progress updates, and export-ready draft package preparation—not a
          replacement for national dashboards or official reporting channels.
        </p>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <Card className="border-border shadow-sm lg:col-span-2">
          <CardHeader>
            <div className="flex items-start gap-3">
              <ClipboardList className="mt-0.5 h-5 w-5 shrink-0 text-emerald-700" aria-hidden />
              <div>
                <CardTitle>Where to manage indicators</CardTitle>
                <CardDescription className="text-base">
                  Open any plan from <strong className="font-semibold text-slate-900">Plans</strong>, then use the{" "}
                  <strong className="font-semibold text-slate-900">Monitoring indicators</strong> section on the plan
                  workspace to create indicators, update progress, and align responsibilities—all scoped to that plan and
                  your tenant session.
                </CardDescription>
              </div>
            </div>
          </CardHeader>
          <CardContent className="flex flex-wrap gap-3">
            <Link href="/plans" className={cn(buttonVariants({}), "gap-2")}>
              Go to Plans
            </Link>
            <p className="w-full text-sm text-muted-foreground">
              After selecting a plan, open its workspace and scroll to <strong className="font-medium text-slate-800">Monitoring indicators</strong>{" "}
              to add or edit rows without leaving the page.
            </p>
          </CardContent>
        </Card>

        <Card className="border-border shadow-sm">
          <CardHeader>
            <div className="flex items-start gap-3">
              <BarChart3 className="mt-0.5 h-5 w-5 shrink-0 text-teal-700" aria-hidden />
              <div>
                <CardTitle className="text-base">Working preparation</CardTitle>
                <CardDescription>
                  Track baseline, current, and target values alongside status and percent complete for implementation
                  meetings and draft exports.
                </CardDescription>
              </div>
            </div>
          </CardHeader>
          <CardContent className="text-sm text-muted-foreground">
            This MVP slice focuses on LGU-facing operating workspace functionality only—no approval workflow, funding
            portal, or national aggregation here.
          </CardContent>
        </Card>
      </div>

      <Card className="border-dashed border-border bg-slate-50/60">
        <CardHeader>
          <div className="flex items-start gap-3">
            <FileOutput className="mt-0.5 h-5 w-5 shrink-0 text-slate-600" aria-hidden />
            <div>
              <CardTitle className="text-base">Export-ready drafts</CardTitle>
              <CardDescription>
                Indicator data you maintain here can align with documents and actions on the same plan to build cohesive
                export-ready draft packages. Official submission routes remain outside this workspace.
              </CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <Button type="button" variant="outline" disabled className="pointer-events-none">
            Export workflows — coming later
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}
