# Planning Empty With Pending Orders

## Context

The delivery planning endpoint initially planned only orders explicitly marked as queued.

## Prompt

The user reported that clicking "Planejar entregas" created zero trips and zero allocated orders even though several orders had been created.

## Result

The planner now includes pending orders that are still outside the queue, sets them to queued during planning, and then plans them normally. This keeps the explicit queue action but makes the global planning button useful for newly created pending orders.

## Review

Added a backend test covering pending not-queued orders being planned.

## Related Files

- `backend/DroneDelivery.Api/Services/DeliveryPlanningService.cs`
- `backend/DroneDelivery.Tests/DeliveryPlanningServiceTests.cs`
- `README.md`
