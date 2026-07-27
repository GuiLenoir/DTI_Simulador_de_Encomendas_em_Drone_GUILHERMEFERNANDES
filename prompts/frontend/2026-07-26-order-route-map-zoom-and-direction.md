# Order Route Map Zoom And Direction

## Context

The order details modal already had a draggable Cartesian route map, but route arrows were visually crowded and there was no zoom control.

## Prompt

Improve the order details Cartesian map by adding zoom and making route lines/arrows clearer when route segments overlap.

## Result

Added zoom controls, reset behavior, zoom-aware panning, and offset direction indicators that sit beside the route segments instead of directly over the main route line.

## Review

Kept the continuous route polyline as the primary visual and used smaller direction indicators to clarify sequence without cluttering the path.

## Related Files

- `frontend/src/pages/OrderPage.tsx`
- `frontend/src/styles.css`
- `TODO.md`
