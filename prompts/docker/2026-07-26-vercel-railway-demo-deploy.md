# Vercel And Railway Demo Deploy

## Context

The project already runs locally with Docker Compose using frontend, backend, and MySQL containers. The request was to prepare an online demo deployment without changing business rules or existing functionality.

## Prompt

Configure deployment for React/Vite on Vercel, ASP.NET Core 8 on Railway using Docker, and MySQL hosted on Railway. Keep local Docker Compose working, use environment variables, avoid production localhost references, run migrations automatically, and document the process.

## Result

Configured production CORS through environment variables, made the backend Dockerfile Railway-port compatible, removed the frontend API client's localhost fallback, added Vercel configuration and deployment env examples, and documented Railway/Vercel deployment steps.

## Review

Accepted deploy-only changes. Business logic was not modified.

## Related Files

- `backend/DroneDelivery.Api/Program.cs`
- `backend/DroneDelivery.Api/Dockerfile`
- `backend/DroneDelivery.Api/appsettings.json`
- `backend/DroneDelivery.Api/appsettings.Development.json`
- `backend/DroneDelivery.Api/.env.railway.example`
- `frontend/src/services/apiClient.ts`
- `frontend/.env.production.example`
- `vercel.json`
- `README.md`
- `TODO.md`
