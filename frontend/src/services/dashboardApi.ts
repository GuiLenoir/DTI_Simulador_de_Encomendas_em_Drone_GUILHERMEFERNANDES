import { apiRequest } from "./apiClient";
import type { DashboardResponse } from "../types/api";

export function getDashboard(): Promise<DashboardResponse> {
  return apiRequest<DashboardResponse>("/api/dashboard");
}
