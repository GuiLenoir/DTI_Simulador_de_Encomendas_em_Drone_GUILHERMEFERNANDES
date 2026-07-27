# Hard Delete Drone

## Context

The frontend exposed both deactivate and delete actions, but the backend `DELETE /api/drones/{id}` still delegated to the deactivate flow.

## Prompt

pra excluir de verdade, pra desativar ja tem opção

## Result

Changed the drone delete service to remove the drone row from the database, while still blocking executing drones and canceling planned trips before deletion.

## Review

Accepted as a backend behavior correction for the existing DELETE endpoint. The frontend confirmation text now makes the permanent delete clearer.

## Related Files

- `backend/DroneDelivery.Api/Services/DroneService.cs`
- `backend/DroneDelivery.Tests/DroneServiceTests.cs`
- `frontend/src/pages/DronePage.tsx`
