import { Fragment, useState } from "react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { DocumentArchiveButton } from "@/components/documents/document-archive-button";
import { DocumentCategoryBadge } from "@/components/documents/document-category-badge";
import { DocumentEditForm } from "@/components/documents/document-edit-form";
import { DocumentSize } from "@/components/documents/document-size";
import type { DocumentSummary } from "@/types/documents";
import type { ActionItemSummary } from "@/types/actions";
import type { PlanSectionSummary } from "@/types/plans";

const evidenceTone: Record<string, "default" | "secondary" | "outline"> = {
  Draft: "outline",
  Internal: "secondary",
  Official: "default",
  Public: "outline",
};

function EvidenceStatusBadge({ evidenceStatus }: { evidenceStatus: DocumentSummary["evidenceStatus"] }) {
  const variant = evidenceTone[evidenceStatus] ?? "outline";
  return (
    <Badge variant={variant} className="font-medium">
      {evidenceStatus}
    </Badge>
  );
}

interface DocumentsListProps {
  readonly documents: DocumentSummary[];
  readonly planSections: readonly PlanSectionSummary[];
  readonly actionItems: readonly ActionItemSummary[];
  readonly onDocumentUpdated: (updated: DocumentSummary) => void;
  readonly onDocumentArchived: (documentId: string) => void;
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

function rowTitle(d: DocumentSummary): string {
  const t = d.title?.trim();
  if (t) {
    return t;
  }
  if (d.originalFileName?.trim()) {
    return d.originalFileName.trim();
  }
  return "—";
}

export function DocumentsList({
  documents,
  planSections,
  actionItems,
  onDocumentUpdated,
  onDocumentArchived
}: DocumentsListProps) {
  const [editingId, setEditingId] = useState<string | null>(null);

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
                    <TableHead className="w-32 hidden xl:table-cell">Evidence</TableHead>
                    <TableHead className="hidden lg:table-cell">File</TableHead>
                    <TableHead className="hidden xl:table-cell">Type</TableHead>
                    <TableHead className="w-24">Size</TableHead>
                    <TableHead className="hidden lg:table-cell lg:w-44">Uploaded</TableHead>
                    <TableHead className="w-40 text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {documents.map((d) => (
                    <Fragment key={d.id}>
                      <TableRow>
                        <TableCell className="max-w-[200px] font-medium text-slate-900 lg:max-w-xs">
                          {rowTitle(d)}
                        </TableCell>
                        <TableCell>
                          <DocumentCategoryBadge category={d.category} />
                        </TableCell>
                        <TableCell className="hidden xl:table-cell">
                          <div className="space-y-1">
                            <EvidenceStatusBadge evidenceStatus={d.evidenceStatus} />
                            {(d.planSectionId || d.actionItemId) ? (
                              <div className="text-xs text-muted-foreground">
                                {d.planSectionId ? <div className="font-mono">{`Section: ${d.planSectionId}`}</div> : null}
                                {d.actionItemId ? <div className="font-mono">{`Action: ${d.actionItemId}`}</div> : null}
                              </div>
                            ) : null}
                          </div>
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
                        <TableCell className="text-right">
                          <div className="flex flex-wrap justify-end gap-2">
                            <Button
                              type="button"
                              variant="secondary"
                              size="sm"
                              onClick={() => setEditingId(editingId === d.id ? null : d.id)}
                            >
                              {editingId === d.id ? "Close" : "Edit"}
                            </Button>
                            <DocumentArchiveButton
                              documentId={d.id}
                              documentLabel={rowTitle(d)}
                              onArchived={() => {
                                setEditingId((cur) => (cur === d.id ? null : cur));
                                onDocumentArchived(d.id);
                              }}
                            />
                          </div>
                        </TableCell>
                      </TableRow>
                      {editingId === d.id ? (
                        <TableRow>
                          <TableCell colSpan={7} className="bg-slate-50/50 p-3">
                            <DocumentEditForm
                              document={d}
                              planSections={planSections}
                              actionItems={actionItems}
                              onSaved={(updated) => {
                                onDocumentUpdated(updated);
                                setEditingId(null);
                              }}
                              onCancel={() => setEditingId(null)}
                            />
                          </TableCell>
                        </TableRow>
                      ) : null}
                    </Fragment>
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
                    <p className="font-medium text-slate-900">{rowTitle(d)}</p>
                    <DocumentCategoryBadge category={d.category} />
                  </div>
                          <div className="mt-2">
                            <EvidenceStatusBadge evidenceStatus={d.evidenceStatus} />
                          </div>
                  <p className="mt-1 font-mono text-xs text-slate-600">{d.originalFileName ?? "—"}</p>
                  <div className="mt-2 flex flex-wrap items-center gap-3 text-xs text-muted-foreground">
                    <DocumentSize bytes={d.sizeBytes} />
                    <span>{d.contentType ?? "—"}</span>
                    <span>{formatWhen(d.uploadedAtUtc, d.createdAtUtc)}</span>
                  </div>
                          {(d.planSectionId || d.actionItemId) ? (
                            <div className="mt-2 text-xs text-muted-foreground">
                              {d.planSectionId ? <div className="font-mono">{`Section: ${d.planSectionId}`}</div> : null}
                              {d.actionItemId ? <div className="font-mono">{`Action: ${d.actionItemId}`}</div> : null}
                            </div>
                          ) : null}
                  <div className="mt-3 flex flex-wrap gap-2">
                    <Button type="button" variant="secondary" size="sm" onClick={() => setEditingId(editingId === d.id ? null : d.id)}>
                      {editingId === d.id ? "Close edit" : "Edit"}
                    </Button>
                    <DocumentArchiveButton
                      documentId={d.id}
                      documentLabel={rowTitle(d)}
                      onArchived={() => {
                        setEditingId((cur) => (cur === d.id ? null : cur));
                        onDocumentArchived(d.id);
                      }}
                    />
                  </div>
                  {editingId === d.id ? (
                    <div className="mt-3">
                      <DocumentEditForm
                        document={d}
                        planSections={planSections}
                        actionItems={actionItems}
                        onSaved={(updated) => {
                          onDocumentUpdated(updated);
                          setEditingId(null);
                        }}
                        onCancel={() => setEditingId(null)}
                      />
                    </div>
                  ) : null}
                </li>
              ))}
            </ul>
          </>
        )}
      </CardContent>
    </Card>
  );
}
