# Backend Checklist

## Foundation

- [x] Create ASP.NET Core backend
- [x] Add backend test project
- [x] Add Dockerfile
- [x] Add Docker Compose with backend and MySQL
- [x] Add Docker Compose frontend service
- [x] Add Swagger
- [x] Add Entity Framework Core MySQL configuration
- [x] Add committed initial EF Core migration
- [x] Add deterministic drone seed

## Backend

- [x] Create Drone entity
- [x] Create Order entity
- [x] Create Delivery entity
- [x] Create enums
- [x] Create DTOs
- [x] Create DbContext and entity configurations
- [x] Create drone endpoints
- [x] Create order endpoints
- [x] Create delivery endpoints
- [x] Create dashboard endpoint
- [x] Add CRUD for drones
- [x] Add complete drone CRUD UI with filters, details, activation, and settings
- [x] Persist global drone battery safety margin in the database
- [x] Exclude inactive drones from manual allocation and global planning
- [x] Add CRUD for orders
- [x] Add delivery route listing
- [x] Create nearest eligible drone allocation service
- [x] Create delivery allocation endpoint
- [x] Add capacity validation
- [x] Add Euclidean distance calculation
- [x] Add battery validation
- [x] Add range validation
- [x] Add delivery state simulation
- [x] Add timestamp-based delivery timeline simulation
- [x] Add dashboard state calculation from timestamps
- [x] Add reports API with delivery summary, drone efficiency, and delivery map
- [x] Add customer simulation API with order creation and tracking
- [x] Update delivery, order, and drone list statuses from elapsed timelines
- [x] Update drone status battery from elapsed individual delivery timelines
- [x] Add global delivery planning queue
- [x] Add multi-order trip planning
- [x] Add configurable battery safety margin
- [x] Add timestamp-based drone charging simulation
- [x] Only recharge drones after trips when no pending order can be served with current battery
- [x] Interrupt drone charging when queued orders can be served with current battery
- [x] Use global charging rate so post-trip battery loss remains visible in the simulation
- [x] Increase demo battery consumption and set charging to 1 percentage point per second
- [x] Add delivery queue by priority and creation time
- [x] Add global exception middleware
- [x] Add backend tests
- [x] Add critical DTI unit tests for planning, routing, and reports

## Documentation

- [x] Add README
- [x] Add AGENTS.md
- [x] Add prompts directory
- [x] Record backend AI request
- [x] Record frontend AI request
- [x] Document final allocation algorithm
- [x] Document assumptions and limitations
- [x] Document Vercel and Railway demo deployment

## Frontend

- [x] Create React frontend
- [x] Add API integration
- [x] Add order page
- [x] Add drone page
- [x] Add delivery allocation flow
- [x] Add frontend Dockerfile
- [x] Add live dashboard polling
- [x] Suppress routine EF SQL command logs without slowing live polling
- [x] Prevent unchanged tracked entities from generating timestamp-only UPDATE statements
- [x] Add delivery progress visualization
- [x] Add order-name tooltip to allocated delivery order numbers
- [x] Add global queue and planning actions
- [x] Add planned and active trip dashboard sections
- [x] Improve dashboard planned trips into upcoming trips projection
- [x] Add drone battery safety margin controls
- [x] Add deterministic multi-knapsack best-fit global planner
- [x] Compare complete planning candidates before persisting trips
- [x] Add automatic queued delivery processing when drones become available
- [x] Add no-fly-zone backend CRUD and route detours
- [x] Add no-fly-zone frontend management panel
- [x] Show no-fly-zone points on the Cartesian map
- [x] Prevent orders inside active no-fly zones
- [x] Improve no-fly-zone map labels
- [x] Add order details modal with delivery route map
- [x] Add zoom and clearer direction indicators to order route map
- [x] Replace route arrows with time-flow route visualization
- [x] Add reports frontend tab
- [x] Improve reports delivery map explorer
- [x] Add simulated customer frontend tab
- [x] Document planning heuristic and no-fly-zone routing
- [x] Fix drone form Portuguese labels and operational status options
- [x] Add drone delete action to the frontend drone management page
- [x] Mark simulated customer orders as received at the delivery point before drone return
