import { apiRequest } from "./apiClient";
import type { OrderPriority, ReportResponse } from "../types/api";

export type ReportFilters = {
  from?: string;
  to?: string;
  droneId?: number;
  priority?: OrderPriority;
};

export function getReport(filters: ReportFilters): Promise<ReportResponse> {
  const params = new URLSearchParams();
  if (filters.from) params.set("from", filters.from);
  if (filters.to) params.set("to", filters.to);
  if (filters.droneId) params.set("droneId", String(filters.droneId));
  if (filters.priority) params.set("priority", filters.priority);
  const query = params.toString();
  return apiRequest<ReportResponse>(`/api/reports${query ? `?${query}` : ""}`);
}
