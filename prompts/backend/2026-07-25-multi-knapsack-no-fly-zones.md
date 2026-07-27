# Multi-Knapsack Planner And No-Fly Zones

## Context

The project already had manual per-order allocation, global queued planning, timestamp-based simulation, Docker, EF Core, MySQL, Swagger, React pages, and backend tests.

## Prompt

Adjust the global planner to match the DTI challenge while preserving the current architecture, manual allocation, frontend endpoints, and timestamp simulation. Implement deterministic multi-knapsack Best Fit planning, respect priority and queue time, validate capacity, range, battery margin in percentage points, and never modify started trips. Add configurable polygon no-fly zones with CRUD, frontend management panel in Portuguese, route detours using polygon vertices, route-blocking errors, tests, README, TODO, and run backend/frontend validation.

## Result

Implemented no-fly-zone entities, EF configuration, migration, CRUD service/controller, route planning service with visibility graph detours, planner integration, manual allocation route integration, frontend no-fly-zone page, translated route errors, tests, and documentation updates.

## Review

Accepted a deterministic heuristic rather than an exact optimizer. The route planner uses polygon vertices as valid detour points and rejects route points inside active zones.

## Related Files

- `backend/DroneDelivery.Api/Services/DeliveryPlanningService.cs`
- `backend/DroneDelivery.Api/Services/RoutePlanningService.cs`
- `backend/DroneDelivery.Api/Controllers/NoFlyZonesController.cs`
- `frontend/src/pages/NoFlyZonePage.tsx`
- `README.md`
- `TODO.md`
