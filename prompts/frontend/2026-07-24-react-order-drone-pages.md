# React Order And Drone Pages

## Context

Implement the first frontend slice for the Drone Delivery Simulator. The backend already exposes `/api/orders` and `/api/drones/status`.

## Prompt

Implement the frontend. Stop after React, API integration, Order page, Drone page. Update TODO.md.

## Result

Created a Vite React TypeScript app with a shared API client, order creation/list page, drone status page, pt-BR enum labels, formatting helpers, loading states, empty states, and friendly error messages.

## Review

Kept API calls in service modules, displayed all visible text in Brazilian Portuguese, and verified the frontend with `npm run build`.

## Related Files

- `frontend/src/App.tsx`
- `frontend/src/pages/OrderPage.tsx`
- `frontend/src/pages/DronePage.tsx`
- `frontend/src/services`
- `TODO.md`
