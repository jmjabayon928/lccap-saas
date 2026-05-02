import Link from "next/link";
import { ClipboardList, FolderKanban, Layers } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button, buttonVariants } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { cn } from "@/lib/utils";

export default function ActionsPage() {
  return (
    <div className="space-y-8">
      <div className="space-y-3">
        <div className="flex flex-wrap items-center gap-2">
          <Badge variant="secondary">Climate action planning workspace</Badge>
        </div>
        <h1 className="page-title">Actions</h1>
        <p className="page-description max-w-3xl">
          Adaptation and mitigation actions are organized <strong className="font-semibold text-slate-800">per plan</strong>
          —use them to prepare evidence-backed drafts and export-ready packages inside your LGU workspace. This supports
          internal preparation and complements existing official reporting channels; it is not an approval or submission
          portal.
        </p>
      </div>

      <Card className="border-emerald-100 bg-gradient-to-br from-emerald-50/80 to-white shadow-sm">
        <CardHeader className="pb-4">
          <div className="flex flex-wrap items-start gap-3">
            <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-emerald-100 text-emerald-800">
              <Layers className="h-5 w-5" aria-hidden />
            </span>
            <div className="min-w-0 space-y-1">
              <CardTitle className="text-xl">Plan-scoped action organizer</CardTitle>
              <CardDescription className="text-base text-slate-700">
                Create and edit action items from a plan workspace so timelines, budgets, owners, and KPIs stay aligned
                with your LCCAP narrative.
              </CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent className="flex flex-col gap-4 sm:flex-row sm:flex-wrap sm:items-center sm:justify-between">
          <p className="max-w-xl text-sm text-muted-foreground">
            Open <span className="font-medium text-slate-800">Plans</span>, choose a plan, then use the Actions panel to
            add adaptation or mitigation measures and track status as working drafts.
          </p>
          <Link href="/plans" className={cn(buttonVariants({ size: "lg" }), "shrink-0 gap-2")}>
            <FolderKanban className="h-4 w-4" aria-hidden />
            Go to Plans
          </Link>
        </CardContent>
      </Card>

      <div className="grid gap-6 lg:grid-cols-3">
        <Card className="border-border shadow-sm">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-base">
              <ClipboardList className="h-4 w-4 text-emerald-700" aria-hidden />
              Where to work
            </CardTitle>
            <CardDescription>
              Lists and forms live on each plan workspace—not on this overview—so actions stay tied to the correct plan
              period and attachments.
            </CardDescription>
          </CardHeader>
          <CardContent className="text-sm text-muted-foreground">
            From <span className="font-medium text-slate-800">Plans</span>, open a plan to edit sections, documents, and
            action items together.
          </CardContent>
        </Card>

        <Card className="border-border shadow-sm">
          <CardHeader>
            <CardTitle className="text-base">Draft-first positioning</CardTitle>
            <CardDescription>
              Treat entries as internal coordination and export-ready drafts until your official process says otherwise.
            </CardDescription>
          </CardHeader>
          <CardContent className="text-sm text-muted-foreground">
            Status and budgets help teams prioritize—they do not replace CCC/DILG/LGA/NICCDIES workflows or external grant
            systems.
          </CardContent>
        </Card>

        <Card className="border-border shadow-sm">
          <CardHeader>
            <CardTitle className="text-base">Monitoring & exports</CardTitle>
            <CardDescription>Coming in later slices from the plan workspace.</CardDescription>
          </CardHeader>
          <CardContent className="flex flex-wrap gap-2">
            <Button type="button" variant="outline" size="sm" disabled>
              Monitoring
            </Button>
            <Button type="button" variant="outline" size="sm" disabled>
              Export PDF
            </Button>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
