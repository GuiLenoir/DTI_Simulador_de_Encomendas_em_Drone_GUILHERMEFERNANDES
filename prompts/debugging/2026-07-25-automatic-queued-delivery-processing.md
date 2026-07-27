# Automatic Queued Delivery Processing

## Context

Global planning created trips for available drones, but orders left in the delivery queue stayed queued after drones completed trips and charging.

## Prompt

Fix the delivery queue so queued orders are automatically planned after drones return and become available, while still minimizing the practical number of trips and respecting existing rules.

## Result

Added a non-destructive queue processing path that does not replan mutable trips or auto-queue new orders. The dashboard endpoint now invokes it before returning state, using the existing frontend polling as the automatic trigger. Completed charging state is synchronized before drone eligibility checks.

## Review

Added a regression test that plans one queued order, leaves another queued, advances the clock until the drone completes charging, and verifies the remaining queued order is planned automatically.

## Related Files

- `backend/DroneDelivery.Api/Services/DeliveryPlanningService.cs`
- `backend/DroneDelivery.Api/Controllers/DashboardController.cs`
- `backend/DroneDelivery.Api/Services/IDeliveryPlanningService.cs`
- `backend/DroneDelivery.Tests/DeliveryPlanningServiceTests.cs`
- `TODO.md`
