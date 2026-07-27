# Missing Deliveries Table

## Context

The backend logged `MySqlConnector.MySqlException: Table 'drone_delivery.Deliveries' doesn't exist` when calling `/api/deliveries/routes`.

## Prompt

User attached the full stack trace for the missing `Deliveries` table error.

## Result

Added the missing EF Core migration metadata attributes to `InitialCreate` so `Database.MigrateAsync()` can discover and apply the migration at startup.

## Review

Added a regression test that verifies the migration has both `MigrationAttribute` and `DbContextAttribute`. Verified with `dotnet build` and `dotnet test`.

## Related Files

- `backend/DroneDelivery.Api/Migrations/20260724233000_InitialCreate.cs`
- `backend/DroneDelivery.Tests/MigrationTests.cs`
