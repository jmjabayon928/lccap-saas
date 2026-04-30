import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";

export default function MonitoringPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="page-title">Monitoring</h1>
        <p className="page-description">Indicator tracking and performance checks for plan outcomes.</p>
      </div>
      <div className="grid gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Indicator Catalog</CardTitle>
            <CardDescription>Will display indicators from monitoring plan endpoints.</CardDescription>
          </CardHeader>
          <CardContent>
            <Badge variant="secondary">MVP backend ready</Badge>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle>Performance Signals</CardTitle>
            <CardDescription>Trend and threshold insights for climate adaptation progress.</CardDescription>
          </CardHeader>
          <CardContent className="text-sm text-muted-foreground">
            Planned integration with GET /api/monitoring/plans/:planId/indicators.
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
