# Migration Seed Column Types

## Context

The backend failed during startup migration with `There is no entity type mapped to the table 'Drones' which is used in a data operation`.

## Prompt

User attached Docker backend logs showing repeated failures while applying `20260724233000_InitialCreate`.

## Result

Updated the manual `InsertData` operation in the initial migration to include explicit `columnTypes`, so EF Core/Pomelo can generate seed SQL without relying on model inference.

## Review

Added a regression test that invokes the migration `Up` method and verifies the seed operation has explicit column types. Verified with `dotnet build`, `dotnet test`, and `docker compose build backend`.

## Related Files

- `backend/DroneDelivery.Api/Migrations/20260724233000_InitialCreate.cs`
- `backend/DroneDelivery.Tests/InitialCreateMigrationTests.cs`
