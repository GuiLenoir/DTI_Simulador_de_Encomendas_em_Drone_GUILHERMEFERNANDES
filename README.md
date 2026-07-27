# Drone Delivery Simulator

Full-stack implementation for the urban drone delivery technical challenge.

## Included

- React + TypeScript frontend
- ASP.NET Core Web API
- Entity Framework Core with MySQL
- Docker Compose with frontend, backend, and database services
- Swagger
- Drone, order, and delivery entities
- DbContext, explicit EF configuration, migrations, and deterministic drone seed
- CRUD endpoints for drones and orders
- Delivery allocation, route listing, timestamp-based simulation, and dashboard endpoints
- Global planning queue, multi-order trips, and timestamp-based drone charging
- Configurable no-fly zones with automatic route detours
- Order, drone, allocation, and live dashboard UI
- xUnit backend tests for core business rules

## Repository Structure

```text
.
├── AGENTS.md
├── rules.md
├── TODO.md
├── prompts/
├── backend/
│   ├── DroneDelivery.Api/
│   └── DroneDelivery.Tests/
├── frontend/
└── docker-compose.yml
```

## Run With Docker

```bash
cp .env.example .env
docker compose up --build
```

Open:

- Frontend: `http://localhost:3000`
- Backend: `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger`

## Run Locally

Requirements:

- .NET 8 SDK or newer
- Node.js 22 or newer
- Docker, for the local MySQL database

```bash
docker compose up database -d
dotnet restore backend/DroneDelivery.Api/DroneDelivery.Api.csproj
dotnet run --project backend/DroneDelivery.Api/DroneDelivery.Api.csproj
```

In another terminal:

```bash
cd frontend
npm install
npm run dev
```

## Run Tests

```bash
dotnet test backend/DroneDelivery.Tests/DroneDelivery.Tests.csproj
```

```bash
cd frontend
npm test
```

See `TESTING.md` for the complete testing guide, including frontend coverage and current backend coverage limitations.

## Build Frontend

```bash
cd frontend
npm run build
```

## Demo Deployment

The application is prepared for an online demo with:

- Frontend: Vercel
- Backend: Railway using the backend Dockerfile
- Database: Railway MySQL

Local Docker Compose continues to use the local MySQL container and is not required for production.

### Railway MySQL

1. Create a new Railway project.
2. Add a MySQL service.
3. Keep the generated database variables private.
4. Use the Railway variables to build the backend connection string.

Required backend variable:

```text
ConnectionStrings__DefaultConnection=Server=${MYSQLHOST};Port=${MYSQLPORT};Database=${MYSQLDATABASE};User=${MYSQLUSER};Password=${MYSQLPASSWORD};SslMode=Preferred;
```

### Railway Backend

Create a Railway service from this repository and deploy it with Docker.

Use:

```text
Dockerfile path: backend/DroneDelivery.Api/Dockerfile
```

Required Railway variables:

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Server=${MYSQLHOST};Port=${MYSQLPORT};Database=${MYSQLDATABASE};User=${MYSQLUSER};Password=${MYSQLPASSWORD};SslMode=Preferred;
Cors__AllowedOrigins__0=https://your-vercel-app.vercel.app
```

Optional Railway variables:

```text
DroneDelivery__BatteryConsumptionPerKm=1.5
DroneDelivery__BatterySafetyMarginPercentagePoints=5
DroneDelivery__DroneSpeedKmPerHour=60
Simulation__LoadingDurationSeconds=3
Simulation__DeliveryDurationSeconds=3
Simulation__SecondsPerKilometer=2
Simulation__ChargingPercentagePointsPerSecond=2
```

The backend listens on Railway's `PORT` variable when it exists, and falls back to port `8080` locally. Entity Framework applies pending migrations automatically during startup with `Database.Migrate()`.

After deploy, open:

```text
https://your-api.up.railway.app/swagger
```

### Vercel Frontend

Create a Vercel project from this repository.

Use:

```text
Framework Preset: Vite
Build Command: cd frontend && npm install && npm run build
Output Directory: frontend/dist
```

Required Vercel variable:

```text
VITE_API_URL=https://your-api.up.railway.app
```

The frontend API client reads `import.meta.env.VITE_API_URL`. Do not point the Vercel build to the local Docker backend.

### Demo Smoke Test

After both services are deployed:

1. Open the Vercel URL.
2. Create a new order.
3. Open the drones page and confirm seeded drones are visible.
4. Use the delivery planning action.
5. Confirm trips/orders update in the dashboard.
6. Open `https://your-api.up.railway.app/swagger` and confirm the API responds.

If the browser blocks requests, verify that `Cors__AllowedOrigins__0` exactly matches the Vercel domain, including `https://`.

## Migrations

The backend includes committed migrations and applies pending migrations at startup.

```bash
dotnet ef migrations add MigrationName --project backend/DroneDelivery.Api/DroneDelivery.Api.csproj
```

## API Endpoints

```http
GET    /api/drones
GET    /api/drones/{id}
GET    /api/drones/status
POST   /api/drones
PUT    /api/drones/{id}
PATCH  /api/drones/{id}/activate
PATCH  /api/drones/{id}/deactivate
DELETE /api/drones/{id}

GET    /api/drone-settings
PUT    /api/drone-settings

GET    /api/orders
GET    /api/orders/{id}
GET    /api/orders/queue
POST   /api/orders
POST   /api/orders/{id}/queue
DELETE /api/orders/{id}/queue
PUT    /api/orders/{id}
DELETE /api/orders/{id}

GET    /api/deliveries
GET    /api/deliveries/{id}
GET    /api/deliveries/routes
POST   /api/deliveries/allocate/{orderId}
POST   /api/deliveries/simulate/{deliveryId}
DELETE /api/deliveries/{id}

GET    /api/dashboard

GET    /api/reports?from=&to=&droneId=&priority=

POST   /api/customer-simulation/orders
GET    /api/customer-simulation/orders/{id}/tracking

POST   /api/delivery-planning/plan
GET    /api/delivery-planning
GET    /api/trips
GET    /api/trips/upcoming
GET    /api/trips/{id}

GET    /api/no-fly-zones
GET    /api/no-fly-zones/{id}
POST   /api/no-fly-zones
PUT    /api/no-fly-zones/{id}
DELETE /api/no-fly-zones/{id}
```

## Allocation Algorithm

1. Loads a pending order.
2. Completes any elapsed delivery timelines.
3. Filters drones with no active delivery according to persisted timestamps.
4. Filters drones capable of carrying the package.
5. Filters drones with enough range for the route and return to base.
6. Filters drones with enough battery for configured consumption per km.
7. Selects the eligible drone with the shortest route distance.
8. Creates a delivery record with the complete UTC simulation timeline.

Individual allocation still creates one delivery for one order through `POST /api/deliveries/allocate/{orderId}`.

## Global Planning

Orders can be added to the global queue with `POST /api/orders/{id}/queue`. The `POST /api/delivery-planning/plan` endpoint replans queued orders, also auto-queues pending orders that are still outside the queue, and recalculates mutable planned trips. Trips that already started loading are immutable and are not recalculated.

Planning uses a deterministic multi-knapsack Best Fit heuristic that compares whole candidate plans before saving trips:

1. Sort orders by priority descending, queue time ascending, package weight descending, and order ID ascending.
2. Consider only pending orders or orders in trips that have not started loading yet.
3. Release and recalculate only planned trips whose loading timestamp is still in the future.
4. Consider currently available drones, excluding active deliveries, active trips, and charging drones.
5. Generate valid trip candidates by drone and order subset while payload, range, route availability, and battery rules remain valid.
6. Recursively combine candidates into complete plans without reusing drones or orders.
7. Prefer the plan that allocates the most orders, then uses the fewest trips.
8. For plans with the same trip count, compare the complete plan by: more highest-priority orders in the first trip, lower total distance, higher summed capacity usage, smaller capable drones, and stable drone/order IDs.

The planner seeks to reduce the number of trips but does not guarantee a globally optimal mathematical solution.

For multi-order routes, delivery sequence preserves priority first. Within the same priority, the route uses a nearest-neighbor heuristic with queue time and order ID as deterministic tie-breakers. Route distance includes return to base when `RequireReturnToBase` is enabled.

The dashboard uses `GET /api/trips/upcoming` for its `Proximas viagens` section. This read-only projection shows trips that have not reached `LoadingStartedAtUtc` yet and separates queued orders that still do not have a planned trip. Active trips are intentionally excluded so the same trip never appears in both `Proximas viagens` and `Viagens ativas`.

## Route Calculation And No-Fly Zones

The city is still represented as a 2D coordinate grid. When there are no active obstacles, route segments use Euclidean distance. When an active no-fly-zone polygon blocks a segment, the backend builds a visibility graph using the segment endpoints and the polygon vertices, then runs a deterministic shortest-path search to find the shortest valid detour.

The resulting route distance is the source of truth for:

- drone range validation;
- battery consumption;
- simulated flight duration;
- manual allocation and global planning.

No-fly zones are managed through `GET/POST/PUT/DELETE /api/no-fly-zones`. Each zone has a name, an active flag, and at least three 2D points. Inactive zones are ignored by route calculation. If a route point is inside an active zone, the API returns `ROUTE_BLOCKED_BY_NO_FLY_ZONE`. If no detour exists, the API returns `NO_VALID_ROUTE_AVAILABLE`.

The detour heuristic uses polygon vertices directly and does not model street-level constraints, altitude, weather, traffic, or curved paths. It is deterministic and practical for the challenge grid, but it is not a full computational geometry optimizer.

## Battery Safety And Charging

Battery consumption uses percentage points:

```text
Estimated route consumption: 40 percentage points
Configured safety margin: 5 percentage points
Minimum battery required: 45%
```

The safety margin is added directly as percentage points, not as a relative multiplier. A drone can start a trip only when its current battery is greater than or equal to estimated consumption plus the global safety margin stored in `DroneSettings`.

The battery safety margin is global and managed through `GET/PUT /api/drone-settings`, with a deterministic default of `5` percentage points. The backend stores charging timestamps and derives current battery without background loops:

```text
BatteryAtChargingStartPercentage + elapsed seconds * ChargingRatePercentagePointsPerSecond
```

After a delivery or trip completes, battery consumption is applied once. Before starting a recharge, the backend checks whether the drone can still serve at least one pending or queued order with its current battery, capacity, range, safe route, consumption rate, and global safety margin. If it can, the drone returns to `Idle` immediately and remains available. If no pending order can be served with the remaining battery, it enters `Charging` until `ChargingCompletedAtUtc`; after that timestamp, its calculated status is `Idle`.

## Drone Management

Drones support complete CRUD through the API and the React screen. Each drone has code, name, maximum capacity, maximum range, battery percentage, average speed, battery consumption per kilometer, current 2D position, operational status, notes, and an active flag.

Drone codes are unique. Capacity, range, speed, and consumption must be positive, and battery percentage must stay between 0 and 100. Drones with history are deactivated logically instead of being physically removed. Inactive drones remain visible for history but are ignored by manual allocation and global planning.

Operational changes are blocked while a drone is executing a delivery or trip. If a drone only has future planned trips, operational changes or deactivation cancel those planned trips, return their orders to the queue, and trigger queue processing again. Trips in `Loading` or later states are never modified.

## Timestamp-Based Simulation

When an order is allocated, the backend persists a full UTC timeline for the delivery:

1. `CreatedAtUtc` and `LoadingStartedAtUtc`
2. `FlyingStartedAtUtc`
3. `DeliveringStartedAtUtc`
4. `ReturningStartedAtUtc`
5. `CompletedAtUtc`

The current drone, order, and delivery state is calculated from these timestamps and `DateTime.UtcNow`. The simulation does not depend on a background worker, long-running HTTP request, `Thread.Sleep`, or chained `Task.Delay` calls. If the backend restarts, the timeline in MySQL remains the source of truth.

Default simulation timing:

- Loading: `3` seconds
- Delivery handoff: `3` seconds
- Flight duration: `2` seconds per kilometer
- Return flight uses the same seconds-per-kilometer rule

These values can be configured with:

```text
SIMULATION_LOADING_DURATION_SECONDS
SIMULATION_DELIVERY_DURATION_SECONDS
SIMULATION_SECONDS_PER_KILOMETER
SIMULATION_CHARGING_PERCENTAGE_POINTS_PER_SECOND
```

The frontend dashboard polls `GET /api/dashboard` every 1000 ms, avoids overlapping requests, and keeps the last successful data visible if a temporary request fails.

## Reports

The `Relatorios` tab is a read-only administrative view based only on completed deliveries and completed trips. The report API consolidates delivery count, average delivery time, drone efficiency, and map points through `GET /api/reports`.

Average delivery time is calculated from the operation start timestamp to each order completion timestamp. Records without enough timestamps are ignored. Drone efficiency is deterministic and uses:

```text
EfficiencyScore = (CompletedDeliveries + TotalTransportedWeight) / (TotalDistance + TotalBatteryConsumed)
```

The denominator is guarded to avoid division by zero. The response also returns the completed deliveries, transported weight, traveled distance, consumed battery, and final score used by the UI.

## Customer Simulation

The `Cliente Simulado` tab lets a customer create a simplified order and follow only that order. `POST /api/customer-simulation/orders` creates the same persisted order used by the administrative flow, adds it to the queue, and triggers queue processing. `GET /api/customer-simulation/orders/{id}/tracking` returns friendly status text, the assigned drone when available, route points, progress, remaining distance, and current drone position.

The tracking position is derived from persisted timestamps and route points. The frontend only renders the interpolated position returned by the backend, so it does not create an independent delivery simulation. For customer-facing proximity messages, the UI assumes `1` coordinate unit equals `1` city block when the package is nearby.

## Assumptions

- The drone base is `(0, 0)`.
- Euclidean distance is used only when no active no-fly zone blocks the route.
- Drones return to base after simulation.
- Battery consumption defaults to `1.5` percent per km.
- Drone speed defaults to `60` km/h.
- The frontend calls the backend through `VITE_API_URL`, which defaults to `http://localhost:8080`.
- Timestamps are stored in UTC and displayed in the frontend using the Brasília time zone (`America/Sao_Paulo`).
- Dashboard delivery progress is calculated by the backend from persisted timeline timestamps.
- Global planning uses a deterministic heuristic to reduce trips, not a mathematically optimal optimizer.
