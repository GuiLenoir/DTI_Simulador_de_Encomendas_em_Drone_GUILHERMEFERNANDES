# Order Create JSON Enums

## Context

Creating a new order from the frontend showed `Não foi possível processar a operação`.

## Prompt

User reported that creating a new order failed from the UI.

## Result

Configured ASP.NET Core JSON serialization to accept and return enum values as strings with `JsonStringEnumConverter`, matching the frontend API contract. Also fixed mojibake in frontend order page and API error fallback text.

## Review

Verified with backend build, backend tests, frontend build, and Docker image builds for backend and frontend.

## Related Files

- `backend/DroneDelivery.Api/Program.cs`
- `frontend/src/pages/OrderPage.tsx`
- `frontend/src/services/apiClient.ts`
