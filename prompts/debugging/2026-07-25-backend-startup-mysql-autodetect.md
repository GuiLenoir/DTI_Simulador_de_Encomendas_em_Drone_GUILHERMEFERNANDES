# Backend Startup MySQL Autodetect

## Context

The backend container crashed on startup with `Unable to connect to any of the specified MySQL hosts` while `ServerVersion.AutoDetect` was configuring the DbContext.

## Prompt

User reported that the backend did not start and attached the container logs.

## Result

Replaced `ServerVersion.AutoDetect(connectionString)` with a fixed MySQL 8.4 server version and enabled Pomelo retry-on-failure. Added retry logic around startup migrations so transient MySQL readiness gaps do not crash the backend immediately.

## Review

Verified with `dotnet build`, `dotnet test`, and `docker compose build backend`.

## Related Files

- `backend/DroneDelivery.Api/Program.cs`
