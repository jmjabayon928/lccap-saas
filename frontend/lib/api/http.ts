import { config } from "@/lib/config";
import { ApiError } from "@/lib/api/api-error";
import { getAccessToken } from "@/lib/auth/auth-storage";

type UnauthorizedHandler = () => Promise<boolean>;

let unauthorizedHandler: UnauthorizedHandler | null = null;

export function setUnauthorizedHandler(handler: UnauthorizedHandler | null): void {
  unauthorizedHandler = handler;
}

export type HttpMethod = "GET" | "POST" | "PUT";

export interface RequestJsonOptions {
  readonly method: HttpMethod;
  readonly path: string;
  readonly body?: unknown;
  readonly signal?: AbortSignal;
}

export interface RequestFormDataOptions {
  readonly method: "POST" | "PUT";
  readonly path: string;
  readonly body: FormData;
  readonly signal?: AbortSignal;
}

function buildUrl(path: string): string {
  const base = config.apiBaseUrl;
  const prefix = path.startsWith("/") ? path : `/${path}`;
  return `${base}${prefix}`;
}

function isAuthEndpoint(path: string): boolean {
  return (
    path.includes("/api/auth/login") ||
    path.includes("/api/auth/refresh") ||
    path.includes("/api/auth/logout")
  );
}

async function withUnauthorizedRetry<T>(path: string, perform: () => Promise<T>): Promise<T> {
  try {
    return await perform();
  } catch (err) {
    if (err instanceof ApiError && err.status === 401 && !isAuthEndpoint(path) && unauthorizedHandler) {
      const refreshed = await unauthorizedHandler();
      if (refreshed) {
        return await perform();
      }
    }
    throw err;
  }
}

function authHeaders(): HeadersInit {
  const token = getAccessToken();
  if (!token) {
    return {};
  }
  return { Authorization: `Bearer ${token}` };
}

async function parseOkJsonBody(response: Response): Promise<unknown> {
  const text = await response.text();
  if (!text.trim()) {
    return null;
  }
  try {
    return JSON.parse(text) as unknown;
  } catch {
    throw new ApiError("Response was not valid JSON", response.status, text.slice(0, 500));
  }
}

export async function requestJson(options: RequestJsonOptions): Promise<unknown> {
  const { method, path, body, signal } = options;
  const headers: HeadersInit = {
    ...authHeaders(),
    Accept: "application/json",
    ...(body !== undefined ? { "Content-Type": "application/json" } : {})
  };

  const perform = async () => {
    const response = await fetch(buildUrl(path), {
      method,
      headers,
      body: body !== undefined ? JSON.stringify(body) : undefined,
      signal,
      credentials: "omit"
    });

    if (!response.ok) {
      throw await ApiError.fromResponse(response);
    }

    return parseOkJsonBody(response);
  };

  return withUnauthorizedRetry(path, perform);
}

export async function requestFormData(options: RequestFormDataOptions): Promise<unknown> {
  const { method, path, body, signal } = options;
  const headers: HeadersInit = {
    ...authHeaders(),
    Accept: "application/json"
  };

  const perform = async () => {
    const response = await fetch(buildUrl(path), {
      method,
      headers,
      body,
      signal,
      credentials: "omit"
    });

    if (!response.ok) {
      throw await ApiError.fromResponse(response);
    }

    return parseOkJsonBody(response);
  };

  return withUnauthorizedRetry(path, perform);
}

export async function requestVoid(path: string, signal?: AbortSignal): Promise<void> {
  const perform = async () => {
    const response = await fetch(buildUrl(path), {
      method: "DELETE",
      headers: {
        ...authHeaders(),
        Accept: "application/json"
      },
      signal,
      credentials: "omit"
    });

    if (!response.ok) {
      throw await ApiError.fromResponse(response);
    }
  };

  return withUnauthorizedRetry(path, perform);
}

export const http = {
  get: (path: string, signal?: AbortSignal) => requestJson({ method: "GET", path, signal }),
  postJson: (path: string, body: unknown, signal?: AbortSignal) =>
    requestJson({ method: "POST", path, body, signal }),
  putJson: (path: string, body: unknown, signal?: AbortSignal) =>
    requestJson({ method: "PUT", path, body, signal }),
  deleteVoid: (path: string, signal?: AbortSignal) => requestVoid(path, signal),
  postFormData: (path: string, body: FormData, signal?: AbortSignal) =>
    requestFormData({ method: "POST", path, body, signal }),
  putFormData: (path: string, body: FormData, signal?: AbortSignal) =>
    requestFormData({ method: "PUT", path, body, signal })
} as const;
