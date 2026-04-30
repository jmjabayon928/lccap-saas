import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";

export default function DocumentsPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="page-title">Documents</h1>
        <p className="page-description">Upload and organize references, maps, and annexes per plan.</p>
      </div>
      <div className="grid gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Upload Queue</CardTitle>
            <CardDescription>Future integration with `POST /api/documents/upload` for document ingestion.</CardDescription>
          </CardHeader>
          <CardContent>
            <Badge variant="secondary">MVP backend ready</Badge>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle>Document Library</CardTitle>
            <CardDescription>Plan-linked inventory from GET /api/plans/:planId/documents.</CardDescription>
          </CardHeader>
          <CardContent className="text-sm text-muted-foreground">
            Filters for document type, section relevance, and upload status will appear here.
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
