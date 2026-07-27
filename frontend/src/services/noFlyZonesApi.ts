import { apiRequest } from "./apiClient";
import type { NoFlyZoneRequest, NoFlyZoneResponse } from "../types/api";

export async function getNoFlyZones(): Promise<NoFlyZoneResponse[]> {
  return apiRequest<NoFlyZoneResponse[]>("/api/no-fly-zones");
}

export async function createNoFlyZone(request: NoFlyZoneRequest): Promise<NoFlyZoneResponse> {
  return apiRequest<NoFlyZoneResponse>("/api/no-fly-zones", {
    method: "POST",
    body: JSON.stringify(request)
  });
}

export async function updateNoFlyZone(id: number, request: NoFlyZoneRequest): Promise<NoFlyZoneResponse> {
  return apiRequest<NoFlyZoneResponse>(`/api/no-fly-zones/${id}`, {
    method: "PUT",
    body: JSON.stringify(request)
  });
}

export async function deleteNoFlyZone(id: number): Promise<void> {
  await apiRequest<void>(`/api/no-fly-zones/${id}`, {
    method: "DELETE"
  });
}
