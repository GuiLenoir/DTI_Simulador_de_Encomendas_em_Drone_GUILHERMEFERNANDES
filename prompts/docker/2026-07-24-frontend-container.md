# Frontend Container

## Context

The project had a React frontend and backend/database Docker services, but the frontend service was missing from Docker Compose.

## Prompt

faltou o container do frontend nao?

## Result

Added a production frontend Dockerfile using Node for build and Nginx for runtime, added the frontend service to Docker Compose, exposed it on port `3000`, and updated `.dockerignore`, `.env.example`, README, and TODO.

## Review

The frontend container builds with `VITE_API_URL` as a build argument so browser requests can target the backend at `http://localhost:8080`.

## Related Files

- `frontend/Dockerfile`
- `frontend/nginx.conf`
- `docker-compose.yml`
- `.dockerignore`
- `.env.example`
- `README.md`
- `TODO.md`
