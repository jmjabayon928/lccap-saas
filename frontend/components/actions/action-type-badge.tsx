import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import type { ActionType } from "@/types/actions";

interface ActionTypeBadgeProps {
  readonly actionType: ActionType;
  readonly className?: string;
}

export function ActionTypeBadge({ actionType, className }: ActionTypeBadgeProps) {
  const styles =
    actionType === "Adaptation"
      ? "border-sky-200 bg-sky-50 text-sky-900"
      : "border-emerald-200 bg-emerald-50 text-emerald-900";

  return (
    <Badge variant="outline" className={cn("font-medium", styles, className)}>
      {actionType}
    </Badge>
  );
}
