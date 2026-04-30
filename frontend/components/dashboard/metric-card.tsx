import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Progress } from "@/components/ui/progress";

interface MetricCardProps {
  title: string;
  value: string;
  description: string;
  progress?: number;
}

export function MetricCard({ title, value, description, progress }: MetricCardProps) {
  return (
    <Card>
      <CardHeader>
        <CardDescription>{title}</CardDescription>
        <CardTitle className="text-2xl">{value}</CardTitle>
      </CardHeader>
      <CardContent>
        <p className="mb-3 text-sm text-muted-foreground">{description}</p>
        {typeof progress === "number" ? <Progress value={progress} /> : null}
      </CardContent>
    </Card>
  );
}
