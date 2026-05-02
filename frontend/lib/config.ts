const DEFAULT_API_BASE_URL = "http://localhost:5000";

function normalizeBaseUrl(url: string): string {
  return url.replace(/\/+$/, "");
}

function readPublicApiBaseUrl(): string {
  const raw = process.env.NEXT_PUBLIC_API_BASE_URL?.trim();
  if (!raw) {
    return normalizeBaseUrl(DEFAULT_API_BASE_URL);
  }
  return normalizeBaseUrl(raw);
}

export const config = {
  apiBaseUrl: readPublicApiBaseUrl()
} as const;

export type AppConfig = typeof config;
