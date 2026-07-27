# Complete Drone CRUD

## Context

The project already had a drone status list, manual allocation, global planning, timestamp-based delivery simulation, and no-fly-zone routing.

## Prompt

Implement a complete drone CRUD reusing the existing architecture, components, services, entities, and data structure. Add listing, creation, editing, activation/deactivation, details/history, and global battery safety margin settings. Preserve manual allocation, existing frontend endpoints, and timestamp-based simulation.

## Result

Implemented persisted global drone settings, expanded drone operational fields, logical activation/deactivation rules, planning integration for inactive drones, frontend CRUD UI, and validation tests.

## Review

Accepted with scoped changes to drone management, settings, planning filters, documentation, and tests.

## Related Files

- `backend/DroneDelivery.Api/Services/DroneService.cs`
- `backend/DroneDelivery.Api/Services/DroneSettingsService.cs`
- `backend/DroneDelivery.Api/Controllers/DronesController.cs`
- `backend/DroneDelivery.Api/Controllers/DroneSettingsController.cs`
- `frontend/src/pages/DronePage.tsx`
