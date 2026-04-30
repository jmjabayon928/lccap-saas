import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";

export default function ActionsPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="page-title">Actions</h1>
        <p className="page-description">Track adaptation and mitigation action items with ownership and status.</p>
      </div>
      <div className="grid gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Action Board</CardTitle>
            <CardDescription>Will connect to GET /api/plans/:planId/actions and item updates.</CardDescription>
          </CardHeader>
          <CardContent>
            <Badge variant="secondary">MVP backend ready</Badge>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle>Action Detail</CardTitle>
            <CardDescription>Timeline, progress, and risk state per action item.</CardDescription>
          </CardHeader>
          <CardContent className="text-sm text-muted-foreground">
            Details from GET /api/actions/:actionItemId will be rendered in this panel.
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
