# Drone Modal Text Fixes

## Context

The drone management page already had a create/edit modal and localized labels, but some visible text had typos, missing accents, or encoding artifacts.

## Prompt

Modal de criar drones tem uns erros de digitação / foramtação do texto principalemnte em status operacional

## Result

Corrected visible pt-BR labels on the drone page and shared enum label mapper. Limited editable drone statuses to operational/admin states while preserving read-only display for execution states returned by the API.

## Review

Accepted as a frontend-only text and presentation fix. No backend rules were changed.

## Related Files

- `frontend/src/pages/DronePage.tsx`
- `frontend/src/utils/labels.ts`
