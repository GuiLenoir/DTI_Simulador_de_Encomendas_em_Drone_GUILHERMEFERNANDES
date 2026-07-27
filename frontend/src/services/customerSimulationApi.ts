import { apiRequest } from "./apiClient";
import type { CustomerOrderCreatedResponse, CustomerOrderRequest, CustomerTrackingResponse } from "../types/api";

export function createCustomerOrder(request: CustomerOrderRequest): Promise<CustomerOrderCreatedResponse> {
  return apiRequest<CustomerOrderCreatedResponse>("/api/customer-simulation/orders", {
    method: "POST",
    body: JSON.stringify(request)
  });
}

export function getCustomerTracking(orderId: number): Promise<CustomerTrackingResponse> {
  return apiRequest<CustomerTrackingResponse>(`/api/customer-simulation/orders/${orderId}/tracking`);
}
