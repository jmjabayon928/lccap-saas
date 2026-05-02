import type { MonitoringIndicatorSummary } from "@/types/monitoring";

interface IndicatorValueSummaryProps {
  readonly indicator: Pick<
    MonitoringIndicatorSummary,
    "baselineValue" | "currentValue" | "targetValue" | "unit"
  >;
  readonly compact?: boolean;
}

function formatNum(v: number | null): string {
  if (v === null || v === undefined) {
    return "—";
  }
  return Number.isInteger(v) ? String(v) : v.toLocaleString(undefined, { maximumFractionDigits: 4 });
}

export function IndicatorValueSummary({ indicator, compact }: IndicatorValueSummaryProps) {
  const unit = indicator.unit?.trim() ? indicator.unit.trim() : null;
  const suffix = unit ? ` ${unit}` : "";

  if (compact) {
    return (
      <span className="text-sm text-muted-foreground">
        <span className="tabular-nums">{formatNum(indicator.baselineValue)}</span>
        {" → "}
        <span className="tabular-nums">{formatNum(indicator.currentValue)}</span>
        {" / "}
        <span className="tabular-nums">{formatNum(indicator.targetValue)}</span>
        {suffix ? <span className="text-slate-600">{suffix}</span> : null}
      </span>
    );
  }

  return (
    <dl className="grid grid-cols-3 gap-2 text-xs sm:text-sm">
      <div className="rounded-md border border-border/80 bg-slate-50/80 px-2 py-1.5">
        <dt className="text-muted-foreground">Baseline</dt>
        <dd className="tabular-nums font-medium text-slate-900">
          {formatNum(indicator.baselineValue)}
          {suffix}
        </dd>
      </div>
      <div className="rounded-md border border-border/80 bg-slate-50/80 px-2 py-1.5">
        <dt className="text-muted-foreground">Current</dt>
        <dd className="tabular-nums font-medium text-slate-900">
          {formatNum(indicator.currentValue)}
          {suffix}
        </dd>
      </div>
      <div className="rounded-md border border-border/80 bg-slate-50/80 px-2 py-1.5">
        <dt className="text-muted-foreground">Target</dt>
        <dd className="tabular-nums font-medium text-slate-900">
          {formatNum(indicator.targetValue)}
          {suffix}
        </dd>
      </div>
    </dl>
  );
}
