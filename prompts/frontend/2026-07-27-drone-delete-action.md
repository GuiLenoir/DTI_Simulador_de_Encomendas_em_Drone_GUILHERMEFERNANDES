# Drone Delete Action

## Context

The backend already exposes `DELETE /api/drones/{id}` as a logical deletion/deactivation flow, but the frontend drone page did not expose this action.

## Prompt

Adicione tb opção pra excluir drone

## Result

Added a frontend API function for deleting drones, a row action with confirmation on the drone management page, friendly messages, and a service test for the DELETE endpoint.

## Review

Accepted as a frontend integration over the existing backend endpoint. The action is disabled while the drone is executing a trip.

## Related Files

- `frontend/src/services/dronesApi.ts`
- `frontend/src/pages/DronePage.tsx`
- `frontend/src/services/httpServices.test.ts`
