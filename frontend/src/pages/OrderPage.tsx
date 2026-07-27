import { FormEvent, PointerEvent, useEffect, useMemo, useRef, useState } from "react";
import { EmptyState, ErrorState, LoadingState } from "../components/PageState";
import { ApiError } from "../services/apiClient";
import { allocateDelivery, getDeliveryRoutes } from "../services/deliveriesApi";
import { getTrips, planDeliveries } from "../services/deliveryPlanningApi";
import { createOrder, getOrders, queueOrder, removeOrderFromQueue } from "../services/ordersApi";
import type {
  CreateOrderRequest,
  DeliveryPlanningResponse,
  DeliveryResponse,
  OrderPriority,
  OrderResponse,
  TripResponse
} from "../types/api";
import { formatDateTime, formatDecimal } from "../utils/formatters";
import {
  getDeliveryStatusLabel,
  getOrderStatusLabel,
  getPriorityLabel,
  getQueueStatusLabel,
  getTripStatusLabel,
  priorityLabels
} from "../utils/labels";

const initialForm: CreateOrderRequest = {
  customerName: "",
  destinationX: 0,
  destinationY: 0,
  packageWeightKg: 1,
  priority: "Medium"
};

const orderPageSize = 5;
const deliveryPageSize = 10;

type OrderFilters = {
  status: string;
  minWeight: string;
  maxWeight: string;
};

type DeliveryFilters = {
  type: string;
  drone: string;
  status: string;
  minWeight: string;
  maxWeight: string;
  minDistance: string;
  maxDistance: string;
  minBattery: string;
};

type DeliveryStatusRow = {
  id: string;
  type: "Trip" | "Individual";
  typeLabel: string;
  orderIds: number[];
  orderTooltip: string;
  droneCode: string;
  statusKey: string;
  statusLabel: string;
  weightKg: number;
  weightLabel: string;
  distanceKm: number;
  minimumBattery: number;
};

export function OrderPage() {
  const [orders, setOrders] = useState<OrderResponse[]>([]);
  const [deliveries, setDeliveries] = useState<DeliveryResponse[]>([]);
  const [trips, setTrips] = useState<TripResponse[]>([]);
  const [form, setForm] = useState<CreateOrderRequest>(initialForm);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isPlanning, setIsPlanning] = useState(false);
  const [allocatingOrderId, setAllocatingOrderId] = useState<number | null>(null);
  const [queueingOrderId, setQueueingOrderId] = useState<number | null>(null);
  const [lastDelivery, setLastDelivery] = useState<DeliveryResponse | null>(null);
  const [lastPlan, setLastPlan] = useState<DeliveryPlanningResponse | null>(null);
  const [selectedOrder, setSelectedOrder] = useState<OrderResponse | null>(null);
  const [orderPage, setOrderPage] = useState(1);
  const [deliveryPage, setDeliveryPage] = useState(1);
  const [orderFilters, setOrderFilters] = useState<OrderFilters>({ status: "", minWeight: "", maxWeight: "" });
  const [deliveryFilters, setDeliveryFilters] = useState<DeliveryFilters>({
    type: "",
    drone: "",
    status: "",
    minWeight: "",
    maxWeight: "",
    minDistance: "",
    maxDistance: "",
    minBattery: ""
  });
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const queuedOrders = orders.filter(isQueued);
  const orderNameById = useMemo(() => new Map(orders.map((order) => [order.id, order.customerName])), [orders]);
  const orderById = useMemo(() => new Map(orders.map((order) => [order.id, order])), [orders]);

  const filteredOrders = useMemo(() => {
    return orders.filter((order) => {
      const status = normalizeValue(order.status);
      return matchesText(status, orderFilters.status) &&
        matchesMin(order.packageWeightKg, orderFilters.minWeight) &&
        matchesMax(order.packageWeightKg, orderFilters.maxWeight);
    });
  }, [orders, orderFilters]);

  const deliveryRows = useMemo(() => {
    const tripRows: DeliveryStatusRow[] = trips.map((trip) => ({
      id: `trip-${trip.id}`,
      type: "Trip",
      typeLabel: `Viagem #${trip.id}`,
      orderIds: trip.orders.map((order) => order.orderId),
      orderTooltip: trip.orders.map((order) => `#${order.orderId} - ${order.customerName}`).join("\n"),
      droneCode: trip.droneCode,
      statusKey: normalizeValue(trip.status),
      statusLabel: getTripStatusLabel(trip.status),
      weightKg: trip.totalWeightKg,
      weightLabel: `${formatDecimal(trip.totalWeightKg)} kg / ${formatDecimal(trip.maximumWeightKg)} kg`,
      distanceKm: trip.estimatedDistanceKm,
      minimumBattery: trip.minimumRequiredBatteryPercentage
    }));

    const individualRows: DeliveryStatusRow[] = deliveries.map((delivery) => ({
      id: `delivery-${delivery.id}`,
      type: "Individual",
      typeLabel: `Individual #${delivery.id}`,
      orderIds: [delivery.orderId],
      orderTooltip: `#${delivery.orderId} - ${orderNameById.get(delivery.orderId) ?? "Pedido nao encontrado"}`,
      droneCode: delivery.droneCode,
      statusKey: normalizeValue(delivery.status),
      statusLabel: getDeliveryStatusLabel(delivery.status),
      weightKg: orderById.get(delivery.orderId)?.packageWeightKg ?? 0,
      weightLabel: orderById.get(delivery.orderId)
        ? `${formatDecimal(orderById.get(delivery.orderId)!.packageWeightKg)} kg`
        : "1 pedido",
      distanceKm: delivery.estimatedDistanceKm,
      minimumBattery: delivery.estimatedBatteryConsumptionPercent
    }));

    return [...tripRows, ...individualRows];
  }, [deliveries, orderById, orderNameById, trips]);

  const filteredDeliveryRows = useMemo(() => {
    return deliveryRows.filter((row) =>
      matchesText(row.type, deliveryFilters.type) &&
      matchesText(row.droneCode, deliveryFilters.drone) &&
      matchesText(row.statusKey, deliveryFilters.status) &&
      matchesMin(row.weightKg, deliveryFilters.minWeight) &&
      matchesMax(row.weightKg, deliveryFilters.maxWeight) &&
      matchesMin(row.distanceKm, deliveryFilters.minDistance) &&
      matchesMax(row.distanceKm, deliveryFilters.maxDistance) &&
      matchesMin(row.minimumBattery, deliveryFilters.minBattery)
    );
  }, [deliveryFilters, deliveryRows]);

  const pagedOrders = paginate(filteredOrders, orderPage, orderPageSize);
  const pagedDeliveryRows = paginate(filteredDeliveryRows, deliveryPage, deliveryPageSize);
  const orderPageCount = getPageCount(filteredOrders.length, orderPageSize);
  const deliveryPageCount = getPageCount(filteredDeliveryRows.length, deliveryPageSize);
  const droneOptions = Array.from(new Set(deliveryRows.map((row) => row.droneCode))).sort();
  const statusOptions = Array.from(new Set(deliveryRows.map((row) => row.statusKey))).sort();

  async function loadOperationData() {
    setIsLoading(true);
    setError(null);
    try {
      const [loadedOrders, loadedDeliveries, loadedTrips] = await Promise.all([getOrders(), getDeliveryRoutes(), getTrips()]);
      setOrders(loadedOrders);
      setDeliveries(loadedDeliveries);
      setTrips(loadedTrips);
    } catch (err) {
      setError(getFriendlyError(err));
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    void loadOperationData();
  }, []);

  useEffect(() => {
    setOrderPage(1);
  }, [orderFilters]);

  useEffect(() => {
    setDeliveryPage(1);
  }, [deliveryFilters]);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setSuccess(null);
    setIsSaving(true);

    try {
      await createOrder(form);
      setForm(initialForm);
      setSuccess("Pedido criado com sucesso.");
      await loadOperationData();
    } catch (err) {
      setError(getFriendlyError(err));
    } finally {
      setIsSaving(false);
    }
  }

  async function handleAllocate(order: OrderResponse) {
    setError(null);
    setSuccess(null);
    setLastDelivery(null);
    setAllocatingOrderId(order.id);

    try {
      const delivery = await allocateDelivery(order.id);
      setLastDelivery(delivery);
      setSuccess(`Pedido alocado para o drone ${delivery.droneCode}.`);
      await loadOperationData();
    } catch (err) {
      setError(getFriendlyError(err));
    } finally {
      setAllocatingOrderId(null);
    }
  }

  async function handleQueue(order: OrderResponse) {
    setError(null);
    setSuccess(null);
    setQueueingOrderId(order.id);

    try {
      if (isQueued(order)) {
        await removeOrderFromQueue(order.id);
        setSuccess("Pedido removido da fila.");
      } else {
        await queueOrder(order.id);
        const plan = await planDeliveries();
        setLastPlan(plan);
        setSuccess("Pedido adicionado a fila e planejamento atualizado.");
      }
      await loadOperationData();
    } catch (err) {
      setError(getFriendlyError(err));
    } finally {
      setQueueingOrderId(null);
    }
  }

  async function handlePlanDeliveries() {
    setError(null);
    setSuccess(null);
    setLastPlan(null);
    setIsPlanning(true);

    try {
      const plan = await planDeliveries();
      setLastPlan(plan);
      setSuccess("Planejamento concluido.");
      await loadOperationData();
    } catch (err) {
      setError(getFriendlyError(err));
    } finally {
      setIsPlanning(false);
    }
  }

  return (
    <section className="page-grid">
      <form className="panel form-panel" onSubmit={handleSubmit}>
        <div className="panel-heading">
          <h3>Novo pedido</h3>
          <span>Coordenadas da cidade</span>
        </div>

        <label>
          Cliente
          <input
            value={form.customerName}
            onChange={(event) => setForm({ ...form, customerName: event.target.value })}
            placeholder="Nome do cliente"
            required
          />
        </label>

        <div className="field-row">
          <label>
            Posicao X
            <input
              type="number"
              step="0.01"
              value={form.destinationX}
              onChange={(event) => setForm({ ...form, destinationX: Number(event.target.value) })}
              required
            />
          </label>
          <label>
            Posicao Y
            <input
              type="number"
              step="0.01"
              value={form.destinationY}
              onChange={(event) => setForm({ ...form, destinationY: Number(event.target.value) })}
              required
            />
          </label>
        </div>

        <div className="field-row">
          <label>
            Peso do pacote
            <input
              type="number"
              min="0.01"
              step="0.01"
              value={form.packageWeightKg}
              onChange={(event) => setForm({ ...form, packageWeightKg: Number(event.target.value) })}
              required
            />
          </label>
          <label>
            Prioridade
            <select
              value={form.priority}
              onChange={(event) => setForm({ ...form, priority: event.target.value as OrderPriority })}
            >
              {Object.entries(priorityLabels).map(([value, label]) => (
                <option key={value} value={value}>
                  {label}
                </option>
              ))}
            </select>
          </label>
        </div>

        {error && <ErrorState message={error} />}
        {success && <div className="state state-success">{success}</div>}

        <button className="primary-action" disabled={isSaving} type="submit">
          {isSaving ? "Criando..." : "Criar pedido"}
        </button>
      </form>

      <section className="panel table-panel">
        <div className="panel-heading">
          <h3>Pedidos cadastrados</h3>
          <div className="action-row">
            <button className="secondary-action" type="button" onClick={() => void loadOperationData()}>
              Atualizar
            </button>
            <button className="primary-action compact-action" disabled={isPlanning} type="button" onClick={() => void handlePlanDeliveries()}>
              {isPlanning ? "Planejando..." : "Planejar entregas"}
            </button>
          </div>
        </div>

        {lastDelivery && (
          <div className="allocation-summary">
            <strong>Entrega alocada</strong>
            <span>Drone {lastDelivery.droneCode}</span>
            <span>{formatDecimal(lastDelivery.estimatedDistanceKm)} km</span>
            <span>{formatDecimal(lastDelivery.estimatedBatteryConsumptionPercent)}% de bateria</span>
          </div>
        )}

        {lastPlan && (
          <div className="allocation-summary">
            <strong>Planejamento concluido</strong>
            <span>{lastPlan.tripsCreated} viagens planejadas</span>
            <span>{lastPlan.ordersAllocated} pedidos alocados</span>
            <span>{lastPlan.ordersRemainingQueued} na fila</span>
          </div>
        )}

        <div className="filter-grid compact-filters">
          <label>
            Status
            <select value={orderFilters.status} onChange={(event) => setOrderFilters({ ...orderFilters, status: event.target.value })}>
              <option value="">Todos</option>
              <option value="Pending">Pendente</option>
              <option value="Allocated">Alocado</option>
              <option value="InTransit">Em transito</option>
              <option value="Delivered">Entregue</option>
            </select>
          </label>
          <label>
            Peso min.
            <input type="number" value={orderFilters.minWeight} onChange={(event) => setOrderFilters({ ...orderFilters, minWeight: event.target.value })} />
          </label>
          <label>
            Peso max.
            <input type="number" value={orderFilters.maxWeight} onChange={(event) => setOrderFilters({ ...orderFilters, maxWeight: event.target.value })} />
          </label>
        </div>

        {isLoading ? (
          <LoadingState message="Carregando pedidos..." />
        ) : filteredOrders.length === 0 ? (
          <EmptyState message="Nenhum pedido encontrado." />
        ) : (
          <>
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>ID</th>
                    <th>Cliente</th>
                    <th>Destino</th>
                    <th>Peso</th>
                    <th>Prioridade</th>
                    <th>Status</th>
                    <th>Fila</th>
                    <th>Criado em</th>
                    <th>Acao</th>
                  </tr>
                </thead>
                <tbody>
                  {pagedOrders.map((order) => (
                    <tr key={order.id}>
                      <td>#{order.id}</td>
                      <td>{order.customerName}</td>
                      <td>
                        ({formatDecimal(order.destinationX)}, {formatDecimal(order.destinationY)})
                      </td>
                      <td>{formatDecimal(order.packageWeightKg)} kg</td>
                      <td>{getPriorityLabel(order.priority)}</td>
                      <td>
                        <span className="status-pill">{getOrderStatusLabel(order.status)}</span>
                      </td>
                      <td>
                        <span className="status-pill">{getQueueStatusLabel(order.queueStatus)}</span>
                      </td>
                      <td>{formatDateTime(order.createdAt)}</td>
                      <td>
                        <div className="row-action-group">
                          <button
                            className="row-action"
                            disabled={!canAllocate(order) || allocatingOrderId === order.id}
                            type="button"
                            onClick={() => void handleAllocate(order)}
                          >
                            {allocatingOrderId === order.id ? "Alocando..." : "Alocar drone"}
                          </button>
                          <button
                            className="row-action muted-action"
                            disabled={!canToggleQueue(order) || queueingOrderId === order.id}
                            type="button"
                            onClick={() => void handleQueue(order)}
                          >
                            {isQueued(order) ? "Remover da fila" : "Adicionar a fila"}
                          </button>
                          <button className="row-action muted-action" type="button" onClick={() => setSelectedOrder(order)}>
                            Detalhes
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <Pagination page={orderPage} pageCount={orderPageCount} total={filteredOrders.length} onChange={setOrderPage} />
          </>
        )}
      </section>

      <section className="panel table-panel routes-panel">
        <div className="panel-heading">
          <h3>Fila de entrega</h3>
          <span>Pedidos ainda aguardando drone disponivel</span>
        </div>

        {queuedOrders.length === 0 ? (
          <EmptyState message="Nenhum pedido aguardando planejamento." />
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Pedido</th>
                  <th>Cliente</th>
                  <th>Prioridade</th>
                  <th>Peso</th>
                  <th>Entrada na fila</th>
                </tr>
              </thead>
              <tbody>
                {queuedOrders.map((order) => (
                  <tr key={order.id}>
                    <td>#{order.id}</td>
                    <td>{order.customerName}</td>
                    <td>{getPriorityLabel(order.priority)}</td>
                    <td>{formatDecimal(order.packageWeightKg)} kg</td>
                    <td>{order.queuedAtUtc ? formatDateTime(order.queuedAtUtc) : "Nao informado"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section className="panel table-panel routes-panel">
        <div className="panel-heading">
          <h3>Status de entregas</h3>
          <span>Entregas individuais e viagens planejadas</span>
        </div>

        <div className="filter-grid">
          <label>
            Tipo de viagem
            <select value={deliveryFilters.type} onChange={(event) => setDeliveryFilters({ ...deliveryFilters, type: event.target.value })}>
              <option value="">Todas</option>
              <option value="Trip">Viagem</option>
              <option value="Individual">Individual</option>
            </select>
          </label>
          <label>
            Drone
            <select value={deliveryFilters.drone} onChange={(event) => setDeliveryFilters({ ...deliveryFilters, drone: event.target.value })}>
              <option value="">Todos</option>
              {droneOptions.map((drone) => (
                <option key={drone} value={drone}>
                  {drone}
                </option>
              ))}
            </select>
          </label>
          <label>
            Status
            <select value={deliveryFilters.status} onChange={(event) => setDeliveryFilters({ ...deliveryFilters, status: event.target.value })}>
              <option value="">Todos</option>
              {statusOptions.map((status) => (
                <option key={status} value={status}>
                  {getStatusOptionLabel(status)}
                </option>
              ))}
            </select>
          </label>
          <label>
            Peso min.
            <input type="number" value={deliveryFilters.minWeight} onChange={(event) => setDeliveryFilters({ ...deliveryFilters, minWeight: event.target.value })} />
          </label>
          <label>
            Peso max.
            <input type="number" value={deliveryFilters.maxWeight} onChange={(event) => setDeliveryFilters({ ...deliveryFilters, maxWeight: event.target.value })} />
          </label>
          <label>
            Distancia min.
            <input type="number" value={deliveryFilters.minDistance} onChange={(event) => setDeliveryFilters({ ...deliveryFilters, minDistance: event.target.value })} />
          </label>
          <label>
            Distancia max.
            <input type="number" value={deliveryFilters.maxDistance} onChange={(event) => setDeliveryFilters({ ...deliveryFilters, maxDistance: event.target.value })} />
          </label>
          <label>
            Bateria minima
            <input type="number" value={deliveryFilters.minBattery} onChange={(event) => setDeliveryFilters({ ...deliveryFilters, minBattery: event.target.value })} />
          </label>
        </div>

        {filteredDeliveryRows.length === 0 ? (
          <EmptyState message="Nenhuma entrega encontrada." />
        ) : (
          <>
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Tipo</th>
                    <th>Pedidos</th>
                    <th>Drone</th>
                    <th>Status</th>
                    <th>Carga</th>
                    <th>Distancia</th>
                    <th>Bateria minima</th>
                  </tr>
                </thead>
                <tbody>
                  {pagedDeliveryRows.map((row) => (
                    <tr key={row.id}>
                      <td>{row.typeLabel}</td>
                      <td>
                        <span className="tooltip-anchor" data-tooltip={row.orderTooltip} tabIndex={0}>
                          {row.orderIds.map((orderId) => `#${orderId}`).join(", ")}
                        </span>
                      </td>
                      <td>{row.droneCode}</td>
                      <td>
                        <span className="status-pill">{row.statusLabel}</span>
                      </td>
                      <td>{row.weightLabel}</td>
                      <td>{formatDecimal(row.distanceKm)} km</td>
                      <td>{formatDecimal(row.minimumBattery)}%</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <Pagination page={deliveryPage} pageCount={deliveryPageCount} total={filteredDeliveryRows.length} onChange={setDeliveryPage} />
          </>
        )}
      </section>

      {selectedOrder && (
        <OrderDetailsModal
          deliveries={deliveries}
          onClose={() => setSelectedOrder(null)}
          order={selectedOrder}
          trips={trips}
        />
      )}
    </section>
  );
}

function OrderDetailsModal({
  order,
  deliveries,
  trips,
  onClose
}: {
  order: OrderResponse;
  deliveries: DeliveryResponse[];
  trips: TripResponse[];
  onClose: () => void;
}) {
  const route = buildOrderRoute(order, deliveries, trips);
  const bounds = getRouteBounds(route.journeys.flatMap((journey) => journey.points));
  const [pan, setPan] = useState({ x: 0, y: 0 });
  const [zoom, setZoom] = useState(1);
  const [hoveredJourneyId, setHoveredJourneyId] = useState<string | null>(null);
  const [dragStart, setDragStart] = useState<{ pointerId: number; clientX: number; clientY: number; panX: number; panY: number } | null>(null);
  const mapFrameRef = useRef<HTMLDivElement | null>(null);
  const viewWidth = 600 / zoom;
  const viewHeight = 360 / zoom;

  function centerOnPoint(point: RouteMapPoint) {
    const x = toRouteScreenX(point.x, bounds);
    const y = toRouteScreenY(point.y, bounds);
    setPan({
      x: x - viewWidth / 2 - (600 - viewWidth) / 2,
      y: y - viewHeight / 2 - (360 - viewHeight) / 2
    });
  }

  function handleMapPointerDown(event: PointerEvent<SVGSVGElement>) {
    event.preventDefault();
    event.stopPropagation();
    event.currentTarget.setPointerCapture(event.pointerId);
    setDragStart({ pointerId: event.pointerId, clientX: event.clientX, clientY: event.clientY, panX: pan.x, panY: pan.y });
  }

  function handleMapPointerMove(event: PointerEvent<SVGSVGElement>) {
    if (!dragStart) {
      return;
    }

    event.preventDefault();
    event.stopPropagation();
    setPan({
      x: dragStart.panX - (event.clientX - dragStart.clientX) / zoom,
      y: dragStart.panY - (event.clientY - dragStart.clientY) / zoom
    });
  }

  function handleMapPointerEnd(event: PointerEvent<SVGSVGElement>) {
    event.preventDefault();
    event.stopPropagation();
    if (event.currentTarget.hasPointerCapture(dragStart?.pointerId ?? event.pointerId)) {
      event.currentTarget.releasePointerCapture(dragStart?.pointerId ?? event.pointerId);
    }
    setDragStart(null);
  }

  useEffect(() => {
    const element = mapFrameRef.current;
    if (!element) {
      return;
    }

    function handleWheel(event: WheelEvent) {
      event.preventDefault();
      event.stopPropagation();
      setZoom((value) => Math.min(3, Math.max(0.75, value + (event.deltaY < 0 ? 0.15 : -0.15))));
    }

    element.addEventListener("wheel", handleWheel, { passive: false });
    return () => element.removeEventListener("wheel", handleWheel);
  }, []);

  useEffect(() => {
    const previousOverflow = document.body.style.overflow;
    const previousPaddingRight = document.body.style.paddingRight;
    const scrollbarWidth = window.innerWidth - document.documentElement.clientWidth;
    document.body.style.overflow = "hidden";
    if (scrollbarWidth > 0) {
      document.body.style.paddingRight = `${scrollbarWidth}px`;
    }

    return () => {
      document.body.style.overflow = previousOverflow;
      document.body.style.paddingRight = previousPaddingRight;
    };
  }, []);

  return (
    <div className="modal-backdrop" role="presentation" onMouseDown={onClose}>
      <section className="modal-panel" role="dialog" aria-modal="true" aria-label="Detalhes do pedido" onMouseDown={(event) => event.stopPropagation()}>
        <div className="panel-heading">
          <h3>Detalhes do pedido #{order.id}</h3>
          <button className="secondary-action" type="button" onClick={onClose}>
            Fechar
          </button>
        </div>

        <div className="detail-grid">
          <div>
            <span>ID do pedido</span>
            <strong>#{order.id}</strong>
          </div>
          <div>
            <span>Cliente</span>
            <strong>{order.customerName}</strong>
          </div>
          <div>
            <span>Destino</span>
            <strong>({formatDecimal(order.destinationX)}, {formatDecimal(order.destinationY)})</strong>
          </div>
          <div>
            <span>Peso</span>
            <strong>{formatDecimal(order.packageWeightKg)} kg</strong>
          </div>
          <div>
            <span>Prioridade</span>
            <strong>{getPriorityLabel(order.priority)}</strong>
          </div>
          <div>
            <span>Status</span>
            <strong>{getOrderStatusLabel(order.status)}</strong>
          </div>
          <div>
            <span>Fila</span>
            <strong>{getQueueStatusLabel(order.queueStatus)}</strong>
          </div>
          <div>
            <span>Criado em</span>
            <strong>{formatDateTime(order.createdAt)}</strong>
          </div>
          <div>
            <span>Entrada na fila</span>
            <strong>{order.queuedAtUtc ? formatDateTime(order.queuedAtUtc) : "Nao informado"}</strong>
          </div>
        </div>

        <div className="route-summary">
          <div>
            <strong>{route.title}</strong>
            <span>{route.description}</span>
          </div>
          <div className="map-controls" aria-label="Controles do mapa">
            <button className="secondary-action icon-action" type="button" onClick={() => setZoom((value) => Math.max(0.75, value - 0.25))}>
              -
            </button>
            <span>{Math.round(zoom * 100)}%</span>
            <button className="secondary-action icon-action" type="button" onClick={() => setZoom((value) => Math.min(3, value + 0.25))}>
              +
            </button>
            <button
              className="secondary-action"
              type="button"
              onClick={() => {
                setPan({ x: 0, y: 0 });
                setZoom(1);
              }}
            >
              Centralizar
            </button>
          </div>
        </div>

        <div className="route-visual-layout">
          <div className="map-frame" ref={mapFrameRef} onMouseDown={(event) => event.stopPropagation()}>
            <span className="map-hint">Arraste para mover</span>
            <svg
              className={dragStart ? "zone-map route-map dragging-map" : "zone-map route-map"}
              viewBox={`${pan.x + (600 - viewWidth) / 2} ${pan.y + (360 - viewHeight) / 2} ${viewWidth} ${viewHeight}`}
              role="img"
              aria-label="Mapa da rota do pedido"
              onPointerDown={handleMapPointerDown}
              onPointerLeave={() => setHoveredJourneyId(null)}
              onPointerMove={handleMapPointerMove}
              onPointerUp={handleMapPointerEnd}
              onPointerCancel={handleMapPointerEnd}
            >
              <line x1="300" y1="-3000" x2="300" y2="3000" />
              <line x1="-3000" y1="180" x2="3000" y2="180" />
              {route.journeys.map((journey) => {
                const isDimmed = hoveredJourneyId !== null && hoveredJourneyId !== journey.id;
                const points = journey.points.map((point) => `${toRouteScreenX(point.x, bounds)},${toRouteScreenY(point.y, bounds)}`).join(" ");
                const first = journey.points[0];
                const last = journey.points[journey.points.length - 1];
                return (
                  <g
                    className={isDimmed ? "route-journey dimmed-route" : "route-journey"}
                    key={journey.id}
                    onMouseEnter={() => setHoveredJourneyId(journey.id)}
                    onMouseLeave={() => setHoveredJourneyId(null)}
                  >
                    <polyline className="route-flow-line" points={points} style={{ stroke: journey.color }} />
                    {last && (
                      <line
                        className="route-return-line"
                        style={{ stroke: journey.color }}
                        x1={toRouteScreenX(last.x, bounds)}
                        y1={toRouteScreenY(last.y, bounds)}
                        x2={toRouteScreenX(first.x, bounds)}
                        y2={toRouteScreenY(first.y, bounds)}
                      />
                    )}
                    <circle cx={toRouteScreenX(first.x, bounds)} cy={toRouteScreenY(first.y, bounds)} r="11" className="route-base-marker" style={{ fill: journey.color }} />
                    <text x={toRouteScreenX(first.x, bounds)} y={toRouteScreenY(first.y, bounds)} className="route-marker-text">
                      B
                    </text>
                    {journey.stops.map((stop) => (
                      <g className="route-stop-marker" key={`${journey.id}-${stop.orderId}`}>
                        <title>{`Parada ${stop.sequence}\nPedido PED-${stop.orderId}\nPrioridade ${getPriorityLabel(stop.priority)}\nPeso ${formatDecimal(stop.weightKg)} kg\nDistancia acumulada ${formatDecimal(stop.accumulatedDistanceKm)} km`}</title>
                        <circle cx={toRouteScreenX(stop.x, bounds)} cy={toRouteScreenY(stop.y, bounds)} r="10" style={{ fill: journey.color }} />
                        <text x={toRouteScreenX(stop.x, bounds)} y={toRouteScreenY(stop.y, bounds)} className="route-marker-text">
                          {stop.sequence}
                        </text>
                      </g>
                    ))}
                  </g>
                );
              })}
            </svg>
          </div>

          <aside className="trip-flow-panel">
            <h4>Fluxo da Viagem</h4>
            {route.journeys.map((journey) => (
              <div className="trip-flow" key={journey.id}>
                <button type="button" onClick={() => centerOnPoint(journey.points[0])}>
                  <span style={{ background: journey.color }} />
                  <strong>Drone/Base</strong>
                </button>
                {journey.stops.map((stop) => (
                  <button key={stop.orderId} type="button" onClick={() => centerOnPoint(stop)}>
                    <span style={{ background: journey.color }}>{stop.sequence}</span>
                    <strong>PED-{stop.orderId}</strong>
                  </button>
                ))}
                <button type="button" onClick={() => centerOnPoint(journey.points[0])}>
                  <span className="return-dot" style={{ borderColor: journey.color }} />
                  <strong>Retorno à Base</strong>
                </button>
              </div>
            ))}
          </aside>
        </div>

        <div className="route-legend">
          {route.journeys.map((journey) => (
            <div key={journey.id}>
              <span style={{ background: journey.color }} />
              <strong>{journey.droneCode}</strong>
              <em>Viagem {journey.sequence}</em>
            </div>
          ))}
        </div>
      </section>
    </div>
  );
}

function Pagination({
  page,
  pageCount,
  total,
  onChange
}: {
  page: number;
  pageCount: number;
  total: number;
  onChange: (page: number) => void;
}) {
  return (
    <div className="pagination">
      <span>
        Pagina {page} de {pageCount} - {total} registros
      </span>
      <div className="action-row">
        <button className="secondary-action" disabled={page <= 1} type="button" onClick={() => onChange(page - 1)}>
          Anterior
        </button>
        <button className="secondary-action" disabled={page >= pageCount} type="button" onClick={() => onChange(page + 1)}>
          Proxima
        </button>
      </div>
    </div>
  );
}

type RouteMapPoint = {
  x: number;
  y: number;
  label: string;
};

type RouteStopPoint = RouteMapPoint & {
  orderId: number;
  priority: OrderPriority | number;
  weightKg: number;
  sequence: number;
  accumulatedDistanceKm: number;
};

type RouteJourney = {
  id: string;
  sequence: number;
  droneCode: string;
  color: string;
  points: RouteMapPoint[];
  stops: RouteStopPoint[];
};

type RouteView = {
  title: string;
  description: string;
  journeys: RouteJourney[];
};

const routeColors = ["#2563eb", "#2f6f63", "#d97706", "#7c3aed", "#dc2626", "#0891b2"];

function buildOrderRoute(order: OrderResponse, deliveries: DeliveryResponse[], trips: TripResponse[]): RouteView {
  const trip = trips.find((item) => item.orders.some((tripOrder) => tripOrder.orderId === order.id));
  if (trip) {
    const orderedStops = [...trip.orders].sort((a, b) => a.deliverySequence - b.deliverySequence);
    const base = { x: 0, y: 0, label: "Base" };
    const stops = orderedStops.map((stop, index) => ({
      x: stop.destinationX,
      y: stop.destinationY,
      label: `PED-${stop.orderId}`,
      orderId: stop.orderId,
      priority: stop.priority,
      weightKg: stop.packageWeightKg,
      sequence: index + 1,
      accumulatedDistanceKm: calculateAccumulatedDistance([base, ...orderedStops.slice(0, index + 1).map((item) => ({
        x: item.destinationX,
        y: item.destinationY,
        label: `PED-${item.orderId}`
      }))])
    }));
    return {
      title: `Viagem #${trip.id} - drone ${trip.droneCode}`,
      description: `${formatDecimal(trip.estimatedDistanceKm)} km estimados em sequencia de entrega`,
      journeys: [{
        id: `trip-${trip.id}`,
        sequence: 1,
        droneCode: trip.droneCode,
        color: routeColors[0],
        points: [base, ...stops],
        stops
      }]
    };
  }

  const delivery = deliveries.find((item) => item.orderId === order.id);
  if (delivery) {
    const base = { x: delivery.startX, y: delivery.startY, label: "Base" };
    const stop = {
      x: delivery.destinationX,
      y: delivery.destinationY,
      label: `PED-${order.id}`,
      orderId: order.id,
      priority: order.priority,
      weightKg: order.packageWeightKg,
      sequence: 1,
      accumulatedDistanceKm: calculateAccumulatedDistance([base, { x: delivery.destinationX, y: delivery.destinationY, label: `PED-${order.id}` }])
    };
    return {
      title: `Entrega individual #${delivery.id} - drone ${delivery.droneCode}`,
      description: `${formatDecimal(delivery.estimatedDistanceKm)} km estimados`,
      journeys: [{
        id: `delivery-${delivery.id}`,
        sequence: 1,
        droneCode: delivery.droneCode,
        color: routeColors[0],
        points: [base, stop],
        stops: [stop]
      }]
    };
  }

  const base = { x: 0, y: 0, label: "Base" };
  const stop = {
    x: order.destinationX,
    y: order.destinationY,
    label: `PED-${order.id}`,
    orderId: order.id,
    priority: order.priority,
    weightKg: order.packageWeightKg,
    sequence: 1,
    accumulatedDistanceKm: calculateAccumulatedDistance([base, { x: order.destinationX, y: order.destinationY, label: `PED-${order.id}` }])
  };
  return {
    title: "Pedido ainda sem rota planejada",
    description: "Destino cadastrado na malha cartesiana",
    journeys: [{
      id: `order-${order.id}`,
      sequence: 1,
      droneCode: "Aguardando drone",
      color: routeColors[0],
      points: [base, stop],
      stops: [stop]
    }]
  };
}

function calculateAccumulatedDistance(points: RouteMapPoint[]): number {
  return points.slice(1).reduce((total, point, index) => {
    const previous = points[index];
    return total + Math.hypot(point.x - previous.x, point.y - previous.y);
  }, 0);
}

function getRouteBounds(points: RouteMapPoint[]) {
  const values = points.flatMap((point) => [point.x, point.y]);
  const max = Math.max(10, ...values.map((value) => Math.abs(value)));
  return { min: -max - 1, max: max + 1 };
}

function toRouteScreenX(value: number, bounds: { min: number; max: number }): number {
  return ((value - bounds.min) / (bounds.max - bounds.min)) * 600;
}

function toRouteScreenY(value: number, bounds: { min: number; max: number }): number {
  return 360 - ((value - bounds.min) / (bounds.max - bounds.min)) * 360;
}

function paginate<T>(items: T[], page: number, pageSize: number): T[] {
  return items.slice((page - 1) * pageSize, page * pageSize);
}

function getPageCount(total: number, pageSize: number): number {
  return Math.max(1, Math.ceil(total / pageSize));
}

function normalizeValue(value: string | number): string {
  return String(value);
}

function matchesText(value: string, filter: string): boolean {
  return !filter || value.toLowerCase() === filter.toLowerCase();
}

function matchesMin(value: number, filter: string): boolean {
  return filter === "" || value >= Number(filter);
}

function matchesMax(value: number, filter: string): boolean {
  return filter === "" || value <= Number(filter);
}

function getStatusOptionLabel(status: string): string {
  const labels: Record<string, string> = {
    Allocated: "Alocada",
    InTransit: "Em transito",
    Delivered: "Entregue",
    Failed: "Falhou",
    Planned: "Planejada",
    Loading: "Carregando",
    Flying: "Em voo",
    Delivering: "Entregando",
    Returning: "Retornando",
    Completed: "Concluida",
    Cancelled: "Cancelada"
  };

  return labels[status] ?? "Status desconhecido";
}

function isPending(order: OrderResponse): boolean {
  return order.status === "Pending" || order.status === 1;
}

function isQueued(order: OrderResponse): boolean {
  return order.queueStatus === "Queued" || order.queueStatus === 2;
}

function canAllocate(order: OrderResponse): boolean {
  return isPending(order) && (order.queueStatus === "NotQueued" || order.queueStatus === 1);
}

function canToggleQueue(order: OrderResponse): boolean {
  return isPending(order) || isQueued(order);
}

function getFriendlyError(error: unknown): string {
  if (error instanceof ApiError) {
    const errors: Record<string, string> = {
      INVALID_PACKAGE_WEIGHT: "O peso do pacote deve ser maior que zero.",
      INVALID_CUSTOMER_NAME: "Informe o nome do cliente.",
      ORDER_DESTINATION_IN_NO_FLY_ZONE: "O destino do pedido esta dentro de uma zona de exclusao aerea ativa.",
      NO_ELIGIBLE_DRONE: "Nenhum drone disponivel consegue transportar este pacote nessa rota.",
      ORDER_NOT_PENDING: "Somente pedidos pendentes podem ser alocados.",
      ORDER_NOT_ELIGIBLE_FOR_QUEUE: "Este pedido nao pode entrar na fila.",
      ORDER_ALREADY_QUEUED: "Este pedido ja esta na fila.",
      TRIP_ALREADY_STARTED: "A viagem ja comecou e nao pode ser alterada.",
      ROUTE_BLOCKED_BY_NO_FLY_ZONE: "A rota passa por uma zona de exclusao aerea ativa.",
      NO_VALID_ROUTE_AVAILABLE: "Nao existe rota valida para contornar as zonas de exclusao aerea.",
      DRONE_RANGE_EXCEEDED: "A rota excede o alcance maximo do drone.",
      INSUFFICIENT_BATTERY: "A bateria do drone nao cobre o consumo estimado mais a margem de seguranca.",
      NOT_FOUND: "O registro solicitado nao foi encontrado."
    };
    return (error.code && errors[error.code]) || error.message || "Nao foi possivel processar a operacao.";
  }

  return "Nao foi possivel conectar ao servidor.";
}
