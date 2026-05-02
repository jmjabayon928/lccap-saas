import Link from "next/link";
import { FileDown, FolderKanban } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { buttonVariants } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { cn } from "@/lib/utils";

export default function ExportsPage() {
  return (
    <div className="space-y-8">
      <div className="space-y-2">
        <Badge variant="secondary">Plan-scoped from workspace</Badge>
        <h1 className="page-title">Exports</h1>
        <p className="page-description max-w-3xl">
          PDF exports are generated per plan from the plan workspace. They are{" "}
          <strong className="font-semibold text-slate-900">draft working outputs</strong> and export-ready preparation
          packages for LGU teams—not official government submissions, approvals, or replacements for national systems.
        </p>
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <Card className="border-border shadow-sm lg:col-span-2">
          <CardHeader>
            <div className="flex items-start gap-3">
              <FolderKanban className="mt-0.5 h-6 w-6 shrink-0 text-emerald-800" aria-hidden />
              <div>
                <CardTitle>Where to export</CardTitle>
                <CardDescription className="text-base">
                  Open a plan, then use <strong className="font-semibold text-slate-900">Export draft PDF package</strong>{" "}
                  on the plan workspace to start a job and download when the API reports completed—authorization and
                  tenant rules are enforced by the backend.
                </CardDescription>
              </div>
            </div>
          </CardHeader>
          <CardContent>
            <Link href="/plans" className={cn(buttonVariants({}), "inline-flex gap-2")}>
              Go to Plans
            </Link>
          </CardContent>
        </Card>

        <Card className="border-border shadow-sm">
          <CardHeader>
            <CardTitle className="text-base">Draft outputs only</CardTitle>
            <CardDescription>
              Treat downloads as internal working PDFs and LGU preparation aids. Route official filings through the
              channels your organization uses—this workspace complements those systems.
            </CardDescription>
          </CardHeader>
          <CardContent className="text-sm text-muted-foreground">
            There is no global export catalog in this MVP slice; each job is tied to the plan where it was started.
          </CardContent>
        </Card>

        <Card className="border-border shadow-sm">
          <CardHeader>
            <div className="flex items-start gap-3">
              <FileDown className="mt-0.5 h-5 w-5 shrink-0 text-teal-800" aria-hidden />
              <div>
                <CardTitle className="text-base">Download behavior</CardTitle>
                <CardDescription>
                  Completed jobs expose a browser download using the API download endpoint—no invented URLs or exposed
                  storage paths in the UI.
                </CardDescription>
              </div>
            </div>
          </CardHeader>
        </Card>
      </div>
    </div>
  );
}
