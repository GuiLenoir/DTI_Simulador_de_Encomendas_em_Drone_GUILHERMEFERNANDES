# Drone Battery After Trip

## Context

The project uses timestamp-based trip simulation and queued delivery planning. Drones should consume battery when a trip completes and should only recharge when no queued or pending order can be served with the remaining battery plus the configured safety margin.

## Prompt

After testing, drones were not losing battery after trips. They must lose battery after the trip. If there are orders in the queue and the drone has enough battery with the safety margin, it should take and execute them. It should only recharge when there is no one in the queue or it does not have enough battery to carry any package.

## Result

Adjusted the planning flow to consider a charging drone available as soon as its timestamp-derived current battery can serve a pending queued order. Drone read endpoints now process the queue before returning status, so elapsed trips are consolidated and battery is applied when the drone panel is refreshed.

## Review

Added tests for battery reduction after a completed trip and for interrupting charging when the current battery can serve a queued order.

## Follow-up

The seeded drone charging rate made small battery losses disappear before the next dashboard refresh. Charging now prefers the global simulation configuration and the default was slowed down so the post-trip battery decrease is visible during demos.

## Demo Tuning

Battery consumption was increased to `2.5` percentage points per kilometer and the charging rate was adjusted to `1` percentage point per second. A migration updates the seeded drones so existing local databases use the new demo consumption rate after startup migrations run.

## Individual Delivery Status

The drone status panel now completes elapsed individual deliveries before mapping drone responses. This keeps manually allocated deliveries consistent with planned trips, so the drone battery shown in the dashboard and drone page reflects completed single-order delivery consumption.

## Query Log Noise

Routine EF Core SQL command logs were lowered to `Warning`, dashboard and drone polling remain at one second for the live simulation, and the polling hook now treats non-positive intervals as paused instead of creating a zero-delay interval.

## Timestamp Update Noise

`SaveChangesAsync` no longer updates timestamp fields for unchanged tracked entities. This prevents read-oriented polling requests from generating `UPDATE Drones SET UpdatedAt = ...` and `UPDATE DroneSettings SET UpdatedAtUtc = ...` statements. Delivery planning routine logs were also moved to `Debug`.

## Related Files

- `backend/DroneDelivery.Api/Services/DeliveryPlanningService.cs`
- `backend/DroneDelivery.Api/Services/DroneService.cs`
- `backend/DroneDelivery.Tests/DeliveryPlanningServiceTests.cs`
