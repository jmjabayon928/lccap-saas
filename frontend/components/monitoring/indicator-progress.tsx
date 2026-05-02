import { Progress } from "@/components/ui/progress";
import { cn } from "@/lib/utils";

interface IndicatorProgressProps {
  readonly progressPercent: number | null;
  readonly className?: string;
}

function clampDisplayPercent(value: number): number {
  return Math.max(0, Math.min(100, value));
}

export function IndicatorProgress({ progressPercent, className }: IndicatorProgressProps) {
  if (progressPercent === null || progressPercent === undefined) {
    return <p className={cn("text-sm text-muted-foreground", className)}>No progress entered</p>;
  }

  const display = clampDisplayPercent(progressPercent);

  return (
    <div className={cn("space-y-1", className)}>
      <div className="flex items-center justify-between gap-2 text-xs text-muted-foreground">
        <span>Progress</span>
        <span className="tabular-nums font-medium text-slate-800">{Math.round(display)}%</span>
      </div>
      <Progress value={display} />
    </div>
  );
}
