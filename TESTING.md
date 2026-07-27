# Testing Guide

This guide explains how to run the current automated checks for the Drone Delivery Simulator.

## Requirements

- .NET 8 SDK or newer
- Node.js 22 or newer
- npm

Docker and MySQL are not required for the unit tests.

## Backend Tests

Run from the repository root:

```bash
dotnet test backend/DroneDelivery.Tests/DroneDelivery.Tests.csproj
```

The backend test project uses:

- xUnit
- Entity Framework Core InMemory provider
- fixed clocks for timestamp-based behavior

The tests are designed to be fast and deterministic. They do not start Docker, connect to MySQL, or wait for real time to pass.

## Backend Build

Run from the repository root:

```bash
dotnet build backend/DroneDelivery.Api/DroneDelivery.Api.csproj
```

## Frontend Build

Run from the repository root:

```bash
cd frontend
npm run build
```

This runs TypeScript compilation and the Vite production build.

## Frontend Tests

Run from the repository root:

```bash
cd frontend
npm test
```

The frontend test setup uses:

- Vitest;
- React Testing Library;
- jest-dom matchers;
- user-event;
- jsdom.

The current tests cover:

- shared API request behavior;
- order, drone, settings, and report HTTP service calls;
- report filter refresh behavior;
- report map route selection;
- report map route visibility toggle.

## Frontend Coverage

Run from the repository root:

```bash
cd frontend
npm run test:coverage
```

This generates text coverage in the terminal and an HTML report in `frontend/coverage`.

## Backend Coverage

Backend coverage tooling is not configured yet.

The backend tests pass with:

```bash
dotnet test backend/DroneDelivery.Tests/DroneDelivery.Tests.csproj
```

But this command cannot collect coverage until a collector such as `coverlet.collector` is added:

```bash
dotnet test backend/DroneDelivery.Tests/DroneDelivery.Tests.csproj --collect:"XPlat Code Coverage"
```

## Current Important Backend Coverage

The most important DTI challenge rules are covered in these files:

- `backend/DroneDelivery.Tests/DeliveryPlanningServiceTests.cs`
- `backend/DroneDelivery.Tests/DeliveryServiceTests.cs`
- `backend/DroneDelivery.Tests/RoutePlanningServiceTests.cs`
- `backend/DroneDelivery.Tests/ReportServiceTests.cs`
- `backend/DroneDelivery.Tests/DroneServiceTests.cs`
- `backend/DroneDelivery.Tests/OrderServiceTests.cs`
- `backend/DroneDelivery.Tests/DeliveryStateServiceTests.cs`
- `backend/DroneDelivery.Tests/DroneStateServiceTests.cs`
- `backend/DroneDelivery.Tests/CustomerSimulationServiceTests.cs`
- `backend/DroneDelivery.Tests/UpcomingTripServiceTests.cs`

These tests cover:

- delivery planning;
- multi-order trip grouping;
- priority ordering;
- deterministic tie-breakers;
- drone eligibility;
- payload, range, and battery constraints;
- battery safety margin;
- timestamp-based simulation;
- queued order processing after drone return;
- drone CRUD business rules;
- order CRUD business rules;
- no-fly-zone route behavior;
- report summary, filters, efficiency, and map grouping.

## Recommended Full Local Check

Run these commands before submitting or reviewing the project:

```bash
dotnet test backend/DroneDelivery.Tests/DroneDelivery.Tests.csproj
dotnet build backend/DroneDelivery.Api/DroneDelivery.Api.csproj
cd frontend
npm test
npm run test:coverage
npm run build
```

Expected result:

- backend tests pass;
- backend build passes;
- frontend tests pass;
- frontend coverage report is generated;
- frontend TypeScript and Vite build pass.

## Known Pending Test Work

- Add more frontend tests for forms, Dashboard, and simulated customer flows.
- Configure backend coverage collection.
