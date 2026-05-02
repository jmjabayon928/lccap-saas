/**
 * Auth DTOs aligned with `POST /api/auth/login` contract.
 * Optional fields from the wire are ignored; unknown shapes fail closed in `parseLoginResponse`.
 */

export interface LoginRequest {
  readonly email: string;
  readonly password: string;
}

export interface AuthUser {
  readonly id: string;
  readonly email: string;
  readonly accountId: string;
  readonly role: string;
  readonly fullName: string;
}

export interface LoginResponse {
  readonly token: string;
  readonly user: AuthUser;
}

export interface AuthSession {
  readonly token: string;
  readonly user: AuthUser;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function isNonEmptyString(value: unknown): value is string {
  return typeof value === "string" && value.trim().length > 0;
}

function parseAuthUser(raw: unknown): AuthUser | null {
  if (!isRecord(raw)) {
    return null;
  }
  const id = raw.id;
  const email = raw.email;
  const accountId = raw.accountId;
  const role = raw.role;
  const fullName = raw.fullName;

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
    id,
    email,
    accountId,
    role,
    fullName
  };
}

export function parseLoginResponse(raw: unknown): LoginResponse {
  if (!isRecord(raw)) {
    throw new Error("Invalid login response: expected object");
  }

  const token = raw.token;
  if (!isNonEmptyString(token)) {
    throw new Error("Invalid login response: missing token");
  }

  // 1. Try flat response (actual backend shape)
  const userId = raw.userId;
  const accountId = raw.accountId;
  const email = raw.email;
  const role = raw.role;
  const fullName = raw.fullName;

  if (
    isNonEmptyString(userId) &&
    isNonEmptyString(accountId) &&
    isNonEmptyString(email) &&
    isNonEmptyString(role) &&
    isNonEmptyString(fullName)
  ) {
    return {
      token,
      user: {
        id: userId,
        accountId,
        email,
        role,
        fullName
      }
    };
  }

  // 2. Try nested response (legacy/fallback shape)
  const userRaw = raw.user;
  const user = parseAuthUser(userRaw);
  if (user) {
    return { token, user };
  }

  throw new Error("Invalid login response: missing or invalid user data");
}
