# Whole-Plan Planning Heuristic

## Context

The delivery planner selected the best next trip greedily and persisted each accepted candidate immediately. This could miss a better complete plan when multiple valid solutions used the same number of trips.

## Prompt

Refactor the planner so it compares full candidate plans before persisting trips. The objective is still to minimize trips, then compare priority served in the first trip, total distance, capacity usage, smallest capable drones, and deterministic tie-breakers.

## Result

The planner now generates valid trip candidates by drone and order subset, recursively combines candidates into complete plans, compares plans with deterministic criteria, and persists only the selected plan.

## Review

Added a regression test where two valid two-trip plans exist and the selected plan is the one with lower total route distance while preserving the same trip count.

## Related Files

- `backend/DroneDelivery.Api/Services/DeliveryPlanningService.cs`
- `backend/DroneDelivery.Tests/DeliveryPlanningServiceTests.cs`
- `README.md`
- `TODO.md`
