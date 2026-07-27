# Time-Flow Route Map

## Context

The order details map used dashed arrows for every segment. On denser routes, arrows overlapped and made the delivery sequence harder to read.

## Prompt

Improve route visualization by replacing arrow-heavy drawing with a time-flow view: large base marker, numbered stops, continuous outbound line, dashed return line, unique trip colors, hover highlighting, point tooltips, legend, and a clickable trip-flow side panel.

## Result

Refactored the order details route map to render journey-based flows with colored markers and lines, native hover details for stops, a legend, and a side panel that centers the map on selected route points.

## Review

Kept existing zoom and pan behavior. The selected order currently shows its related trip or individual delivery route.

## Related Files

- `frontend/src/pages/OrderPage.tsx`
- `frontend/src/styles.css`
- `TODO.md`
