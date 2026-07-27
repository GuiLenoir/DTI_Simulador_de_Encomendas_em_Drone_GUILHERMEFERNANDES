import { apiRequest } from "./apiClient";
import type { DroneRequest, DroneResponse, DroneSettingsResponse } from "../types/api";

export function getDrones(): Promise<DroneResponse[]> {
  return apiRequest<DroneResponse[]>("/api/drones/status");
}

export function createDrone(request: DroneRequest): Promise<DroneResponse> {
  return apiRequest<DroneResponse>("/api/drones", {
    method: "POST",
    body: JSON.stringify(request)
  });
}

export function updateDrone(id: number, request: DroneRequest): Promise<DroneResponse> {
  return apiRequest<DroneResponse>(`/api/drones/${id}`, {
    method: "PUT",
    body: JSON.stringify(request)
  });
}

export function activateDrone(id: number): Promise<DroneResponse> {
  return apiRequest<DroneResponse>(`/api/drones/${id}/activate`, { method: "PATCH" });
}

export function deactivateDrone(id: number): Promise<DroneResponse> {
  return apiRequest<DroneResponse>(`/api/drones/${id}/deactivate`, { method: "PATCH" });
}

export function deleteDrone(id: number): Promise<void> {
  return apiRequest<void>(`/api/drones/${id}`, { method: "DELETE" });
}

export function getDroneSettings(): Promise<DroneSettingsResponse> {
  return apiRequest<DroneSettingsResponse>("/api/drone-settings");
}

export function updateDroneSettings(batterySafetyMarginPercentagePoints: number): Promise<DroneSettingsResponse> {
  return apiRequest<DroneSettingsResponse>("/api/drone-settings", {
    method: "PUT",
    body: JSON.stringify({ batterySafetyMarginPercentagePoints })
  });
}
