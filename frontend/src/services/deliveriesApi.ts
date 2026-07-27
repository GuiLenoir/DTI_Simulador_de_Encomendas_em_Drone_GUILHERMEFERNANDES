import { apiRequest } from "./apiClient";
import type { DeliveryResponse } from "../types/api";

export function getDeliveryRoutes(): Promise<DeliveryResponse[]> {
  return apiRequest<DeliveryResponse[]>("/api/deliveries/routes");
}

export function allocateDelivery(orderId: number): Promise<DeliveryResponse> {
  return apiRequest<DeliveryResponse>(`/api/deliveries/allocate/${orderId}`, {
    method: "POST"
  });
}
