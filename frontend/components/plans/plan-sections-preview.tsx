import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { cn } from "@/lib/utils";
import { SectionStatusBadge } from "@/components/plans/section-status-badge";
import type { PlanSectionSummary } from "@/types/plans";

interface PlanSectionsPreviewProps {
  readonly sections: PlanSectionSummary[];
  readonly selectedSectionKey: string | null;
  readonly onSelectSection: (sectionKey: string) => void;
}

function formatEdited(iso: string | null): string {
  if (!iso || !iso.trim()) {
    return "—";
  }
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) {
    return iso;
  }
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(d);
}

export function PlanSectionsPreview({ sections, selectedSectionKey, onSelectSection }: PlanSectionsPreviewProps) {
  return (
    <Card className="border-border shadow-sm">
      <CardHeader>
        <CardTitle>Sections</CardTitle>
        <CardDescription>Select a row to edit title and content in the panel beside this list.</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {sections.length === 0 ? (
          <div className="rounded-lg border border-dashed border-border bg-slate-50/80 px-4 py-10 text-center text-sm text-muted-foreground">
            No sections returned yet. They may still be provisioning, or the API returned an empty list.
          </div>
        ) : (
          <>
            <div className="hidden md:block">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="w-24">Order</TableHead>
                    <TableHead className="w-36">Key</TableHead>
                    <TableHead>Title</TableHead>
                    <TableHead className="w-40">Status</TableHead>
                    <TableHead className="hidden lg:table-cell lg:w-44">Last edited</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {sections.map((s) => {
                    const selected = selectedSectionKey === s.sectionKey;
                    return (
                      <TableRow
                        key={s.id}
                        tabIndex={0}
                        data-state={selected ? "selected" : undefined}
                        aria-label={
                          selected ? `${s.title}, selected section` : `${s.title}, press Enter to select for editing`
                        }
                        className={cn(
                          "cursor-pointer transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
                          selected ? "bg-emerald-50/90 hover:bg-emerald-50" : "hover:bg-slate-50"
                        )}
                        onClick={() => onSelectSection(s.sectionKey)}
                        onKeyDown={(ev) => {
                          if (ev.key === "Enter" || ev.key === " ") {
                            ev.preventDefault();
                            onSelectSection(s.sectionKey);
                          }
                        }}
                      >
                        <TableCell className="font-medium tabular-nums">{s.sortOrder}</TableCell>
                        <TableCell className="font-mono text-xs text-slate-700">{s.sectionKey}</TableCell>
                        <TableCell className="max-w-[220px] truncate text-slate-900 lg:max-w-none">{s.title}</TableCell>
                        <TableCell>
                          <SectionStatusBadge content={s.content} />
                        </TableCell>
                        <TableCell className="hidden text-muted-foreground lg:table-cell">{formatEdited(s.lastEditedAtUtc)}</TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </div>

            <ul className="flex flex-col gap-2 md:hidden">
              {sections.map((s) => {
                const selected = selectedSectionKey === s.sectionKey;
                return (
                  <li key={s.id}>
                    <button
                      type="button"
                      onClick={() => onSelectSection(s.sectionKey)}
                      className={cn(
                        "flex w-full flex-col gap-2 rounded-lg border px-3 py-3 text-left text-sm transition-colors",
                        selected ? "border-emerald-300 bg-emerald-50/90" : "border-border bg-white hover:bg-slate-50"
                      )}
                    >
                      <div className="flex flex-wrap items-center justify-between gap-2">
                        <span className="font-mono text-xs text-slate-600">{s.sectionKey}</span>
                        <span className="tabular-nums text-xs text-muted-foreground">#{s.sortOrder}</span>
                      </div>
                      <span className="font-medium text-slate-900">{s.title}</span>
                      <div className="flex flex-wrap items-center justify-between gap-2">
                        <SectionStatusBadge content={s.content} />
                        <span className="text-xs text-muted-foreground">{formatEdited(s.lastEditedAtUtc)}</span>
                      </div>
                    </button>
                  </li>
                );
              })}
            </ul>
          </>
        )}
      </CardContent>
    </Card>
  );
}
