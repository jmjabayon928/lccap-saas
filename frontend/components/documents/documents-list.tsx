import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { DocumentCategoryBadge } from "@/components/documents/document-category-badge";
import { DocumentSize } from "@/components/documents/document-size";
import type { DocumentSummary } from "@/types/documents";

interface DocumentsListProps {
  readonly documents: DocumentSummary[];
}

function formatWhen(uploadedAtUtc: string | null, createdAtUtc: string | null): string {
  const raw = uploadedAtUtc ?? createdAtUtc;
  if (!raw || !raw.trim()) {
    return "—";
  }
  const d = new Date(raw);
  if (Number.isNaN(d.getTime())) {
    return raw;
  }
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(d);
}

export function DocumentsList({ documents }: DocumentsListProps) {
  return (
    <Card className="border-border shadow-sm">
      <CardHeader>
        <CardTitle>Attached documents</CardTitle>
        <CardDescription>Files linked to this plan for references, maps, and annex materials.</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {documents.length === 0 ? (
          <div className="rounded-lg border border-dashed border-border bg-slate-50/80 px-4 py-10 text-center text-sm text-muted-foreground">
            No documents yet. Upload a file using the form above.
          </div>
        ) : (
          <>
            <div className="hidden md:block">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Title</TableHead>
                    <TableHead className="w-32">Category</TableHead>
                    <TableHead className="hidden lg:table-cell">File</TableHead>
                    <TableHead className="hidden xl:table-cell">Type</TableHead>
                    <TableHead className="w-24">Size</TableHead>
                    <TableHead className="hidden lg:table-cell lg:w-44">Uploaded</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {documents.map((d) => (
                    <TableRow key={d.id}>
                      <TableCell className="max-w-[200px] font-medium text-slate-900 lg:max-w-xs">{d.title}</TableCell>
                      <TableCell>
                        <DocumentCategoryBadge category={d.category} />
                      </TableCell>
                      <TableCell className="hidden font-mono text-xs text-slate-700 lg:table-cell">
                        {d.originalFileName ?? "—"}
                      </TableCell>
                      <TableCell className="hidden text-muted-foreground xl:table-cell">
                        {d.contentType ?? "—"}
                      </TableCell>
                      <TableCell className="tabular-nums text-muted-foreground">
                        <DocumentSize bytes={d.sizeBytes} />
                      </TableCell>
                      <TableCell className="hidden text-muted-foreground lg:table-cell">
                        {formatWhen(d.uploadedAtUtc, d.createdAtUtc)}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>

            <ul className="flex flex-col gap-3 md:hidden">
              {documents.map((d) => (
                <li
                  key={d.id}
                  className="rounded-lg border border-border bg-white px-3 py-3 text-sm shadow-sm"
                >
                  <div className="flex flex-wrap items-start justify-between gap-2">
                    <p className="font-medium text-slate-900">{d.title}</p>
                    <DocumentCategoryBadge category={d.category} />
                  </div>
                  <p className="mt-1 font-mono text-xs text-slate-600">{d.originalFileName ?? "—"}</p>
                  <div className="mt-2 flex flex-wrap items-center gap-3 text-xs text-muted-foreground">
                    <DocumentSize bytes={d.sizeBytes} />
                    <span>{d.contentType ?? "—"}</span>
                    <span>{formatWhen(d.uploadedAtUtc, d.createdAtUtc)}</span>
                  </div>
                </li>
              ))}
            </ul>
          </>
        )}
      </CardContent>
    </Card>
  );
}
