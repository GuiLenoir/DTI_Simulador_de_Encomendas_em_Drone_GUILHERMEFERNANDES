# Planning Execution Strategy

## Context

The MySQL provider is configured with retry-on-failure execution strategy. The global delivery planning flow uses a manual transaction while recalculating mutable planned trips.

## Prompt

The user reported an unexpected error when clicking the delivery planning action and provided backend logs.

## Result

The planning transaction now runs inside the Entity Framework execution strategy returned by `CreateExecutionStrategy()`, which is required for user-initiated transactions with MySQL retry behavior.

## Review

Backend build, backend tests, and frontend build were run successfully after the fix.

## Related Files

- `backend/DroneDelivery.Api/Services/DeliveryPlanningService.cs`
