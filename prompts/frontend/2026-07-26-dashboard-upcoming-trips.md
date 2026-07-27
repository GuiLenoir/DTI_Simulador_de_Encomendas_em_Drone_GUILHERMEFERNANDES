# Dashboard Upcoming Trips

## Context

The dashboard had separate planned and active trip sections, but planned trips often moved to active too quickly to make the planned section useful.

## Prompt

Improve only the dashboard `Viagens planejadas` section by turning it into `Proximas viagens`, showing trips that have not started and queued orders still awaiting planning, without duplicating active trips.

## Result

Added a read-only `GET /api/trips/upcoming` endpoint and frontend rendering for upcoming trips plus unplanned queued orders. Active trips remain handled only by the existing active trips section.

## Review

Accepted with tests for planned trips, started-trip exclusion, and unplanned orders without compatible drone capacity.

## Related Files

- `backend/DroneDelivery.Api/Services/UpcomingTripService.cs`
- `backend/DroneDelivery.Api/Controllers/TripsController.cs`
- `frontend/src/pages/DashboardPage.tsx`
- `backend/DroneDelivery.Tests/UpcomingTripServiceTests.cs`
