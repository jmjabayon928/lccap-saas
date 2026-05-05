"use client";

import { useCallback, useEffect, useState } from "react";
import * as authClient from "@/lib/auth/auth-client";
import { AUTH_SESSION_STORAGE_KEY, getAuthSession } from "@/lib/auth/auth-storage";
import type { AuthSession } from "@/lib/auth/auth-types";

export interface UseAuthSessionResult {
  /** Session from storage after mount; includes credentials held for API use — prefer `getAccessToken()` from auth-storage for bearer injection, not ad hoc reads of `session.token`. */
  readonly session: AuthSession | null;
  readonly isAuthenticated: boolean;
  readonly isLoading: boolean;
  readonly logout: () => Promise<void>;
  readonly refreshSession: () => void;
}

function shouldReactToStorageKey(key: string | null): boolean {
  return key === null || key === AUTH_SESSION_STORAGE_KEY;
}

export function useAuthSession(): UseAuthSessionResult {
  const [session, setSession] = useState<AuthSession | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const refreshSession = useCallback(() => {
    setSession(getAuthSession());
  }, []);

  useEffect(() => {
    refreshSession();
    setIsLoading(false);
  }, [refreshSession]);

  useEffect(() => {
    function onStorage(ev: StorageEvent): void {
      if (ev.storageArea !== window.localStorage) {
        return;
      }
      if (!shouldReactToStorageKey(ev.key)) {
        return;
      }
      refreshSession();
    }

    window.addEventListener("storage", onStorage);
    return () => window.removeEventListener("storage", onStorage);
  }, [refreshSession]);

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
