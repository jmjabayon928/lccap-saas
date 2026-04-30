import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";

interface PlanWorkspacePageProps {
  params: Promise<{ planId: string }>;
}

export default async function PlanWorkspacePage({ params }: PlanWorkspacePageProps) {
  const { planId } = await params;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="page-title">Plan Workspace</h1>
        <p className="page-description">Collaborative section editing for plan ID: {planId}</p>
      </div>
      <div className="grid gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Sections Workspace</CardTitle>
            <CardDescription>Will surface sections from GET /api/plans/:planId/sections.</CardDescription>
          </CardHeader>
          <CardContent>
            <Badge variant="secondary">MVP backend ready</Badge>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle>Section Editor</CardTitle>
            <CardDescription>Structured editor mapped to section endpoints and update flows.</CardDescription>
          </CardHeader>
          <CardContent className="text-sm text-muted-foreground">
            Section detail, save status, and validation indicators will appear in this panel.
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
