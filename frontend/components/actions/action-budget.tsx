interface ActionBudgetProps {
  readonly budgetAmount: number;
}

const phpFormatter = new Intl.NumberFormat("en-PH", {
  style: "currency",
  currency: "PHP",
  minimumFractionDigits: 2,
  maximumFractionDigits: 2
});

export function ActionBudget({ budgetAmount }: ActionBudgetProps) {
  if (!Number.isFinite(budgetAmount)) {
    return <span className="tabular-nums text-muted-foreground">—</span>;
  }

  return <span className="tabular-nums text-slate-800">{phpFormatter.format(budgetAmount)}</span>;
}
