import { AlertTriangle } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import type { RiskItem } from "@/types/dashboard";

interface RiskOverviewCardProps {
  risks: RiskItem[];
}

export function RiskOverviewCard({ risks }: RiskOverviewCardProps) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Risk Overview</CardTitle>
        <CardDescription>Items requiring operational attention before next review cycle.</CardDescription>
      </CardHeader>
      <CardContent className="space-y-3">
        {risks.map((risk) => (
          <div key={risk.id} className="rounded-md border border-border p-3">
            <div className="mb-2 flex items-center justify-between gap-2">
              <p className="text-sm font-medium text-slate-900">{risk.title}</p>
              <Badge variant={risk.level === "high" ? "danger" : risk.level === "medium" ? "warning" : "success"}>
                {risk.level}
              </Badge>
            </div>
            <div className="flex items-center gap-2 text-xs text-muted-foreground">
              <AlertTriangle className="h-3.5 w-3.5" />
              <span>
                {risk.owner} | Due {risk.dueDate}
              </span>
            </div>
          </div>
        ))}
      </CardContent>
    </Card>
  );
}
