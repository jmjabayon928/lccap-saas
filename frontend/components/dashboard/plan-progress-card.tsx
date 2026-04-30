import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Progress } from "@/components/ui/progress";
import type { PlanSummary, SectionProgress } from "@/types/dashboard";

interface PlanProgressCardProps {
  plan: PlanSummary;
  sections: SectionProgress[];
}

export function PlanProgressCard({ plan, sections }: PlanProgressCardProps) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>{plan.title}</CardTitle>
        <CardDescription>
          {plan.lguName} | Target {plan.targetYear}
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div>
          <div className="mb-2 flex items-center justify-between text-sm">
            <span className="text-muted-foreground">Overall Completion</span>
            <span className="font-semibold text-slate-900">{plan.completionPercent}%</span>
          </div>
          <Progress value={plan.completionPercent} />
        </div>
        <div className="space-y-2">
          {sections.map((section) => (
            <div key={section.sectionKey} className="flex items-center justify-between rounded-md border border-border p-2">
              <p className="text-sm text-slate-700">{section.sectionName}</p>
              <div className="flex items-center gap-2">
                <Badge
                  variant={
                    section.status === "complete"
                      ? "success"
                      : section.status === "needs_attention"
                        ? "warning"
                        : "secondary"
                  }
                >
                  {section.status.replace("_", " ")}
                </Badge>
                <span className="text-xs font-medium text-slate-600">{section.completionPercent}%</span>
              </div>
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}
