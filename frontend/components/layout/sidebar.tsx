"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import type { ComponentType } from "react";
import { LayoutDashboard, FileText, FolderOpen, CheckSquare, Activity, FileOutput, Map, Bot, History } from "lucide-react";
import { cn } from "@/lib/utils";
import { Badge } from "@/components/ui/badge";
import { useAuthSession } from "@/lib/auth/use-auth-session";

interface NavItem {
  label: string;
  href: string;
  icon: ComponentType<{ className?: string }>;
  future?: boolean;
}

const navItems: NavItem[] = [
  { label: "Dashboard", href: "/dashboard", icon: LayoutDashboard },
  { label: "Plans", href: "/plans", icon: FileText },
  { label: "Plan Workspace", href: "/plans", icon: FolderOpen },
  { label: "Documents", href: "/documents", icon: FileText },
  { label: "Actions", href: "/actions", icon: CheckSquare },
  { label: "Monitoring", href: "/monitoring", icon: Activity },
  { label: "Exports", href: "/exports", icon: FileOutput },
  { label: "Audit History", href: "/audit", icon: History },
  { label: "Future: Maps", href: "/dashboard", icon: Map, future: true },
  { label: "Future: AI Assistant", href: "/dashboard", icon: Bot, future: true }
];

export function Sidebar() {
  const pathname = usePathname();
  const { session } = useAuthSession();
  const role = session?.user.role;

  const filteredItems = navItems.filter((item) => {
    if (item.href === "/audit") {
      return role === "Admin" || role === "Reviewer";
    }
    return true;
  });

  return (
    <aside className="h-full border-r border-border bg-white p-3 md:p-4">
      <div className="mb-6">
        <p className="text-sm font-semibold text-slate-900">LCCAP SaaS</p>
        <p className="text-xs text-muted-foreground">Demo LGU Command Center</p>
      </div>
      <nav className="space-y-1">
        {filteredItems.map((item) => {
          const Icon = item.icon;
          const active = pathname === item.href || (item.href !== "/dashboard" && pathname.startsWith(item.href));
          return (
            <Link
              key={item.label}
              href={item.href}
              className={cn(
                "flex items-center justify-between rounded-md px-3 py-2 text-sm transition-colors",
                active ? "bg-emerald-50 text-emerald-900" : "text-slate-700 hover:bg-slate-100"
              )}
            >
              <span className="flex items-center gap-2">
                <Icon className="h-4 w-4" />
                {item.label}
              </span>
              {item.future ? <Badge variant="outline">Soon</Badge> : null}
            </Link>
          );
        })}
      </nav>
    </aside>
  );
}
