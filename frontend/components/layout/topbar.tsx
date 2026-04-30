import { Bell, Building2, UserCircle2 } from "lucide-react";
import { Badge } from "@/components/ui/badge";

export function Topbar() {
  return (
    <header className="sticky top-0 z-20 flex h-16 items-center justify-between border-b border-border bg-white px-4 md:px-6">
      <div>
        <p className="text-sm text-muted-foreground">Tenant</p>
        <p className="font-semibold text-slate-900">Demo LGU</p>
      </div>
      <div className="flex items-center gap-3">
        <Badge variant="secondary" className="hidden sm:inline-flex">
          Climate Planning Workspace
        </Badge>
        <div className="hidden items-center gap-2 rounded-md border border-border px-3 py-1.5 sm:flex">
          <Building2 className="h-4 w-4 text-slate-500" />
          <span className="text-sm text-slate-700">LCCAP SaaS</span>
        </div>
        <button className="rounded-md p-2 text-slate-500 transition-colors hover:bg-slate-100">
          <Bell className="h-4 w-4" />
        </button>
        <div className="flex items-center gap-1 rounded-md border border-border px-3 py-1.5">
          <UserCircle2 className="h-4 w-4 text-emerald-700" />
          <span className="text-sm text-slate-700">Climate Planner</span>
        </div>
      </div>
    </header>
  );
}
