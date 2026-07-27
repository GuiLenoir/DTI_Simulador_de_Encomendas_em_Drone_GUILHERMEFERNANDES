# Delivery Status Pagination Filters

## Context

The order page shows registered orders, delivery queue, and delivery status for individual deliveries and multi-order trips.

## Prompt

The user asked for tooltips to work on trip rows and for the registered orders and delivery status sections to support pagination and filters.

## Result

Registered orders now have status and weight filters plus 10-item pagination. Delivery status now has 10-item pagination and filters for trip type, drone, status, minimum and maximum weight, minimum and maximum distance, and minimum battery. Trip order numbers now show a hover/focus tooltip with each order and customer name.

## Review

Frontend build and backend tests passed after the change.

## Related Files

- `frontend/src/pages/OrderPage.tsx`
- `frontend/src/styles.css`
