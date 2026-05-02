import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import type { MonitoringStatus } from "@/types/monitoring";

interface IndicatorStatusBadgeProps {
  readonly status: MonitoringStatus;
}

function variantForStatus(
  status: MonitoringStatus
): { variant: "default" | "secondary" | "success" | "warning" | "outline"; className?: string } {
  switch (status) {
    case "NotStarted":
      return { variant: "outline" };
    case "InProgress":
      return {
        variant: "outline",
        className: "border-sky-300 bg-sky-50 text-sky-950"
      };
    case "OnTrack":
      return { variant: "secondary" };
    case "Delayed":
      return { variant: "warning" };
    case "Completed":
      return { variant: "success" };
    default:
      return { variant: "outline" };
  }
}

export function IndicatorStatusBadge({ status }: IndicatorStatusBadgeProps) {
  const { variant, className } = variantForStatus(status);
  return (
    <Badge variant={variant} className={cn(className)}>
      {status}
    </Badge>
  );
}
