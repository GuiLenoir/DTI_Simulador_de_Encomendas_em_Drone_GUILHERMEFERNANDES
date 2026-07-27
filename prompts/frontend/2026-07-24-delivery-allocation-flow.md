# Delivery Allocation Flow

## Context

The backend already exposes delivery allocation through `POST /api/deliveries/allocate/{orderId}` and route listing through `GET /api/deliveries/routes`.

## Prompt

Implement the delivery allocation flow. Stop after all tests pass.

## Result

Added frontend delivery API integration, an allocation action for pending orders, route listing, delivery status labels, allocation summaries, and user-friendly allocation errors.

## Review

Verified with `npm run build` and `dotnet test backend/DroneDelivery.Tests/DroneDelivery.Tests.csproj`.

## Related Files

- `frontend/src/pages/OrderPage.tsx`
- `frontend/src/services/deliveriesApi.ts`
- `frontend/src/types/api.ts`
- `frontend/src/utils/labels.ts`
- `frontend/src/styles.css`
- `TODO.md`
