"use client";

import Link from "next/link";
import { ClipboardList } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { buttonVariants } from "@/components/ui/button";
import { Card, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { CreatePlanForm } from "@/components/plans/create-plan-form";
import { cn } from "@/lib/utils";
import { useAuthSession } from "@/lib/auth/use-auth-session";

export default function PlansPage() {
  const { isAuthenticated, isLoading } = useAuthSession();

  return (
    <div className="space-y-6">
      <div>
        <div className="flex flex-wrap items-center gap-2">
          <h1 className="page-title">Plans</h1>
          <Badge variant="secondary">Workspace</Badge>
        </div>
        <p className="page-description">
          Create a climate action plan to open the full LCCAP workspace — sections, collaboration, and downstream modules
          attach to the plan you register here.
        </p>
      </div>

      {!isLoading && !isAuthenticated ? (
        <Card className="border-amber-200/90 bg-gradient-to-r from-amber-50/90 to-white shadow-sm">
          <CardHeader className="pb-3 pt-5">
            <div className="flex flex-wrap items-start justify-between gap-4">
              <div className="space-y-1">
                <CardTitle className="text-lg text-slate-900">Sign in recommended</CardTitle>
                <CardDescription className="text-base text-slate-600">
                  Creating a plan calls secured tenant APIs. Sign in with your tenant credentials before submitting the
                  form.
                </CardDescription>
              </div>
              <Link href="/login" className={cn(buttonVariants({ variant: "default" }), "inline-flex shrink-0 gap-2")}>
                <ClipboardList className="h-4 w-4" aria-hidden />
                Go to login
              </Link>
            </div>
          </CardHeader>
        </Card>
      ) : null}

      <Card className="border-emerald-100 bg-emerald-50/30">
        <CardHeader className="pb-2">
          <CardTitle className="text-base">Standard LCCAP workspace</CardTitle>
          <CardDescription>
            Create a plan to generate the standard LCCAP workspace sections provisioned by your API. You will be taken to
            the plan workspace when creation succeeds.
          </CardDescription>
        </CardHeader>
      </Card>

      <CreatePlanForm />
    </div>
  );
}
