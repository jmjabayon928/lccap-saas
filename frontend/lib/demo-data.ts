import type { DashboardData } from "@/types/dashboard";

export const demoDashboardData: DashboardData = {
  plan: {
    id: "plan-2026-marikina",
    title: "Marikina City LCCAP 2026-2030",
    lguName: "Demo LGU",
    targetYear: 2030,
    completionPercent: 68,
    sectionsCompleted: 11,
    totalSections: 16,
    uploadedDocuments: 27,
    monitoringIndicators: 42
  },
  sections: [
    { sectionKey: "ghg", sectionName: "GHG Inventory", status: "complete", completionPercent: 100 },
    { sectionKey: "hazard", sectionName: "Hazard and Vulnerability", status: "in_progress", completionPercent: 72 },
    { sectionKey: "adaptation", sectionName: "Adaptation Actions", status: "in_progress", completionPercent: 64 },
    { sectionKey: "mitigation", sectionName: "Mitigation Program", status: "needs_attention", completionPercent: 43 },
    { sectionKey: "finance", sectionName: "Climate Finance", status: "in_progress", completionPercent: 58 }
  ],
  actionStatuses: [
    { status: "planned", label: "Planned", count: 18 },
    { status: "in_progress", label: "In Progress", count: 24 },
    { status: "at_risk", label: "At Risk", count: 6 },
    { status: "done", label: "Completed", count: 31 }
  ],
  risks: [
    {
      id: "risk-001",
      title: "Flood sensor replacement procurement delay",
      owner: "City DRRMO",
      level: "high",
      dueDate: "2026-05-20"
    },
    {
      id: "risk-002",
      title: "Barangay mobility survey completion lag",
      owner: "Planning Office",
      level: "medium",
      dueDate: "2026-05-28"
    },
    {
      id: "risk-003",
      title: "Incomplete baseline for waste diversion",
      owner: "ENRO",
      level: "medium",
      dueDate: "2026-06-03"
    }
  ],
  recentActivity: [
    {
      id: "act-1",
      title: "Updated adaptation section narrative and targets",
      actor: "Climate Planner",
      timestamp: "2 hours ago",
      category: "plan"
    },
    {
      id: "act-2",
      title: "Uploaded flood susceptibility map package",
      actor: "GIS Analyst",
      timestamp: "5 hours ago",
      category: "document"
    },
    {
      id: "act-3",
      title: "Marked 3 transport actions as in progress",
      actor: "Sector Lead",
      timestamp: "Yesterday",
      category: "action"
    },
    {
      id: "act-4",
      title: "Added indicator: households with elevated flooring",
      actor: "Monitoring Officer",
      timestamp: "Yesterday",
      category: "monitoring"
    }
  ],
  exportReadiness: {
    scorePercent: 81,
    blockers: 2,
    notes: "Finalize mitigation cost assumptions and attach signed endorsement before PDF export."
  }
};
