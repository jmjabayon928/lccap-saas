import { Badge } from "@/components/ui/badge";

export type SectionDraftStatus = "empty" | "drafted";

function deriveStatus(content: string): SectionDraftStatus {
  return content.trim().length === 0 ? "empty" : "drafted";
}

interface SectionStatusBadgeProps {
  readonly content: string;
  readonly className?: string;
}

export function SectionStatusBadge({ content, className }: SectionStatusBadgeProps) {
  const status = deriveStatus(content);
  if (status === "empty") {
    return (
      <Badge variant="outline" className={className}>
        Empty · Not started
      </Badge>
    );
  }
  return (
    <Badge variant="secondary" className={className}>
      Drafted · In progress
    </Badge>
  );
}
