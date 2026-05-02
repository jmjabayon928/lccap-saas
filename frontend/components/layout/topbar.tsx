"use client";

import { useRouter } from "next/navigation";
import { Bell, Building2, LogOut, UserCircle2 } from "lucide-react";
import Link from "next/link";
import { Badge } from "@/components/ui/badge";
import { Button, buttonVariants } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { useAuthSession } from "@/lib/auth/use-auth-session";

export function Topbar() {
  const router = useRouter();
  const { session, logout, isLoading } = useAuthSession();

  function handleLogout() {
    logout();
    router.push("/login");
    router.refresh();
  }

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
        <button type="button" className="rounded-md p-2 text-slate-500 transition-colors hover:bg-slate-100">
          <Bell className="h-4 w-4" />
        </button>

        {!isLoading && session ? (
          <div className="flex items-center gap-2">
            <div className="flex max-w-[200px] items-center gap-2 rounded-md border border-border px-3 py-1.5 md:max-w-xs">
              <UserCircle2 className="h-4 w-4 shrink-0 text-emerald-700" />
              <div className="min-w-0 text-left">
                <p className="truncate text-sm font-medium text-slate-900">{session.user.email}</p>
                <p className="truncate text-xs text-muted-foreground">{session.user.role}</p>
              </div>
            </div>
            <Button type="button" variant="outline" size="sm" className="gap-1.5" onClick={handleLogout}>
              <LogOut className="h-4 w-4" />
              <span className="hidden sm:inline">Log out</span>
            </Button>
          </div>
        ) : !isLoading ? (
          <Link href="/login" className={cn(buttonVariants({ variant: "outline", size: "sm" }))}>
            Login
          </Link>
        ) : (
          <div className="h-9 w-20 animate-pulse rounded-md bg-slate-100" aria-hidden />
        )}
      </div>
    </header>
  );
}
