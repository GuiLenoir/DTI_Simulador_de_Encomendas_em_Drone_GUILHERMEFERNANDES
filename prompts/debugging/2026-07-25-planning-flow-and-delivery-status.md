# Planning Flow And Delivery Status

## Context

The user clarified that global planning should calculate trips for all pending deliveries, group multiple orders per drone when possible, and then reflect those planned trips in the delivery status area.

## Prompt

The user reported that planning was not working as intended: planning should produce the smallest practical number of trips, planned deliveries should appear in the delivery status section, queued orders should be replanned when possible, charging should update live, and battery safety margin can be global.

## Result

Planning uses the global battery safety margin. The order page now shows individual deliveries and multi-order trips together in a delivery status section. Adding an order to the queue triggers replanning. The drone page polls status and shows live charging or charged battery state. Per-drone margin editing was removed from the frontend flow.

## Review

Backend build, backend tests, and frontend build passed.

## Related Files

- `backend/DroneDelivery.Api/Services/DeliveryPlanningService.cs`
- `backend/DroneDelivery.Api/Services/DeliveryService.cs`
- `backend/DroneDelivery.Api/Services/DroneService.cs`
- `backend/DroneDelivery.Api/Options/DroneDeliveryOptions.cs`
- `frontend/src/pages/OrderPage.tsx`
- `frontend/src/pages/DronePage.tsx`
- `README.md`
