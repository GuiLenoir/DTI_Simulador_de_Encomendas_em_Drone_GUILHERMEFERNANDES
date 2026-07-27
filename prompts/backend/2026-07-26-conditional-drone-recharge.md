# Conditional Drone Recharge

## Context

Drones previously started charging after every completed delivery or trip when battery was below 100%.

## Prompt

Change recharge behavior so drones only start charging after a trip when they cannot serve any pending order with their current battery. If they can serve at least one order, they should remain available instead of charging.

## Result

Added a shared drone/order capability service and used it from individual delivery completion and trip completion. The service validates pending orders against capacity, route availability, range, battery consumption, and global safety margin.

## Review

Accepted with updated tests and documentation. Existing timestamp-based charging remains unchanged when recharge is actually needed.

## Related Files

- `backend/DroneDelivery.Api/Services/DroneOrderCapabilityService.cs`
- `backend/DroneDelivery.Api/Services/DeliveryService.cs`
- `backend/DroneDelivery.Api/Services/DeliveryPlanningService.cs`
- `backend/DroneDelivery.Tests/DeliveryServiceTests.cs`
- `backend/DroneDelivery.Tests/DeliveryPlanningServiceTests.cs`
