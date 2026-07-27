import type { DeliveryStatus, DroneStatus, OrderPriority, OrderQueueStatus, OrderStatus, TripStatus } from "../types/api";

const droneStatusByNumber: Record<number, DroneStatus> = {
  1: "Idle",
  2: "Loading",
  3: "Flying",
  4: "Delivering",
  5: "Returning",
  6: "Charging",
  7: "Maintenance",
  8: "Unavailable"
};

const orderStatusByNumber: Record<number, OrderStatus> = {
  1: "Pending",
  2: "Allocated",
  3: "InTransit",
  4: "Delivered",
  5: "Rejected"
};

const priorityByNumber: Record<number, OrderPriority> = {
  1: "Low",
  2: "Medium",
  3: "High"
};

const deliveryStatusByNumber: Record<number, DeliveryStatus> = {
  1: "Allocated",
  2: "InTransit",
  3: "Delivered",
  4: "Failed"
};

const queueStatusByNumber: Record<number, OrderQueueStatus> = {
  1: "NotQueued",
  2: "Queued",
  3: "Planned",
  4: "Allocated",
  5: "Completed",
  6: "Cancelled"
};

const tripStatusByNumber: Record<number, TripStatus> = {
  1: "Planned",
  2: "Loading",
  3: "Flying",
  4: "Delivering",
  5: "Returning",
  6: "Completed",
  7: "Cancelled"
};

export const droneStatusLabels: Record<DroneStatus, string> = {
  Idle: "Disponível",
  Loading: "Carregando",
  Flying: "Em voo",
  Delivering: "Entregando",
  Returning: "Retornando à base",
  Charging: "Recarregando",
  Maintenance: "Em manutenÃ§Ã£o",
  Unavailable: "IndisponÃ­vel"
};

export const orderStatusLabels: Record<OrderStatus, string> = {
  Pending: "Pendente",
  Allocated: "Drone alocado",
  InTransit: "Em trânsito",
  Delivered: "Entregue",
  Rejected: "Rejeitado"
};

export const priorityLabels: Record<OrderPriority, string> = {
  Low: "Baixa",
  Medium: "Média",
  High: "Alta"
};

export const deliveryStatusLabels: Record<DeliveryStatus, string> = {
  Allocated: "Alocada",
  InTransit: "Em trânsito",
  Delivered: "Entrega concluída",
  Failed: "Falhou"
};

export const queueStatusLabels: Record<OrderQueueStatus, string> = {
  NotQueued: "Fora da fila",
  Queued: "Na fila",
  Planned: "Planejado",
  Allocated: "Alocado",
  Completed: "Conclui­do",
  Cancelled: "Cancelado"
};

export const tripStatusLabels: Record<TripStatus, string> = {
  Planned: "Planejada",
  Loading: "Carregando",
  Flying: "Em voo",
  Delivering: "Entregando",
  Returning: "Retornando à base",
  Completed: "Concluída",
  Cancelled: "Cancelada"
};

export const phaseLabels: Record<string, string> = {
  Planned: "Planejada",
  Loading: "Carregando",
  Flying: "Em voo",
  Delivering: "Entregando",
  Returning: "Retornando à base",
  Completed: "Entrega concluída"
};

export function getDroneStatusLabel(value: DroneStatus | number): string {
  const status = typeof value === "number" ? droneStatusByNumber[value] : value;
  return status ? droneStatusLabels[status] : "Status desconhecido";
}

export function getOrderStatusLabel(value: OrderStatus | number): string {
  const status = typeof value === "number" ? orderStatusByNumber[value] : value;
  return status ? orderStatusLabels[status] : "Status desconhecido";
}

export function getPriorityLabel(value: OrderPriority | number): string {
  const priority = typeof value === "number" ? priorityByNumber[value] : value;
  return priority ? priorityLabels[priority] : "Prioridade desconhecida";
}

export function getDeliveryStatusLabel(value: DeliveryStatus | number): string {
  const status = typeof value === "number" ? deliveryStatusByNumber[value] : value;
  return status ? deliveryStatusLabels[status] : "Status desconhecido";
}

export function getQueueStatusLabel(value: OrderQueueStatus | number): string {
  const status = typeof value === "number" ? queueStatusByNumber[value] : value;
  return status ? queueStatusLabels[status] : "Status desconhecido";
}

export function getTripStatusLabel(value: TripStatus | number): string {
  const status = typeof value === "number" ? tripStatusByNumber[value] : value;
  return status ? tripStatusLabels[status] : "Status desconhecido";
}

export function getPhaseLabel(value: string): string {
  return phaseLabels[value] ?? "Etapa desconhecida";
}
