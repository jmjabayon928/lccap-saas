import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";

export default function PlansPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="page-title">Plans</h1>
        <p className="page-description">Create and manage LCCAP plans across planning cycles and LGU contexts.</p>
      </div>
      <div className="grid gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Plan Registry</CardTitle>
            <CardDescription>Will list all plans from POST /api/plans and GET /api/plans/:planId.</CardDescription>
          </CardHeader>
          <CardContent>
            <Badge variant="secondary">MVP backend ready</Badge>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle>Plan Lifecycle Controls</CardTitle>
            <CardDescription>Draft, publish, and updates aligned with future plan endpoints.</CardDescription>
          </CardHeader>
          <CardContent className="text-sm text-muted-foreground">
            Actions for create, edit, and validation will appear here once API wiring begins.
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
