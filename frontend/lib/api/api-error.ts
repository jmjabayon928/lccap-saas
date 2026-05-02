export interface ApiErrorBody {
  readonly status: number;
  readonly message: string;
  readonly details?: unknown;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function pickMessageFromUnknownBody(body: unknown): string | undefined {
  if (!isRecord(body)) {
    return undefined;
  }
  const msg = body.message ?? body.error ?? body.title;
  if (typeof msg === "string" && msg.trim().length > 0) {
    return msg;
  }
  return undefined;
}

export class ApiError extends Error {
  readonly status: number;
  readonly details?: unknown;

  constructor(message: string, status: number, details?: unknown) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.details = details;
  }

  static async fromResponse(response: Response): Promise<ApiError> {
    const status = response.status;
    const rawText = await response.text().catch(() => "");

    if (!rawText.trim()) {
      return new ApiError(response.statusText || `Request failed (${status})`, status);
    }

    let parsed: unknown;
    try {
      parsed = JSON.parse(rawText) as unknown;
    } catch {
      return new ApiError(rawText.slice(0, 500), status, rawText);
    }

    const fromJson = pickMessageFromUnknownBody(parsed);
    const message = fromJson ?? response.statusText ?? `Request failed (${status})`;
    return new ApiError(message, status, parsed);
  }

  toJSON(): ApiErrorBody {
    return {
      status: this.status,
      message: this.message,
      ...(this.details !== undefined ? { details: this.details } : {})
    };
  }
}

export function isApiError(value: unknown): value is ApiError {
  return value instanceof ApiError;
}
