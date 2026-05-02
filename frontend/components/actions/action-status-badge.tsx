import { Badge } from "@/components/ui/badge";
import type { ActionStatus } from "@/types/actions";

interface ActionStatusBadgeProps {
  readonly status: ActionStatus;
}

function variantForStatus(status: ActionStatus): "default" | "secondary" | "success" | "warning" | "danger" | "outline" {
  switch (status) {
    case "Planned":
      return "outline";
    case "InProgress":
    case "OnTrack":
      return "default";
    case "Delayed":
      return "warning";
    case "Completed":
      return "success";
    case "Cancelled":
      return "danger";
    default:
      return "outline";
  }
}

export function ActionStatusBadge({ status }: ActionStatusBadgeProps) {
  return <Badge variant={variantForStatus(status)}>{status}</Badge>;
}
