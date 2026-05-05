import type { AuthSession } from "@/lib/auth/auth-types";

/** Legacy storage key retained for reference and defensive cleanup only.
 * Access tokens are no longer persisted to localStorage. */
export const AUTH_SESSION_STORAGE_KEY = "lccap.auth.session.v1" as const;

const STORAGE_KEY = AUTH_SESSION_STORAGE_KEY;

// Module-level in-memory session. Never written to localStorage, sessionStorage,
// or any JS-accessible persistent storage. Survives only for current page lifetime.
let currentSession: AuthSession | null = null;

// Defensive one-time cleanup of legacy persisted token from prior MVP versions.
// Runs at module evaluation time in the browser.
if (typeof window !== "undefined") {
  try {
    window.localStorage.removeItem(STORAGE_KEY);
  } catch {
    // Ignore (e.g. private browsing, quota, or disabled storage)
  }
}

function isNonEmptyString(value: unknown): value is string {
  return typeof value === "string" && value.trim().length > 0;
}

export function getAuthSession(): AuthSession | null {
  return currentSession;
}

export function setAuthSession(session: AuthSession): void {
  if (!session || !isNonEmptyString(session.token) || !session.user) {
    return;
  }
  currentSession = {
    token: session.token,
    user: { ...session.user }
  };
}

export function clearAuthSession(): void {
  currentSession = null;
  if (typeof window !== "undefined") {
    try {
      window.localStorage.removeItem(STORAGE_KEY);
    } catch {
      // Ignore storage errors during cleanup
    }
  }
}

export function getAccessToken(): string | null {
  return currentSession?.token ?? null;
}

/** Explicit helper for any external callers that want to force-remove legacy key. */
export function clearPersistedLegacyAuthSession(): void {
  if (typeof window !== "undefined") {
    try {
      window.localStorage.removeItem(STORAGE_KEY);
    } catch {
      // Ignore
    }
  }
}
