import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { PlanMetadataEditForm } from "@/components/plans/plan-metadata-edit-form";
import { PlanArchiveButton } from "@/components/plans/plan-archive-button";
import type { PlanSummary } from "@/types/plans";
import { useState } from "react";
import { useRouter } from "next/navigation";

interface PlanSummaryCardProps {
  readonly plan: PlanSummary;
  readonly onPlanUpdated?: (updated: PlanSummary) => void;
  readonly onRefresh?: () => void;
}

export function PlanSummaryCard({ plan, onPlanUpdated, onRefresh }: PlanSummaryCardProps) {
  const [isEditing, setIsEditing] = useState(false);
  const router = useRouter();

  if (isEditing) {
    return (
      <PlanMetadataEditForm
        plan={plan}
        onCancel={() => setIsEditing(false)}
        onSuccess={(updated) => {
          setIsEditing(false);
          onPlanUpdated?.(updated);
        }}
      />
    );
  }

  const isLegacy = plan.rowVersion === null;

  return (
    <Card className="border-border shadow-sm">
      <CardHeader>
        <div className="flex flex-wrap items-start justify-between gap-2">
          <div className="space-y-1">
            <CardTitle className="text-xl leading-tight">{plan.title}</CardTitle>
            <CardDescription>
              Planning period {plan.startYear}–{plan.endYear} · {plan.templateMode} · v{plan.versionNumber}
            </CardDescription>
          </div>
          <div className="flex items-center gap-2">
            <Badge variant="secondary">{plan.status}</Badge>
            {isLegacy ? (
              <Button variant="outline" size="sm" onClick={onRefresh} className="text-amber-700 border-amber-200 bg-amber-50 hover:bg-amber-100">
                Refresh to edit
              </Button>
            ) : (
              <Button variant="outline" size="sm" onClick={() => setIsEditing(true)}>
                Edit details
              </Button>
            )}
          </div>
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        {isLegacy && (
          <p className="text-xs text-amber-700 bg-amber-50 p-2 rounded border border-amber-100">
            This plan record is missing a concurrency token. Please refresh to enable editing.
          </p>
        )}
        {plan.description ? (
          <p className="text-sm text-foreground">{plan.description}</p>
        ) : (
          <p className="text-sm text-muted-foreground italic">No description provided.</p>
        )}
        <div className="pt-2 border-t border-border/50">
          <PlanArchiveButton
            planId={plan.id}
            planTitle={plan.title}
            onSuccess={() => router.push("/plans")}
          />
        </div>
      </CardContent>
    </Card>
  );
}
