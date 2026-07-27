# Customer Received At Destination

## Context

The simulated customer tracking page considered the delivery complete only after the drone returned to base, because the API exposed the full operational route completion time.

## Prompt

No cliente simulado, ele só termina quando o drone volta a base

FAz um aviso quando o cliente RECEBE o pedido
O drone ainda deve continuar voltando pra base mas o status de entrega concluida nessa parte é quando ela chega no ponto de entrega

Se possivel a visualização do drone só va até o ponto de entrega

## Result

Customer tracking now marks the order as received when the drone reaches the customer destination. The operational drone simulation still continues returning to base, while the customer map and progress stop at the delivery point.

## Review

Accepted as a customer-facing tracking adjustment. Backend delivery/trip lifecycle rules were preserved.

## Related Files

- `backend/DroneDelivery.Api/Services/CustomerSimulationService.cs`
- `backend/DroneDelivery.Tests/CustomerSimulationServiceTests.cs`
- `frontend/src/pages/CustomerSimulationPage.tsx`
