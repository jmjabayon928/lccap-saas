import type { Metadata } from "next";
import { Leaf, Lock, Shield } from "lucide-react";
import { LoginForm } from "@/components/auth/login-form";

export const metadata: Metadata = {
  title: "Sign in — LCCAP SaaS",
  description: "Enterprise Local Climate Action Planning Platform"
};

export default function LoginPage() {
  return (
    <div className="flex min-h-screen bg-slate-50">
      <div className="hidden w-[42%] flex-col justify-between border-r border-border bg-gradient-to-br from-emerald-900 via-teal-900 to-slate-900 p-10 text-white lg:flex">
        <div>
          <div className="flex items-center gap-2">
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-white/10 backdrop-blur">
              <Leaf className="h-6 w-6" aria-hidden />
            </div>
            <div>
              <p className="text-lg font-semibold tracking-tight">LCCAP SaaS</p>
              <p className="text-sm text-emerald-100/90">Enterprise Local Climate Action Planning Platform</p>
            </div>
          </div>
        </div>
        <div className="space-y-6">
          <blockquote className="text-lg font-medium leading-relaxed text-emerald-50/95">
            Coordinate adaptation and mitigation programs with a tenant-scoped workspace built for LGU climate
            planning cycles.
          </blockquote>
          <ul className="space-y-3 text-sm text-emerald-100/85">
            <li className="flex gap-2">
              <Shield className="mt-0.5 h-4 w-4 shrink-0" aria-hidden />
              <span>Tenant-scoped access — data stays within your organization boundary.</span>
            </li>
            <li className="flex gap-2">
              <Lock className="mt-0.5 h-4 w-4 shrink-0" aria-hidden />
              <span>JWT-secured API — bearer tokens issued by the LCCAP identity service.</span>
            </li>
            <li className="flex gap-2">
              <Shield className="mt-0.5 h-4 w-4 shrink-0" aria-hidden />
              <span>MVP local session storage — token held in the browser for development; migrate to httpOnly cookies for production hardening.</span>
            </li>
          </ul>
        </div>
        <p className="text-xs text-emerald-200/70">© {new Date().getFullYear()} LCCAP SaaS — climate planning workspace.</p>
      </div>

      <div className="flex flex-1 flex-col justify-center px-6 py-12 sm:px-10 lg:px-16">
        <div className="mx-auto w-full max-w-md space-y-8">
          <div className="lg:hidden">
            <p className="text-xl font-semibold text-slate-900">LCCAP SaaS</p>
            <p className="mt-1 text-sm text-muted-foreground">Enterprise Local Climate Action Planning Platform</p>
          </div>

          <div className="rounded-xl border border-border bg-white p-8 shadow-sm">
            <div className="space-y-2">
              <h1 className="text-2xl font-semibold tracking-tight text-slate-900">Welcome back</h1>
              <p className="text-sm text-muted-foreground">Sign in with your organizational credentials.</p>
            </div>

            <div className="mt-8">
              <LoginForm />
            </div>

            <p className="mt-6 text-center text-xs text-muted-foreground">
              Protected workspace. Unauthorized access is prohibited.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
