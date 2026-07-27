export type DroneStatus = "Idle" | "Loading" | "Flying" | "Delivering" | "Returning" | "Charging" | "Maintenance" | "Unavailable";
export type OrderPriority = "Low" | "Medium" | "High";
export type OrderStatus = "Pending" | "Allocated" | "InTransit" | "Delivered" | "Rejected";
export type DeliveryStatus = "Allocated" | "InTransit" | "Delivered" | "Failed";
export type OrderQueueStatus = "NotQueued" | "Queued" | "Planned" | "Allocated" | "Completed" | "Cancelled";
export type TripStatus = "Planned" | "Loading" | "Flying" | "Delivering" | "Returning" | "Completed" | "Cancelled";

export type DroneResponse = {
  id: number;
  code: string;
  name: string;
  maxPackageWeightKg: number;
  maxRangeKm: number;
  batteryLevelPercent: number;
  batterySafetyMarginPercentagePoints: number;
  averageSpeedKmPerHour: number;
  batteryConsumptionPercentagePerKm: number;
  currentX: number;
  currentY: number;
  status: DroneStatus | number;
  notes?: string | null;
  isActive: boolean;
  hasExecutingTrip: boolean;
  hasPlannedTrips: boolean;
  chargingStartedAtUtc?: string | null;
  chargingCompletedAtUtc?: string | null;
  chargingProgressPercentage: number;
  createdAt: string;
  updatedAt: string;
};

export type DroneRequest = {
  code: string;
  name: string;
  maxPackageWeightKg: number;
  maxRangeKm: number;
  batteryLevelPercent: number;
  averageSpeedKmPerHour: number;
  batteryConsumptionPercentagePerKm: number;
  currentX: number;
  currentY: number;
  status: DroneStatus;
  notes?: string | null;
  isActive: boolean;
};

export type DroneSettingsResponse = {
  batterySafetyMarginPercentagePoints: number;
  updatedAtUtc: string;
};

export type CreateOrderRequest = {
  customerName: string;
  destinationX: number;
  destinationY: number;
  packageWeightKg: number;
  priority: OrderPriority;
};

export type OrderResponse = {
  id: number;
  customerName: string;
  destinationX: number;
  destinationY: number;
  packageWeightKg: number;
  priority: OrderPriority | number;
  status: OrderStatus | number;
  queueStatus: OrderQueueStatus | number;
  queuedAtUtc?: string | null;
  createdAt: string;
  updatedAt: string;
};

export type DeliveryResponse = {
  id: number;
  droneId: number;
  droneCode: string;
  orderId: number;
  status: DeliveryStatus | number;
  startX: number;
  startY: number;
  destinationX: number;
  destinationY: number;
  endX: number;
  endY: number;
  estimatedDistanceKm: number;
  estimatedBatteryConsumptionPercent: number;
  estimatedDurationMinutes: number;
  allocatedAt: string;
  deliveredAt?: string | null;
};

export type DashboardDroneResponse = {
  id: number;
  code: string;
  batteryLevelPercent: number;
  currentX: number;
  currentY: number;
  maxPackageWeightKg: number;
  maxRangeKm: number;
  status: DroneStatus | number;
  activeOrderId?: number | null;
  activeDeliveryId?: number | null;
  activeTripId?: number | null;
  batterySafetyMarginPercentagePoints: number;
  chargingStartedAtUtc?: string | null;
  chargingCompletedAtUtc?: string | null;
  chargingProgressPercentage: number;
};

export type DashboardDeliveryResponse = {
  id: number;
  orderId: number;
  droneId: number;
  droneCode: string;
  status: DeliveryStatus | number;
  currentPhase: string;
  currentPhaseStartedAtUtc: string;
  nextPhaseAtUtc: string;
  completedAtUtc: string;
  elapsedSeconds: number;
  remainingPhaseSeconds: number;
  progressPercentage: number;
  estimatedDistanceKm: number;
  estimatedBatteryConsumptionPercent: number;
  destinationX: number;
  destinationY: number;
};

export type TripOrderResponse = {
  orderId: number;
  customerName: string;
  priority: OrderPriority | number;
  packageWeightKg: number;
  destinationX: number;
  destinationY: number;
  deliverySequence: number;
  estimatedArrivalAtUtc: string;
};

export type TripResponse = {
  id: number;
  droneId: number;
  droneCode: string;
  status: TripStatus | number;
  currentPhase: string;
  plannedAtUtc: string;
  loadingStartedAtUtc: string;
  flyingStartedAtUtc: string;
  deliveringStartedAtUtc: string;
  returningStartedAtUtc: string;
  completedAtUtc: string;
  nextPhaseAtUtc: string;
  remainingPhaseSeconds: number;
  progressPercentage: number;
  totalWeightKg: number;
  maximumWeightKg: number;
  capacityUsagePercentage: number;
  estimatedDistanceKm: number;
  estimatedBatteryConsumptionPercentagePoints: number;
  batterySafetyMarginPercentagePoints: number;
  minimumRequiredBatteryPercentage: number;
  batteryAtDeparturePercentage: number;
  expectedBatteryAtReturnPercentage: number;
  orders: TripOrderResponse[];
};

export type UpcomingTripsResponse = {
  generatedAtUtc: string;
  upcomingTrips: UpcomingTripResponse[];
  unplannedOrders: UnplannedOrderResponse[];
};

export type UpcomingTripResponse = {
  tripId?: number | null;
  droneCode?: string | null;
  orders: UpcomingTripOrderResponse[];
  totalWeightKg: number;
  droneCapacityKg?: number | null;
  capacityUsagePercentage: number;
  estimatedDistanceKm: number;
  estimatedBatteryConsumptionPercentagePoints: number;
  batterySafetyMarginPercentagePoints: number;
  minimumRequiredBatteryPercentage: number;
  estimatedStartAtUtc?: string | null;
  waitingCode: string;
  waitingReason: string;
  friendlyStatus: string;
  blockingTripId?: number | null;
};

export type UpcomingTripOrderResponse = {
  orderId: number;
  orderCode: string;
  customerName: string;
  priority: OrderPriority | number;
  packageWeightKg: number;
};

export type UnplannedOrderResponse = {
  orderId: number;
  orderCode: string;
  customerName: string;
  priority: OrderPriority | number;
  packageWeightKg: number;
  queuedAtUtc?: string | null;
  waitingCode: string;
  waitingReason: string;
};

export type DeliveryPlanningResponse = {
  tripsCreated: number;
  ordersAllocated: number;
  ordersRemainingQueued: number;
  trips: TripResponse[];
  unallocatedOrders: { orderId: number; customerName: string; reason: string }[];
};

export type NoFlyZonePoint = {
  x: number;
  y: number;
};

export type NoFlyZoneResponse = {
  id: number;
  name: string;
  isActive: boolean;
  points: NoFlyZonePoint[];
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type NoFlyZoneRequest = {
  name: string;
  isActive: boolean;
  points: NoFlyZonePoint[];
};

export type DashboardResponse = {
  currentUtc: string;
  completedDeliveries: number;
  pendingDeliveries: number;
  averageDeliveryMinutes: number;
  mostEfficientDrone?: string | null;
  drones: DashboardDroneResponse[];
  orders: OrderResponse[];
  activeDeliveries: DashboardDeliveryResponse[];
  plannedTrips: TripResponse[];
  activeTrips: TripResponse[];
  queuedOrders: OrderResponse[];
};

export type ReportResponse = {
  summary: {
    completedDeliveries: number;
    averageDeliverySeconds: number;
  };
  mostEfficientDrone?: {
    droneId: number;
    droneCode: string;
    completedDeliveries: number;
    totalTransportedWeightKg: number;
    totalDistanceKm: number;
    totalBatteryConsumedPercentagePoints: number;
    efficiencyScore: number;
  } | null;
  map: {
    displayedDeliveries: number;
    usedDrones: number;
    totalDistanceKm: number;
    journeys: DeliveryMapJourneyResponse[];
  };
};

export type DeliveryMapJourneyResponse = {
  id: string;
  tripId?: number | null;
  deliveryId?: number | null;
  droneId: number;
  droneCode: string;
  completedAtUtc: string;
  distanceKm: number;
  points: DeliveryMapPointResponse[];
};

export type DeliveryMapPointResponse = {
  sequence: number;
  type: string;
  orderId?: number | null;
  orderCode?: string | null;
  priority?: OrderPriority | number | null;
  weightKg?: number | null;
  x: number;
  y: number;
  completedAtUtc?: string | null;
};

export type CustomerOrderRequest = {
  customerName: string;
  packageDescription?: string | null;
  packageWeightKg: number;
  destinationX: number;
  destinationY: number;
  priority: OrderPriority;
};

export type CustomerOrderCreatedResponse = {
  orderId: number;
  orderCode: string;
};

export type CustomerTrackingResponse = {
  orderId: number;
  orderCode: string;
  friendlyStatus: string;
  internalStatus: string;
  droneCode?: string | null;
  tripId?: number | null;
  deliveryId?: number | null;
  priority: OrderPriority | number;
  weightKg: number;
  destination: { x: number; y: number };
  route: CustomerRoutePointResponse[];
  tripStartedAtUtc?: string | null;
  estimatedCompletionAtUtc?: string | null;
  progressPercentage: number;
  remainingDistance: number;
  currentPosition: { x: number; y: number };
  feedbackMessage: string;
};

export type CustomerRoutePointResponse = {
  sequence: number;
  type: string;
  orderId?: number | null;
  orderCode?: string | null;
  priority?: OrderPriority | number | null;
  weightKg?: number | null;
  x: number;
  y: number;
};

export type ProblemDetails = {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
};
