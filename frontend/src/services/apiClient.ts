import type { ProblemDetails } from "../types/api";

const API_URL = import.meta.env.VITE_API_URL ?? "";

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status?: number,
    public readonly code?: string
  ) {
    super(message);
  }
}

export async function apiRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_URL}${path}`, {
    headers: {
      "Content-Type": "application/json",
      ...init?.headers
    },
    ...init
  });

  if (!response.ok) {
    let problem: ProblemDetails | undefined;
    try {
      problem = (await response.json()) as ProblemDetails;
    } catch {
      problem = undefined;
    }

    throw new ApiError(
      problem?.detail ?? problem?.title ?? "Não foi possível concluir a solicitação.",
      response.status,
      problem?.type
    );
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}
