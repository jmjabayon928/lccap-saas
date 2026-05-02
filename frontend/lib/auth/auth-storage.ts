import type { AuthSession } from "@/lib/auth/auth-types";

/** Single localStorage key for the MVP auth payload; must match `storage` event listeners. */
export const AUTH_SESSION_STORAGE_KEY = "lccap.auth.session.v1" as const;

const STORAGE_KEY = AUTH_SESSION_STORAGE_KEY;

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function isNonEmptyString(value: unknown): value is string {
  return typeof value === "string" && value.trim().length > 0;
}

function parseStoredSession(raw: unknown): AuthSession | null {
  if (!isRecord(raw)) {
    return null;
  }
  const token = raw.token;
  const userRaw = raw.user;
  if (!isNonEmptyString(token) || !isRecord(userRaw)) {
    return null;
  }
  const id = userRaw.id;
  const email = userRaw.email;
  const accountId = userRaw.accountId;
  const role = userRaw.role;
  const fullName = userRaw.fullName;

  if (
    !isNonEmptyString(id) ||
    !isNonEmptyString(email) ||
    !isNonEmptyString(accountId) ||
    !isNonEmptyString(role) ||
    !isNonEmptyString(fullName)
  ) {
    return null;
  }
  return {
    token,
    user: {
      id,
      email,
      accountId,
      role,
      fullName
    }
  };
}

export function getAuthSession(): AuthSession | null {
  if (typeof window === "undefined") {
    return null;
  }
  try {
    const rawJson = window.localStorage.getItem(STORAGE_KEY);
    if (!rawJson) {
      return null;
    }
    const parsed: unknown = JSON.parse(rawJson);
    return parseStoredSession(parsed);
  } catch {
    return null;
  }
}

export function setAuthSession(session: AuthSession): void {
  if (typeof window === "undefined") {
    return;
  }
  const payload: AuthSession = {
    token: session.token,
    user: { ...session.user }
  };
  window.localStorage.setItem(STORAGE_KEY, JSON.stringify(payload));
}

export function clearAuthSession(): void {
  if (typeof window === "undefined") {
    return;
  }
  window.localStorage.removeItem(STORAGE_KEY);
}

export function getAccessToken(): string | null {
  return getAuthSession()?.token ?? null;
}
