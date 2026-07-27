# Frontend Test Configuration

## Context

The frontend used React, Vite, and TypeScript, but did not have a test runner, jsdom environment, React Testing Library, or coverage provider configured.

## Prompt

Configure the frontend tests now.

## Result

Configured Vitest with jsdom, React Testing Library, jest-dom, user-event, and V8 coverage. Added focused tests for the API client, HTTP service modules, and the Reports page/map behavior.

## Review

Accepted a minimal, useful test setup without adding broad end-to-end tests. Backend coverage tooling remains separate and pending.

## Related Files

- `frontend/package.json`
- `frontend/package-lock.json`
- `frontend/vite.config.ts`
- `frontend/tsconfig.json`
- `frontend/src/test/setup.ts`
- `frontend/src/services/apiClient.test.ts`
- `frontend/src/services/httpServices.test.ts`
- `frontend/src/pages/ReportsPage.test.tsx`
- `TESTING.md`
- `README.md`
- `TODO.md`
