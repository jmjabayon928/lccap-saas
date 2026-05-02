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
  if (
    !isNonEmptyString(id) ||
    !isNonEmptyString(email) ||
    !isNonEmptyString(accountId) ||
    !isNonEmptyString(role)
  ) {
    return null;
  }
  return { id, email, accountId, role };
}

export function parseLoginResponse(raw: unknown): LoginResponse {
  if (!isRecord(raw)) {
    throw new Error("Invalid login response: expected object");
  }
  const token = raw.token;
  const userRaw = raw.user;
  if (!isNonEmptyString(token)) {
    throw new Error("Invalid login response: missing token");
  }
  const user = parseAuthUser(userRaw);
  if (!user) {
    throw new Error("Invalid login response: missing or invalid user");
  }
  return { token, user };
}
