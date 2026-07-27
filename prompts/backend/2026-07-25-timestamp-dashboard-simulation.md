# Timestamp Dashboard Simulation

## Context

The project already had manual delivery allocation and a basic dashboard endpoint. Allocated drones stayed in `Loading` unless a separate simulate endpoint was called.

## Prompt

Implement a timestamp-based drone delivery simulation and show its progress live on the dashboard. Use UTC timestamps as the source of truth, avoid long-running requests, avoid `Thread.Sleep`, keep controllers thin, add migration and tests, and use frontend polling.

## Result

Added delivery timeline fields and simulation options, a clock abstraction, timestamp-derived delivery state service, active-drone allocation checks, dashboard DTOs with calculated state/progress, a migration for timeline fields, and a React dashboard polling every 1000 ms with progress bars.

## Review

Verified with backend restore/build/test, frontend build, Docker Compose config, and Docker image build. The simulation resumes from persisted timestamps after backend restarts.

## Related Files

- `backend/DroneDelivery.Api/Models/Delivery.cs`
- `backend/DroneDelivery.Api/Services/DeliveryStateService.cs`
- `backend/DroneDelivery.Api/Services/DeliveryService.cs`
- `backend/DroneDelivery.Api/Services/DashboardService.cs`
- `backend/DroneDelivery.Api/Migrations/20260725043000_AddDeliveryTimeline.cs`
- `backend/DroneDelivery.Tests/DeliveryStateServiceTests.cs`
- `frontend/src/pages/DashboardPage.tsx`
- `frontend/src/hooks/usePolling.ts`
- `README.md`
- `TODO.md`
