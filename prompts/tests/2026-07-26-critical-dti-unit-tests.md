# Critical DTI Unit Tests

## Context

The project already had an ASP.NET Core backend test project using xUnit and an unconfigured frontend test setup. The request was to add only the most important unit tests for the DTI practical challenge, without pursuing full coverage or adding unnecessary tools.

## Prompt

Implement only the most important unit tests for the project, prioritizing DTI challenge business rules: delivery planning, drone selection, payload/range/battery constraints, order priority, timestamp simulation, order/drone rules, no-fly zones, reports, and essential frontend behaviors. Avoid Docker, MySQL, real APIs, sleeps, and broad end-to-end coverage.

## Result

Added focused backend tests for the delivery planner, route planning, and report service. Frontend tests were left pending because the repository does not currently include Vitest, React Testing Library, jsdom, or npm test scripts.

## Review

Accepted focused backend coverage without changing business rules. Coverage tooling was attempted but is not configured in the project.

## Related Files

- `backend/DroneDelivery.Tests/DeliveryPlanningServiceTests.cs`
- `backend/DroneDelivery.Tests/RoutePlanningServiceTests.cs`
- `backend/DroneDelivery.Tests/ReportServiceTests.cs`
- `TODO.md`
