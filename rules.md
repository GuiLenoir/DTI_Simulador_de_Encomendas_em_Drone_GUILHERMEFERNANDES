# Add the project rules here

Replace this placeholder with `AI_RULES_AND_PROJECT_MEMORY.md`.

Codex is instructed by `AGENTS.md` to read this file before making changes.
# AI Rules and Project Memory

> This document defines the rules and project context that should be considered when using AI assistance during the development of the Drone Delivery Simulator.

# Language and Localization Rules

## Source Code

All technical content must be written in English, including:

- Class names
- Method names
- Variable names
- Folder names
- File names
- API routes
- DTOs
- Entities
- Enums
- Database fields
- Code comments
- Test names
- Git commit messages
- Technical documentation

Examples:

```csharp
public class DroneAllocationService
{
    public async Task<Drone?> FindEligibleDroneAsync(...)
}
```

```http
POST /api/orders
GET /api/drones
POST /api/deliveries/allocate
```

Do not use Portuguese identifiers such as:

```csharp
CadastrarPedido()
BuscarDroneDisponivel()
PesoMaximo
```

Use:

```csharp
CreateOrder()
FindAvailableDrone()
MaxWeight
```

---

## User Interface

All text visible to the final user must be written in Brazilian Portuguese (`pt-BR`).

This includes:

- Page titles
- Navigation labels
- Form labels
- Buttons
- Table headers
- Loading messages
- Empty-state messages
- Success messages
- Validation messages
- Error messages displayed by the frontend
- Dashboard labels
- Date and number formatting

Examples:

```tsx
<h1>Simulador de Entregas por Drone</h1>

<button>Criar pedido</button>

<label>Peso do pacote</label>
```

The interface must not display technical enum values directly.

For example, the backend may return:

```json
{
  "status": "InTransit",
  "priority": "High"
}
```

The frontend must display:

```text
Em trânsito
Alta
```

---

## Frontend Translation Mapping

Use explicit translation maps for backend enum values.

Example:

```ts
export const droneStatusLabels = {
  Idle: "Disponível",
  Loading: "Carregando pacote",
  Flying: "Em voo",
  Delivering: "Realizando entrega",
  Returning: "Retornando à base",
  Charging: "Recarregando"
} as const;
```

```ts
export const orderStatusLabels = {
  Pending: "Pendente",
  Allocated: "Drone alocado",
  InTransit: "Em trânsito",
  Delivered: "Entregue",
  Rejected: "Rejeitado"
} as const;
```

```ts
export const priorityLabels = {
  Low: "Baixa",
  Medium: "Média",
  High: "Alta"
} as const;
```

Do not translate the values exchanged with the API.

Only translate them for presentation in the UI.

---

## Formatting

Use the Brazilian Portuguese locale when formatting values.

```ts
new Date(value).toLocaleString("pt-BR");
```

```ts
value.toLocaleString("pt-BR", {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2
});
```

Use the metric system.

- Weight: kg
- Distance: km
- Battery: %
- Time: minutes or hours

---

## API Error Messages

Internal exception names and logs may remain in English.

The API should expose stable English error codes.

The frontend is responsible for translating them into Portuguese.

Example backend response:

```json
{
  "code": "NO_ELIGIBLE_DRONE",
  "title": "No eligible drone",
  "detail": "No available drone can carry this package."
}
```

Example frontend message:

```text
Nenhum drone disponível consegue transportar este pacote.
```

Avoid displaying backend English messages directly to the user.

## 1. Purpose

The project is a **Drone Delivery Simulator** for urban package deliveries.

The system must receive delivery orders, allocate them to available drones, calculate routes, simulate delivery execution, and expose the operation through a web interface and REST API.

This document contains:

- Project rules
- Business rules
- Architecture decisions
- Development conventions
- Project memory for future AI-assisted sessions
- Guidance for the `prompts/` directory

The actual prompts used during development should be stored separately inside the `prompts/` directory.

---

# 2. Project Memory

## 2.1 Technology Stack

The application will use:

- **Frontend:** React
- **Backend:** ASP.NET Core Web API
- **Architecture:** MVC-oriented backend structure
- **ORM:** Entity Framework Core
- **Database:** MySQL
- **Containerization:** Docker and Docker Compose
- **Testing:** xUnit for backend unit tests

## 2.2 Container Structure

The application must be separated into three containers:

1. `frontend`
2. `backend`
3. `database`

Expected communication flow:

```text
React Frontend
      |
      | HTTP / JSON
      v
ASP.NET Core Backend
      |
      | Entity Framework Core
      v
MySQL Database
```

## 2.3 Backend Architecture

The backend follows an MVC-oriented Web API structure.

Recommended folders:

```text
backend/
├── Controllers/
├── Models/
├── DTOs/
├── Services/
├── Data/
├── Middlewares/
├── Migrations/
└── Tests/
```

Responsibilities:

- **Controllers:** Receive HTTP requests and return HTTP responses.
- **Models:** Represent domain and persistence entities.
- **DTOs:** Define API request and response contracts.
- **Services:** Contain business rules and application logic.
- **Data:** Contain `DbContext`, entity configurations, migrations, and database seed logic.
- **Middlewares:** Handle cross-cutting concerns such as exceptions.
- **Tests:** Validate the main business rules.

Controllers should remain thin. Business rules must not be implemented directly inside controllers.

## 2.4 Frontend Structure

Recommended folders:

```text
frontend/
├── src/
│   ├── components/
│   ├── pages/
│   ├── services/
│   ├── hooks/
│   ├── types/
│   └── utils/
```

The frontend is responsible for:

- Creating delivery orders
- Displaying drones and their current status
- Displaying delivery status
- Displaying routes
- Displaying dashboard metrics
- Presenting validation and error messages clearly

---

# 3. Core Functional Rules

## 3.1 Drone Rules

Each drone must have, at minimum:

- Unique identifier
- Name or code
- Maximum package weight
- Maximum travel range
- Current battery level
- Current position
- Current status

A drone cannot receive a package when:

- The package exceeds its maximum weight capacity
- The required route exceeds its available range
- The available battery is insufficient
- The drone is not in an available state

The drone must have enough capacity to complete the required route and return to the base when return-to-base behavior is enabled.

## 3.2 Order Rules

Each delivery order must contain:

- Customer location using two-dimensional coordinates `(X, Y)`
- Package weight
- Delivery priority
- Creation timestamp
- Current order status

Supported priorities:

- Low
- Medium
- High

The system should process higher-priority orders before lower-priority orders.

When two orders have the same priority, the oldest order should be processed first.

## 3.3 Coordinate System

The city is represented as a two-dimensional coordinate grid.

Example:

```text
Base: (0, 0)
Customer A: (10, 5)
Customer B: (4, 8)
```

The first implementation should use Euclidean distance:

```text
distance = sqrt((x2 - x1)^2 + (y2 - y1)^2)
```

Unless otherwise specified, the drone base is located at:

```text
(0, 0)
```

## 3.4 Allocation Rules

The allocation process must:

1. Select only available drones.
2. Reject drones that cannot carry the package.
3. Reject drones without enough range.
4. Reject drones without enough battery.
5. Select the most suitable eligible drone.

The initial allocation strategy should be simple and deterministic.

Recommended initial strategy:

```text
Choose the eligible drone with the shortest total route distance.
```

Possible future improvements:

- Battery-aware scoring
- Load-aware scoring
- Package grouping
- Route optimization
- Nearest-neighbor routing
- Multi-order delivery trips

## 3.5 Delivery Queue Rules

Orders should be sorted using the following criteria:

1. Priority, from highest to lowest
2. Creation time, from oldest to newest
3. Distance, from shortest to longest, when required as a tie-breaker

Example:

```text
High priority order created at 09:00
High priority order created at 09:10
Medium priority order created at 08:30
Low priority order created at 08:00
```

## 3.6 Drone States

The drone simulation should support the following states:

```text
Idle
Loading
Flying
Delivering
Returning
Charging
```

Recommended state flow:

```text
Idle
  -> Loading
  -> Flying
  -> Delivering
  -> Returning
  -> Idle
```

When battery charging is implemented:

```text
Returning
  -> Charging
  -> Idle
```

Invalid state transitions must be rejected.

## 3.7 Battery Rules

Battery consumption should decrease according to traveled distance.

A simple initial formula may be used:

```text
battery consumption = distance * configured consumption rate
```

The exact consumption rate must be configurable.

A drone must not begin a delivery when the estimated battery consumption is greater than its available battery.

When automatic charging is implemented, a drone with low battery should return to the base and enter the `Charging` state.

## 3.8 Route Rules

The route must include:

- Starting point
- Delivery destination
- Optional additional delivery points
- Return to base, when applicable
- Total estimated distance
- Estimated delivery time

The initial version may support one package per route.

Package grouping and multi-stop routes are optional improvements.

## 3.9 Delivery Time

Estimated delivery time may be calculated using:

```text
estimated time = total distance / drone speed
```

Drone speed should be configurable.

Simulation time may be accelerated.

Example:

```text
1 real second = 1 simulated minute
```

The acceleration factor must be documented when used.

---

# 4. API Rules

The backend must expose REST endpoints.

Minimum suggested endpoints:

```http
POST /api/orders
GET  /api/orders
GET  /api/orders/{id}

POST /api/drones
GET  /api/drones
GET  /api/drones/status

POST /api/deliveries/allocate
POST /api/deliveries/simulate
GET  /api/deliveries/routes

GET  /api/dashboard
```

The challenge explicitly suggests endpoints equivalent to:

```http
POST /orders
GET  /deliveries/route
GET  /drones/status
```

## 4.1 API Response Rules

The API must:

- Use JSON
- Return appropriate HTTP status codes
- Return clear validation messages
- Use consistent response formats
- Avoid exposing internal exception details

Recommended status codes:

- `200 OK`
- `201 Created`
- `204 No Content`
- `400 Bad Request`
- `404 Not Found`
- `409 Conflict`
- `422 Unprocessable Entity`
- `500 Internal Server Error`

## 4.2 Validation Error Format

Prefer ASP.NET Core `ProblemDetails`.

Example:

```json
{
  "type": "validation_error",
  "title": "Invalid delivery order",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "errors": {
    "packageWeight": [
      "Package weight must be greater than zero."
    ]
  }
}
```

---

# 5. Data and Entity Framework Rules

## 5.1 Persistence

MySQL is the persistent database.

Entity Framework Core must be used for:

- Entity mapping
- Database queries
- Migrations
- Relationships
- Persistence

## 5.2 Date and Time

All timestamps should be stored in UTC.

Examples:

- `CreatedAt`
- `UpdatedAt`
- `DeliveredAt`
- Drone state transition timestamps

## 5.3 Numeric Precision

Package weight, capacity, and other decimal values must use explicit database precision.

Example:

```csharp
builder.Property(x => x.PackageWeight)
    .HasPrecision(10, 2);
```

## 5.4 Database Initialization

The project should provide migrations.

For local development, the backend may automatically apply pending migrations during startup.

The database container must use a persistent Docker volume.

## 5.5 Seed Data

The application should provide initial drone records so that the evaluator can test the system immediately.

Seed data must be deterministic and documented.

---

# 6. Error Handling Rules

The system must reject invalid operations with clear messages.

Examples:

- Package exceeds all drone capacities
- No drone is currently available
- Drone does not have enough battery
- Drone does not have enough range
- Invalid coordinates
- Invalid priority
- Order does not exist
- Drone does not exist
- Invalid state transition

A global exception-handling middleware should translate known application exceptions into appropriate HTTP responses.

Unexpected errors should be logged and returned as generic server errors.

---

# 7. Testing Rules

Unit tests are mandatory.

Tests should focus on business rules rather than trivial controller behavior.

Minimum recommended test cases:

1. Reject a package with invalid weight.
2. Reject a package heavier than every drone capacity.
3. Ignore unavailable drones.
4. Ignore drones without enough range.
5. Ignore drones without enough battery.
6. Select the nearest eligible drone.
7. Prioritize high-priority orders.
8. Use creation time as a priority tie-breaker.
9. Calculate Euclidean distance correctly.
10. Decrease battery according to traveled distance.
11. Reject invalid drone state transitions.
12. Return the drone to `Idle` after completing the route.
13. Return a clear error when allocation is impossible.

Tests should be deterministic.

Time-dependent logic should use an abstraction such as a clock service when necessary.

---

# 8. Dashboard Rules

The dashboard should display at least:

- Number of completed deliveries
- Number of pending deliveries
- Average delivery time
- Most efficient drone
- Current drone statuses

Optional dashboard information:

- Total distance traveled
- Battery consumption
- Failed deliveries
- Delivery count by priority
- Delivery route map
- Drone utilization percentage

The delivery map may be implemented using:

- SVG
- Canvas
- CSS Grid
- A chart library
- ASCII representation

An external map service is not required because the domain uses an abstract coordinate grid.

---

# 9. Docker Rules

The repository must provide a `docker-compose.yml` file.

Expected services:

```text
frontend
backend
database
```

The database service must include:

- MySQL image
- Environment variables
- Persistent volume
- Health check

The backend should wait for the database health check before starting database-dependent operations.

The frontend should communicate with the backend using a configurable API URL.

Sensitive values should not be committed directly.

Provide:

```text
.env.example
```

Do not commit:

```text
.env
```

Expected startup command:

```bash
docker compose up --build
```

The README must document:

- Required software
- Environment variables
- Startup command
- Frontend URL
- Backend URL
- Swagger URL
- Test command
- Migration command, when applicable

---

# 10. Scope Rules

The implementation should prioritize a complete working flow over unnecessary architectural complexity.

Prioritize:

- Working order creation
- Drone allocation
- Route calculation
- Delivery simulation
- Validations
- Tests
- Dashboard
- Docker execution
- Documentation

Avoid introducing unnecessary complexity unless the core solution is already complete.

Do not prioritize:

- Microservices
- Message brokers
- Event sourcing
- Full CQRS
- Complex generic repository abstractions
- External authentication
- External map providers
- Perfect route optimization

A simple, clear, and tested algorithm is preferred over an incomplete advanced algorithm.

---

# 11. Coding Conventions

## 11.1 General

- Use English for source code, class names, method names, variables, and API routes.
- Prefer clear names over abbreviations.
- Keep methods small and focused.
- Avoid duplicated business logic.
- Use asynchronous database operations.
- Pass `CancellationToken` to asynchronous endpoints and services.
- Enable nullable reference types.
- Use dependency injection.
- Keep configuration outside business logic.

## 11.2 Backend

- Use `async` and `await` for I/O operations.
- Use DTOs instead of exposing persistence entities directly.
- Keep controllers thin.
- Keep business logic in services.
- Prefer specific services over a generic repository.
- Use explicit entity configurations.
- Use UTC timestamps.
- Validate input before persistence.

## 11.3 Frontend

- Use TypeScript.
- Keep API calls inside service modules.
- Reuse UI components.
- Display loading, success, empty, and error states.
- Keep API types explicit.
- Avoid duplicating backend business rules in the frontend.
- Treat the backend as the source of truth.

---

# 12. Security and Configuration Rules

- Do not commit passwords or secrets.
- Use environment variables for database credentials.
- Configure CORS only for expected frontend origins.
- Validate all external input.
- Avoid returning stack traces to clients.
- Use parameterized queries through Entity Framework Core.
- Do not trust values calculated only by the frontend.

Authentication is outside the initial scope unless added after the mandatory requirements are complete.

---

# 13. Documentation Rules

The repository README must include:

1. Project overview
2. Architecture overview
3. Technology stack
4. How to run with Docker
5. How to run tests
6. API documentation
7. Business rules
8. Allocation algorithm explanation
9. Assumptions and limitations
10. Future improvements

Important technical decisions should be explained briefly.

Example decisions:

- Euclidean distance was selected because the city is represented as a coordinate grid.
- A greedy allocation algorithm was selected due to the project scope.
- Controllers were kept thin to separate HTTP concerns from business rules.
- Entity Framework Core is used directly by application services to avoid unnecessary repository abstraction.

---

# 14. AI Usage Rules

AI may be used to assist with:

- Code generation
- Refactoring
- Test-case suggestions
- Documentation
- Error analysis
- Docker configuration
- API design
- UI component scaffolding
- Algorithm discussion

AI-generated content must always be reviewed before being committed.

The developer remains responsible for:

- Verifying correctness
- Running tests
- Reviewing security
- Understanding generated code
- Ensuring consistency with project rules
- Removing unused or unnecessary code

AI should not introduce technologies or architectural patterns that conflict with this document unless the decision is explicitly reviewed and recorded.

When asking AI for code, requests should include:

- Current architecture
- Relevant entities
- Expected behavior
- Validation rules
- Existing coding conventions
- Expected test cases

---

# 15. Project Assumptions

The following assumptions are used until explicitly changed:

- The drone base is located at `(0, 0)`.
- Distance is calculated using Euclidean distance.
- Drones return to base after completing a route.
- Orders are initially allocated one at a time.
- High priority is processed before medium and low priority.
- Creation time is used as the first tie-breaker.
- The nearest eligible drone is selected.
- Battery consumption is proportional to distance.
- Drone speed is constant during the initial simulation.
- Obstacles and no-fly zones are optional.
- Multi-package routes are optional.
- Authentication is not part of the initial version.
- The backend is the source of truth for allocation and simulation.

Any changed assumption must be updated in this document.

---

# 16. Open Decisions

The following decisions should be defined during implementation:

- Exact drone battery consumption rate
- Exact drone speed
- Maximum coordinate boundaries
- Whether route range always includes the return trip
- Whether allocation occurs automatically after order creation
- Whether simulation is manual or background-based
- Whether package grouping will be implemented
- Whether obstacles will be implemented
- Dashboard refresh interval
- Exact definition of the most efficient drone

Until decided, AI-generated code must not silently invent permanent values for these items.

Use configuration or clearly marked placeholders where possible.

---

# 17. Prompt Records

Prompts used during development must be stored in:

```text
prompts/
```

Suggested structure:

```text
prompts/
├── backend/
├── frontend/
├── tests/
├── docker/
├── documentation/
└── debugging/
```

Suggested file naming convention:

```text
YYYY-MM-DD-short-description.md
```

Example:

```text
prompts/backend/2026-01-15-drone-allocation-service.md
```

Each prompt file may contain:

```markdown
# Prompt Title

## Context

Brief project context supplied to the AI.

## Prompt

The exact prompt used.

## Result

Summary of the generated result.

## Review

What was accepted, changed, or rejected.

## Related Files

- `backend/Services/DroneAllocationService.cs`
- `backend/Tests/DroneAllocationServiceTests.cs`
```

Do not place full prompt history inside this file. This file should only define the rules and persistent project memory.

---

# 18. Definition of Done

A feature is considered complete when:

- The behavior is implemented.
- Business rules are respected.
- Input validation is included.
- Errors are handled clearly.
- Relevant unit tests pass.
- The API contract is documented.
- The feature works through Docker when applicable.
- No secrets are committed.
- The code follows the conventions in this document.
- AI-generated code has been reviewed and understood.

---

# 19. Initial Delivery Goal

The first complete version should support:

1. Starting all services with Docker Compose.
2. Creating delivery orders through the React interface.
3. Persisting orders in MySQL.
4. Listing available drones.
5. Allocating an eligible drone.
6. Calculating route distance.
7. Simulating delivery state transitions.
8. Updating drone battery.
9. Displaying delivery and drone status.
10. Displaying basic dashboard metrics.
11. Handling invalid orders clearly.
12. Running automated backend tests.

Advanced features should only be added after this complete flow is stable.
