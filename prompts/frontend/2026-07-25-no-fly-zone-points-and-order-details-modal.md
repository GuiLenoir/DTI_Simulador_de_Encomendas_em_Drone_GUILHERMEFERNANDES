# No-Fly Zone Points And Order Details Modal

## Context

The no-fly-zone page displayed polygons, but users could not inspect the configured points on the Cartesian map. Orders also lacked a detailed route view.

## Prompt

Improve the no-fly-zone map to show current zone points. Add a details button for each order that opens a modal with order details and a Cartesian map showing where the delivery happened or the trip route sequence.

## Result

The no-fly-zone panel now lists coordinates and renders numbered points on the map. The orders table now has a details modal with order metadata and a route map for individual deliveries, multi-order trips, or pending destination-only orders. Trip order API responses now include destination coordinates.

## Review

Kept the API change additive by extending trip order responses with destination coordinates. Existing endpoints and frontend calls remain in place.

## Related Files

- `backend/DroneDelivery.Api/DTOs/DeliveryDtos.cs`
- `backend/DroneDelivery.Api/Services/DeliveryPlanningService.cs`
- `backend/DroneDelivery.Api/Services/DashboardService.cs`
- `frontend/src/pages/NoFlyZonePage.tsx`
- `frontend/src/pages/OrderPage.tsx`
- `frontend/src/types/api.ts`
- `frontend/src/styles.css`
