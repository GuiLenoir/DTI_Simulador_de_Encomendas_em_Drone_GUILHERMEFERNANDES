# No-Fly Zone Order Validation And Map Labels

## Context

The no-fly-zone Cartesian map displayed large coordinate labels that could overlap. Orders could still be created with destinations inside active no-fly zones.

## Prompt

Improve no-fly-zone map labels and add a backend rule that rejects new orders inside active no-fly zones, with a frontend alert message.

## Result

Added an active-zone point containment check to the route planning service, validated order create/update destinations, translated the new error code in the order page, and simplified map labels to small numbered markers with coordinate details kept in the zone list and SVG tooltip.

## Review

The backend treats points inside or on the boundary of an active no-fly zone as invalid delivery destinations.

## Related Files

- `backend/DroneDelivery.Api/Services/IRoutePlanningService.cs`
- `backend/DroneDelivery.Api/Services/RoutePlanningService.cs`
- `backend/DroneDelivery.Api/Services/OrderService.cs`
- `backend/DroneDelivery.Tests/OrderServiceTests.cs`
- `frontend/src/pages/OrderPage.tsx`
- `frontend/src/pages/NoFlyZonePage.tsx`
- `frontend/src/styles.css`
- `TODO.md`
