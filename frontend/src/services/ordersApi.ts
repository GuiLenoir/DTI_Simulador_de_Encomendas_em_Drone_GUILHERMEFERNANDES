import { apiRequest } from "./apiClient";
import type { CreateOrderRequest, OrderResponse } from "../types/api";

export function getOrders(): Promise<OrderResponse[]> {
  return apiRequest<OrderResponse[]>("/api/orders");
}

export function createOrder(request: CreateOrderRequest): Promise<OrderResponse> {
  return apiRequest<OrderResponse>("/api/orders", {
    method: "POST",
    body: JSON.stringify(request)
  });
}

export function queueOrder(orderId: number): Promise<OrderResponse> {
  return apiRequest<OrderResponse>(`/api/orders/${orderId}/queue`, {
    method: "POST"
  });
}

export function removeOrderFromQueue(orderId: number): Promise<OrderResponse> {
  return apiRequest<OrderResponse>(`/api/orders/${orderId}/queue`, {
    method: "DELETE"
  });
}
