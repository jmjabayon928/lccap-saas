import { FileOutput, Layers, LineChart, ListChecks, Paperclip } from "lucide-react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";

export function ExportReadinessCard() {
  return (
    <Card className="border-border bg-slate-50/60 shadow-sm">
      <CardHeader className="pb-2">
        <div className="flex items-start gap-3">
          <FileOutput className="mt-0.5 h-5 w-5 shrink-0 text-emerald-800" aria-hidden />
          <div>
            <CardTitle className="text-base">Draft working package</CardTitle>
            <CardDescription>
              This PDF is a <strong className="font-medium text-slate-900">working output</strong> from your LCCAP
              workspace—an export-ready draft for LGU preparation. It is{" "}
              <strong className="font-medium text-slate-900">not</strong> an official submission, approval, or
              compliance certification, and it complements existing agency systems rather than replacing them.
            </CardDescription>
          </div>
        </div>
      </CardHeader>
      <CardContent className="space-y-3 text-sm text-muted-foreground">
        <p className="font-medium text-slate-800">What typically strengthens a draft export</p>
        <ul className="space-y-2">
          <li className="flex gap-2">
            <Layers className="mt-0.5 h-4 w-4 shrink-0 text-teal-700" aria-hidden />
            <span>
              <span className="text-slate-900">Plan sections</span> — narrative content your team is editing in this
              workspace.
            </span>
          </li>
          <li className="flex gap-2">
            <Paperclip className="mt-0.5 h-4 w-4 shrink-0 text-teal-700" aria-hidden />
            <span>
              <span className="text-slate-900">Documents / evidence</span> — supporting files attached to the plan.
            </span>
          </li>
          <li className="flex gap-2">
            <ListChecks className="mt-0.5 h-4 w-4 shrink-0 text-teal-700" aria-hidden />
            <span>
              <span className="text-slate-900">Action items</span> — adaptation and mitigation measures you track here.
            </span>
          </li>
          <li className="flex gap-2">
            <LineChart className="mt-0.5 h-4 w-4 shrink-0 text-teal-700" aria-hidden />
            <span>
              <span className="text-slate-900">Monitoring indicators</span> — implementation tracking rows for this plan.
            </span>
          </li>
        </ul>
      </CardContent>
    </Card>
  );
}
