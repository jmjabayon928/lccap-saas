import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import type { PlanSummary } from "@/types/plans";

interface PlanSummaryCardProps {
  readonly plan: PlanSummary;
}

export function PlanSummaryCard({ plan }: PlanSummaryCardProps) {
  return (
    <Card className="border-border shadow-sm">
      <CardHeader>
        <div className="flex flex-wrap items-start justify-between gap-2">
          <div className="space-y-1">
            <CardTitle className="text-xl leading-tight">{plan.title}</CardTitle>
            <CardDescription>
              Planning period {plan.startYear}–{plan.endYear}
            </CardDescription>
          </div>
          <Badge variant="secondary">{plan.status}</Badge>
        </div>
      </CardHeader>
      <CardContent className="space-y-3 text-sm text-muted-foreground">
        <p>
          Standard workspace sections are seeded by the backend when this plan is created. Use the section list below as
          your collaboration checklist.
        </p>
      </CardContent>
    </Card>
  );
}
