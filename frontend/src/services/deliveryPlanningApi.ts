import { apiRequest } from "./apiClient";
import type { DeliveryPlanningResponse, TripResponse, UpcomingTripsResponse } from "../types/api";

export function planDeliveries(): Promise<DeliveryPlanningResponse> {
  return apiRequest<DeliveryPlanningResponse>("/api/delivery-planning/plan", {
    method: "POST"
  });
}

export function getTrips(): Promise<TripResponse[]> {
  return apiRequest<TripResponse[]>("/api/trips");
}

export function getUpcomingTrips(): Promise<UpcomingTripsResponse> {
  return apiRequest<UpcomingTripsResponse>("/api/trips/upcoming");
}
