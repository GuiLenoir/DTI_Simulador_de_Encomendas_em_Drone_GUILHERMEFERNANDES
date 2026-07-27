# Reports Map Explorer

## Context

The reports tab already had a Cartesian map showing completed delivery history, but it rendered all routes at once.

## Prompt

Improve only the existing Cartesian map in the `Relatorios` tab. Do not change backend, indicators, filters, other maps, planning, or execution rules. Show delivery points by default, hide routes initially, add interactive trip selection, remove arrows, use numbered stops for the selected trip, and keep an optional toggle for all routes.

## Result

Updated the report-only map rendering to behave as a delivery explorer with stable trip colors, selected-trip route display, dimmed unrelated points, numbered selected stops, dashed return-to-base segment, and responsive trip selector.

## Review

Accepted with frontend build validation. No backend changes were made.

## Related Files

- `frontend/src/pages/ReportsPage.tsx`
- `frontend/src/styles.css`
