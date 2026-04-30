import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import type { ActionStatusSummary } from "@/types/dashboard";

interface ActionStatusCardProps {
  statuses: ActionStatusSummary[];
}

export function ActionStatusCard({ statuses }: ActionStatusCardProps) {
  const total = statuses.reduce((sum, item) => sum + item.count, 0);

  return (
    <Card>
      <CardHeader>
        <CardTitle>Action Items by Status</CardTitle>
        <CardDescription>{total} total tracked actions in the demo workspace.</CardDescription>
      </CardHeader>
      <CardContent>
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Status</TableHead>
              <TableHead className="text-right">Count</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {statuses.map((status) => (
              <TableRow key={status.status}>
                <TableCell>{status.label}</TableCell>
                <TableCell className="text-right font-semibold">{status.count}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </CardContent>
    </Card>
  );
}
