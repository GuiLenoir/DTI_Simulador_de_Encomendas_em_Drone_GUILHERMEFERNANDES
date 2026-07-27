# Reports And Customer Simulation

## Context

The project already had administrative dashboard, orders, drones, no-fly zones, global planning, timestamp-based simulation, and Cartesian route visualizations.

## Prompt

Implement two new frontend and backend areas: `Relatorios` and `Cliente Simulado`. Reports must show completed delivery indicators, drone efficiency, and a delivery map. Customer simulation must create a simplified order, use the normal queue/planning flow, and track only that order with friendly status and route progress.

## Result

Added read-only report endpoints and services, customer simulation endpoints and tracking service, React tabs, report map, customer order form, and customer tracking map using backend-derived route progress.

## Review

Accepted with scoped reuse of existing entities, services, DTO patterns, timestamp simulation, and frontend styles.

## Related Files

- `backend/DroneDelivery.Api/Services/ReportService.cs`
- `backend/DroneDelivery.Api/Services/CustomerSimulationService.cs`
- `backend/DroneDelivery.Api/Controllers/ReportsController.cs`
- `backend/DroneDelivery.Api/Controllers/CustomerSimulationController.cs`
- `frontend/src/pages/ReportsPage.tsx`
- `frontend/src/pages/CustomerSimulationPage.tsx`
