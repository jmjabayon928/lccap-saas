import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import type { ExportStatus } from "@/types/exports";

interface ExportStatusBadgeProps {
  readonly status: ExportStatus;
}

function variantForStatus(
  status: ExportStatus
): { variant: "default" | "secondary" | "success" | "warning" | "danger" | "outline"; className?: string } {
  switch (status) {
    case "Queued":
      return { variant: "outline" };
    case "Running":
      return {
        variant: "outline",
        className: "border-sky-300 bg-sky-50 text-sky-950"
      };
    case "Completed":
      return { variant: "success" };
    case "Failed":
      return { variant: "danger" };
    case "Cancelled":
      return {
        variant: "outline",
        className: "border-slate-200 bg-slate-100 text-slate-600"
      };
    default:
      return { variant: "outline" };
  }
}

export function ExportStatusBadge({ status }: ExportStatusBadgeProps) {
  const { variant, className } = variantForStatus(status);
  return (
    <Badge variant={variant} className={cn(className)}>
      {status}
    </Badge>
  );
}
