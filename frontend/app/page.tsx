import Link from "next/link";
import { ArrowRight } from "lucide-react";
import { buttonVariants } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";

export default function HomePage() {
  return (
    <div className="mx-auto max-w-4xl py-12">
      <Card className="overflow-hidden">
        <CardHeader className="bg-gradient-to-r from-emerald-700 to-teal-700 text-white">
          <CardTitle className="text-3xl">LCCAP SaaS Command Center</CardTitle>
          <CardDescription className="text-emerald-50">
            Climate planning workspace for LGU implementation, monitoring, and export readiness.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-6 p-8">
          <p className="text-slate-700">
            This frontend foundation includes dashboard intelligence views, planning workspace navigation, and enterprise
            UI components ready for future API wiring.
          </p>
          <Link
            href="/dashboard"
            className={cn(buttonVariants({ variant: "default", size: "default" }), "w-full sm:w-auto")}
          >
            Open Dashboard
            <ArrowRight className="h-4 w-4" />
          </Link>
        </CardContent>
      </Card>
    </div>
  );
}
