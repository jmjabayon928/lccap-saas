"use client";

import { useCallback, useEffect, useState } from "react";
import * as authClient from "@/lib/auth/auth-client";
import { getAuthSession } from "@/lib/auth/auth-storage";
import type { AuthSession } from "@/lib/auth/auth-types";

export interface UseAuthSessionResult {
  /** In-memory session (populated on mount via refresh cookie if needed).
   * Prefer getAccessToken() for API calls; session is for UI only. */
  readonly session: AuthSession | null;
  readonly isAuthenticated: boolean;
  readonly isLoading: boolean;
  readonly logout: () => Promise<void>;
  readonly refreshSession: () => void;
}

export function useAuthSession(): UseAuthSessionResult {
  const [session, setSession] = useState<AuthSession | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // Exposed refreshSession keeps previous shape (void) for compatibility.
  // It syncs the hook state from current memory session (no network).
  const refreshSession = useCallback(() => {
    setSession(getAuthSession());
  }, []);

  // On mount: if memory already has session (SPA nav), use it.
  // Otherwise attempt silent refresh using HttpOnly cookie to restore.
  // This prevents logged-out flash on hard reload while keeping isLoading.
  useEffect(() => {
    let cancelled = false;

    async function restoreOrRefresh() {
      const existing = getAuthSession();
      if (existing) {
        if (!cancelled) {
          setSession(existing);
          setIsLoading(false);
        }
        return;
      }

      try {
        const restored = await authClient.refreshSession();
        if (!cancelled) {
          setSession(restored);
        }
      } catch {
        // No valid refresh cookie or refresh failed: remain unauthenticated.
        // auth-client / handler already cleared memory on failure path.
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    }

    restoreOrRefresh();

    return () => {
      cancelled = true;
    };
  }, []);

  const logout = useCallback(async () => {
    await authClient.logout();
    setSession(null);
  }, []);

  return {
    session,
    isAuthenticated: session !== null,
    isLoading,
    logout,
    refreshSession
  };
}
