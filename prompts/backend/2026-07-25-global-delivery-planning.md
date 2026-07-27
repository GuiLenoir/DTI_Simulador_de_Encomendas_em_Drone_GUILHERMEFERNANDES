# Global Delivery Planning

## Context

The existing Drone Delivery Simulator supported individual order allocation, timestamp-based delivery simulation, MySQL persistence, Docker, React pages, and dashboard polling.

## Prompt

The user asked to implement a global delivery planning queue, multi-order trips, automatic drone selection, battery safety margin, and timestamp-based battery charging simulation while preserving individual order allocation.

## Result

Added queue fields to orders, safety margin and charging fields to drones, trip and trip-order entities, EF configuration, migration, delivery planning endpoints, trip endpoints, planning service, trip state service, charging service, frontend queue/planning actions, dashboard trip sections, and drone margin controls.

## Review

The implementation uses a deterministic heuristic rather than mathematical optimization. It groups compatible queued orders while respecting priority, queue time, payload, range, battery consumption, and battery safety margin in percentage points.

## Related Files

- `backend/DroneDelivery.Api/Models/Trip.cs`
- `backend/DroneDelivery.Api/Models/TripOrder.cs`
- `backend/DroneDelivery.Api/Services/DeliveryPlanningService.cs`
- `backend/DroneDelivery.Api/Services/ChargingService.cs`
- `backend/DroneDelivery.Api/Controllers/DeliveryPlanningController.cs`
- `backend/DroneDelivery.Api/Controllers/TripsController.cs`
- `backend/DroneDelivery.Api/Migrations/20260725170000_AddGlobalDeliveryPlanning.cs`
- `frontend/src/pages/OrderPage.tsx`
- `frontend/src/pages/DashboardPage.tsx`
- `frontend/src/pages/DronePage.tsx`
