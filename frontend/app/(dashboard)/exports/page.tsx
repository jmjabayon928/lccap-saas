import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";

export default function ExportsPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="page-title">Exports</h1>
        <p className="page-description">Generate and track LCCAP PDF export jobs for submission workflows.</p>
      </div>
      <div className="grid gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Export Queue</CardTitle>
            <CardDescription>Will trigger POST /api/plans/:planId/exports/pdf jobs.</CardDescription>
          </CardHeader>
          <CardContent>
            <Badge variant="secondary">MVP backend ready</Badge>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle>Job Status & Downloads</CardTitle>
            <CardDescription>Progress and retrieval from export job status endpoints.</CardDescription>
          </CardHeader>
          <CardContent className="text-sm text-muted-foreground">
            Job polling and download links will use export status and download APIs.
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
