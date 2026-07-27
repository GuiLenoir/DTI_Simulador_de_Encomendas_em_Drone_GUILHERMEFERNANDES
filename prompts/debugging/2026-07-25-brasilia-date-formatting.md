# Brasilia Date Formatting

## Context

The frontend displayed order creation and delivery allocation timestamps with an unexpected time zone offset.

## Prompt

User reported that times shown after creating orders and allocating deliveries looked wrong and should use current UTC/Brasília time.

## Result

Updated frontend date formatting to interpret timestamp strings without an explicit offset as UTC and display them with `pt-BR` formatting fixed to `America/Sao_Paulo`.

## Review

Kept UTC persistence in the backend and handled local presentation in the frontend. Documented the behavior in README.

## Related Files

- `frontend/src/utils/formatters.ts`
- `README.md`
