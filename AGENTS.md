# Codex Agent Instructions

Before making changes, read:

1. `rules.md`
2. `TODO.md`
3. `README.md`

If `rules.md` is missing, stop and ask the user to add it.

## Working rules

- Implement one coherent task at a time.
- Keep ASP.NET Core controllers thin.
- Put business logic in backend services.
- Use DTOs for API requests and responses.
- Use Entity Framework Core for persistence.
- Use async database operations and pass cancellation tokens.
- Use English for code, API routes, identifiers, and documentation.
- Add or update tests for every business rule.
- Do not introduce microservices, CQRS, MediatR, message brokers, authentication, or external map providers unless explicitly requested.
- Prefer simple, readable code over abstractions.
- Run the relevant build and tests before finishing.
- Update `TODO.md` only after verifying that a task works.
- Record significant AI requests in the appropriate file under `prompts/`.

## Validation commands

Backend:

```bash
dotnet restore backend/DroneDelivery.Api/DroneDelivery.Api.csproj
dotnet build backend/DroneDelivery.Api/DroneDelivery.Api.csproj
dotnet test backend/DroneDelivery.Tests/DroneDelivery.Tests.csproj
```

Frontend:

```bash
cd frontend
npm install
npm run build
npm run test
```

Full application:

```bash
docker compose up --build
```
