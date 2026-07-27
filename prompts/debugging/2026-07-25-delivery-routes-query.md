# Delivery Routes Query

## Context

The `/api/deliveries/routes` endpoint failed while reading deliveries through Pomelo/MySQL. The stack trace pointed to `DeliveryService.GetAllAsync`.

## Prompt

User reported a runtime stack trace ending at `DeliveryService.GetAllAsync` and `DeliveriesController.GetRoutes`.

## Result

Changed delivery listing to materialize entities with `Include(delivery => delivery.Drone)` before mapping to DTOs, avoiding custom DTO mapping inside the EF query.

## Review

Verified with `dotnet test backend/DroneDelivery.Tests/DroneDelivery.Tests.csproj` and `dotnet build backend/DroneDelivery.Api/DroneDelivery.Api.csproj`.

## Related Files

- `backend/DroneDelivery.Api/Services/DeliveryService.cs`
