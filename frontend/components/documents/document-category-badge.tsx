import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";

interface DocumentCategoryBadgeProps {
  readonly category: string;
  readonly className?: string;
}

const tone: Record<string, "default" | "secondary" | "outline"> = {
  Clup: "default",
  Cdp: "secondary",
  Drrm: "secondary",
  HazardStudy: "outline",
  ClimateData: "secondary",
  Map: "outline",
  Reference: "default",
  Other: "outline"
};

export function DocumentCategoryBadge({ category, className }: DocumentCategoryBadgeProps) {
  const variant = tone[category] ?? "outline";
  return (
    <Badge variant={variant} className={cn("font-medium", className)}>
      {category}
    </Badge>
  );
}
