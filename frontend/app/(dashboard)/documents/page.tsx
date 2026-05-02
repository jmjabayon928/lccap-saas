import Link from "next/link";
import { FolderKanban, Files } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { buttonVariants } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { cn } from "@/lib/utils";

export default function DocumentsPage() {
  return (
    <div className="space-y-6">
      <div>
        <div className="flex flex-wrap items-center gap-2">
          <h1 className="page-title">Documents</h1>
          <Badge variant="secondary">Plan-scoped</Badge>
        </div>
        <p className="page-description">
          References, hazard studies, maps, and annex files live under each climate action plan. Upload and browse files from a
          plan workspace—not globally—so tenant boundaries stay aligned with the API.
        </p>
      </div>

      <Card className="border-emerald-100 bg-emerald-50/40 shadow-sm">
        <CardHeader className="space-y-3">
          <div className="flex flex-wrap items-start gap-3">
            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-emerald-700/10 text-emerald-800">
              <Files className="h-5 w-5" aria-hidden />
            </div>
            <div className="min-w-0 flex-1 space-y-1">
              <CardTitle className="text-xl">Open a plan to manage files</CardTitle>
              <CardDescription className="text-base text-slate-700">
                Create or select a plan, then use the workspace <strong>Documents</strong> panel to upload (multipart) and
                view the library for that plan only. There is no tenant-wide document index in this MVP UI.
              </CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <Link
            href="/plans"
            className={cn(buttonVariants({ variant: "default" }), "inline-flex gap-2")}
          >
            <FolderKanban className="h-4 w-4" aria-hidden />
            Go to plans
          </Link>
        </CardContent>
      </Card>

      <div className="grid gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Categories</CardTitle>
            <CardDescription>
              Uploads are labeled (e.g. CLUP, DRRM, hazard studies) for sorting and review—aligned with the API contract.
            </CardDescription>
          </CardHeader>
          <CardContent className="text-sm text-muted-foreground">
            Extension and size checks in the UI complement server enforcement; always treat the API as authoritative.
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle>Library view</CardTitle>
            <CardDescription>
              Each plan workspace lists documents returned by GET /api/plans/&#123;planId&#125;/documents, newest first.
            </CardDescription>
          </CardHeader>
          <CardContent className="text-sm text-muted-foreground">
            Download links will appear when a safe download endpoint is wired—avoid guessing URLs from stored paths.
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
