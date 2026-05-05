import { apiClient } from "@/lib/api/api-client";
import { endpoints } from "@/lib/api/endpoints";
import { setUnauthorizedHandler } from "@/lib/api/http";
import { ApiError } from "@/lib/api/api-error";
import { config } from "@/lib/config";
import type { LoginRequest } from "@/lib/auth/auth-types";
import type { AuthSession } from "@/lib/auth/auth-types";
import { parseLoginResponse } from "@/lib/auth/auth-types";
import {
  clearAuthSession,
  getAuthSession,
  setAuthSession
} from "@/lib/auth/auth-storage";

// Auth client uses memory-only session storage (via auth-storage).
// Refresh token never touches JS; only the HttpOnly cookie set by backend.

export async function login(request: LoginRequest): Promise<AuthSession> {
  const { token, user } = await apiClient.login(request);
  const session: AuthSession = { token, user };
  setAuthSession(session);
  return session;
}

export async function refreshSession(): Promise<AuthSession> {
  const url = `${config.apiBaseUrl}${endpoints.authRefresh()}`;
  const response = await fetch(url, {
    method: "POST",
    credentials: "include",
    headers: {
      Accept: "application/json"
    }
  });

  if (!response.ok) {
    throw await ApiError.fromResponse(response);
  }

  const data: unknown = await response.json();
  const parsed = parseLoginResponse(data);
  const session: AuthSession = { token: parsed.token, user: parsed.user };
  setAuthSession(session);
  return session;
}

export async function logout(): Promise<void> {
  try {
    const url = `${config.apiBaseUrl}${endpoints.authLogout()}`;
    await fetch(url, {
      method: "POST",
      credentials: "include"
    });
  } finally {
    clearAuthSession();
  }
}

export function getCurrentSession(): AuthSession | null {
  return getAuthSession();
}

// Register 401 refresh handler after refreshSession is defined (no TDZ)
setUnauthorizedHandler(async () => {
  try {
    await refreshSession();
    return true;
  } catch {
    clearAuthSession();
    return false;
  }
});
