export type RiskLevel = "low" | "medium" | "high";
export type ActionStatus = "planned" | "in_progress" | "at_risk" | "done";
export type SectionStatus = "complete" | "in_progress" | "needs_attention";

export interface PlanSummary {
  id: string;
  title: string;
  lguName: string;
  targetYear: number;
  completionPercent: number;
  sectionsCompleted: number;
  totalSections: number;
  uploadedDocuments: number;
  monitoringIndicators: number;
}

export interface SectionProgress {
  sectionKey: string;
  sectionName: string;
  status: SectionStatus;
  completionPercent: number;
}

export interface ActionStatusSummary {
  status: ActionStatus;
  label: string;
  count: number;
}

export interface RiskItem {
  id: string;
  title: string;
  owner: string;
  level: RiskLevel;
  dueDate: string;
}

export interface ActivityItem {
  id: string;
  title: string;
  actor: string;
  timestamp: string;
  category: "plan" | "document" | "action" | "monitoring" | "export";
}

export interface ExportReadiness {
  scorePercent: number;
  blockers: number;
  notes: string;
}

export interface DashboardData {
  plan: PlanSummary;
  sections: SectionProgress[];
  actionStatuses: ActionStatusSummary[];
  risks: RiskItem[];
  recentActivity: ActivityItem[];
  exportReadiness: ExportReadiness;
}
