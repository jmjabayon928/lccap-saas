import { apiClient } from "@/lib/api/api-client";
import type { LoginRequest } from "@/lib/auth/auth-types";
import type { AuthSession } from "@/lib/auth/auth-types";
import {
  clearAuthSession,
  getAuthSession,
  setAuthSession
} from "@/lib/auth/auth-storage";

export async function login(request: LoginRequest): Promise<AuthSession> {
  const { token, user } = await apiClient.login(request);
  const session: AuthSession = { token, user };
  setAuthSession(session);
  return session;
}

/** Clears persisted session only; callers (e.g. layout chrome) own navigation after sign-out. */
export function logout(): void {
  clearAuthSession();
}

export function getCurrentSession(): AuthSession | null {
  return getAuthSession();
}
