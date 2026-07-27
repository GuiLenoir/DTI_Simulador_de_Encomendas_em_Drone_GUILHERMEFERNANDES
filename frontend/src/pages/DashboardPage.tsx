import { useCallback, useState } from "react";
import { EmptyState, ErrorState, LoadingState } from "../components/PageState";
import { usePolling } from "../hooks/usePolling";
import { getDashboard } from "../services/dashboardApi";
import { getUpcomingTrips } from "../services/deliveryPlanningApi";
import type { DashboardResponse, TripResponse, UpcomingTripsResponse } from "../types/api";
import { formatDateTime, formatDecimal, formatTime } from "../utils/formatters";
import { getDroneStatusLabel, getPhaseLabel, getPriorityLabel, getTripStatusLabel } from "../utils/labels";

export function DashboardPage() {
  const [dashboard, setDashboard] = useState<DashboardResponse | null>(null);
  const [upcoming, setUpcoming] = useState<UpcomingTripsResponse | null>(null);
  const [isInitialLoading, setIsInitialLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadDashboard = useCallback(async () => {
    try {
      const [response, upcomingResponse] = await Promise.all([getDashboard(), getUpcomingTrips()]);
      setDashboard(response);
      setUpcoming(upcomingResponse);
      setError(null);
    } catch {
      setError("Nao foi possivel atualizar o painel. Tentando novamente...");
    } finally {
      setIsInitialLoading(false);
    }
  }, []);

  usePolling(loadDashboard, 1000);

  if (isInitialLoading && !dashboard) {
    return <LoadingState message="Atualizando dados..." />;
  }

  return (
    <section className="dashboard-layout">
      {error && <ErrorState message={error} />}

      {dashboard && (
        <>
          <div className="summary-grid">
            <article className="summary-card">
              <span>Entregas concluidas</span>
              <strong>{dashboard.completedDeliveries}</strong>
            </article>
            <article className="summary-card">
              <span>Pedidos pendentes</span>
              <strong>{dashboard.pendingDeliveries}</strong>
            </article>
            <article className="summary-card">
              <span>Pedidos na fila</span>
              <strong>{dashboard.queuedOrders.length}</strong>
            </article>
            <article className="summary-card">
              <span>Atualizado agora</span>
              <strong>{formatTime(dashboard.currentUtc)}</strong>
            </article>
          </div>

          <UpcomingTripSection upcoming={upcoming} hasActiveTrips={dashboard.activeTrips.length > 0} />
          <TripSection title="Viagens ativas" empty="Nenhuma viagem ativa no momento." trips={dashboard.activeTrips} showProgress />

          <section className="panel table-panel wide-panel">
            <div className="panel-heading">
              <h3>Entregas individuais ativas</h3>
              <span>{formatDateTime(dashboard.currentUtc)}</span>
            </div>

            {dashboard.activeDeliveries.length === 0 ? (
              <EmptyState message="Nenhuma entrega individual ativa no momento." />
            ) : (
              <div className="active-delivery-grid">
                {dashboard.activeDeliveries.map((delivery) => (
                  <article className="active-delivery-card" key={delivery.id}>
                    <div className="delivery-card-header">
                      <div>
                        <h3>{delivery.droneCode}</h3>
                        <span>Pedido #{delivery.orderId}</span>
                      </div>
                      <strong>{getPhaseLabel(delivery.currentPhase)}</strong>
                    </div>

                    <div className="progress-bar" aria-label={`Progresso em ${delivery.progressPercentage}%`}>
                      <div style={{ width: `${delivery.progressPercentage}%` }} />
                    </div>
                    <p className="progress-copy">{delivery.progressPercentage}% da entrega</p>
                  </article>
                ))}
              </div>
            )}
          </section>

          <section className="panel table-panel wide-panel">
            <div className="panel-heading">
              <h3>Status dos drones</h3>
              <span>Atualizacao automatica</span>
            </div>
            <div className="drone-grid">
              {dashboard.drones.map((drone) => (
                <article className="drone-card" key={drone.id}>
                  <div className="drone-card-header">
                    <div>
                      <h3>{drone.code}</h3>
                      <span>{getDroneStatusLabel(drone.status)}</span>
                    </div>
                    <div className="battery" aria-label={`Bateria em ${formatDecimal(drone.batteryLevelPercent, 0)}%`}>
                      <div style={{ width: `${Math.max(0, Math.min(100, drone.batteryLevelPercent))}%` }} />
                      <span>{formatDecimal(drone.batteryLevelPercent, 0)}%</span>
                    </div>
                  </div>
                  <dl className="metric-grid">
                    <div>
                      <dt>Associado</dt>
                      <dd>{drone.activeTripId ? `Viagem #${drone.activeTripId}` : drone.activeOrderId ? `#${drone.activeOrderId}` : "Nenhum"}</dd>
                    </div>
                    <div>
                      <dt>Posicao</dt>
                      <dd>
                        ({formatDecimal(drone.currentX)}, {formatDecimal(drone.currentY)})
                      </dd>
                    </div>
                    <div>
                      <dt>Margem de bateria</dt>
                      <dd>{formatDecimal(drone.batterySafetyMarginPercentagePoints)} p.p.</dd>
                    </div>
                    <div>
                      <dt>Recarga</dt>
                      <dd>
                        {drone.chargingCompletedAtUtc
                          ? `${drone.chargingProgressPercentage}% ate ${formatTime(drone.chargingCompletedAtUtc)}`
                          : "Nenhuma"}
                      </dd>
                    </div>
                  </dl>
                </article>
              ))}
            </div>
          </section>
        </>
      )}
    </section>
  );
}

function UpcomingTripSection({ upcoming, hasActiveTrips }: { upcoming: UpcomingTripsResponse | null; hasActiveTrips: boolean }) {
  const trips = upcoming?.upcomingTrips ?? [];
  const unplannedOrders = upcoming?.unplannedOrders ?? [];
  const isEmpty = trips.length === 0 && unplannedOrders.length === 0;

  return (
    <section className="panel table-panel wide-panel">
      <div className="panel-heading">
        <div>
          <h3>Proximas viagens</h3>
          <span>O que sera executado depois</span>
        </div>
        <span>{trips.length} viagens - {unplannedOrders.length} pedidos aguardando</span>
      </div>

      {isEmpty ? (
        <EmptyState message={hasActiveTrips ? "Todos os pedidos planejados ja estao em execucao." : "Nenhuma proxima viagem planejada. Novas viagens aparecerao aqui quando houver pedidos aguardando execucao."} />
      ) : (
        <>
          {trips.length > 0 && (
            <div className="trip-grid">
              {trips.map((trip, index) => (
                <article className="active-delivery-card" key={trip.tripId ?? `projection-${index}`}>
                  <div className="delivery-card-header">
                    <div>
                      <h3>{trip.tripId ? `Viagem #${trip.tripId}` : "Proposta de viagem"}</h3>
                      <span>{trip.droneCode ? `Drone ${trip.droneCode}` : "Drone a definir"}</span>
                    </div>
                    <strong>{trip.waitingReason}</strong>
                  </div>
                  <dl className="metric-grid">
                    <div>
                      <dt>Pedidos</dt>
                      <dd title={trip.orders.map((order) => `${order.orderCode} - ${order.customerName}`).join("\n")}>
                        {trip.orders.map((order) => order.orderCode).join(", ")}
                      </dd>
                    </div>
                    <div>
                      <dt>Prioridades</dt>
                      <dd>{Array.from(new Set(trip.orders.map((order) => getPriorityLabel(order.priority)))).join(", ")}</dd>
                    </div>
                    <div>
                      <dt>Carga</dt>
                      <dd>{formatDecimal(trip.totalWeightKg)} kg / {trip.droneCapacityKg ? `${formatDecimal(trip.droneCapacityKg)} kg` : "-"}</dd>
                    </div>
                    <div>
                      <dt>Ocupacao</dt>
                      <dd>{formatDecimal(trip.capacityUsagePercentage)}%</dd>
                    </div>
                    <div>
                      <dt>Distancia</dt>
                      <dd>{formatDecimal(trip.estimatedDistanceKm)} km</dd>
                    </div>
                    <div>
                      <dt>Consumo</dt>
                      <dd>{formatDecimal(trip.estimatedBatteryConsumptionPercentagePoints)}%</dd>
                    </div>
                    <div>
                      <dt>Margem</dt>
                      <dd>{formatDecimal(trip.batterySafetyMarginPercentagePoints)} p.p.</dd>
                    </div>
                    <div>
                      <dt>Bateria necessaria</dt>
                      <dd>{formatDecimal(trip.minimumRequiredBatteryPercentage)}%</dd>
                    </div>
                    <div>
                      <dt>Inicio previsto</dt>
                      <dd>{trip.blockingTripId ? `apos a Viagem #${trip.blockingTripId}` : trip.estimatedStartAtUtc ? formatTime(trip.estimatedStartAtUtc) : "Indisponivel"}</dd>
                    </div>
                    <div>
                      <dt>Status</dt>
                      <dd>{trip.friendlyStatus}</dd>
                    </div>
                  </dl>
                  <button className="secondary-action card-action" type="button" title="Detalhes ja exibidos no card">
                    Ver planejamento
                  </button>
                </article>
              ))}
            </div>
          )}

          {unplannedOrders.length > 0 && (
            <div className="upcoming-unplanned">
              <div className="panel-heading compact-heading">
                <h3>Pedidos aguardando planejamento</h3>
                <span>{unplannedOrders.length} pedidos</span>
              </div>
              <div className="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>Pedido</th>
                      <th>Peso</th>
                      <th>Prioridade</th>
                      <th>Tempo na fila</th>
                      <th>Motivo</th>
                    </tr>
                  </thead>
                  <tbody>
                    {unplannedOrders.map((order) => (
                      <tr key={order.orderId}>
                        <td>{order.orderCode}</td>
                        <td>{formatDecimal(order.packageWeightKg)} kg</td>
                        <td>{getPriorityLabel(order.priority)}</td>
                        <td>{order.queuedAtUtc ? formatDateTime(order.queuedAtUtc) : "Ainda fora da fila"}</td>
                        <td>{order.waitingReason}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </>
      )}
    </section>
  );
}

function TripSection({
  title,
  empty,
  trips,
  showProgress = false
}: {
  title: string;
  empty: string;
  trips: TripResponse[];
  showProgress?: boolean;
}) {
  return (
    <section className="panel table-panel wide-panel">
      <div className="panel-heading">
        <h3>{title}</h3>
        <span>{trips.length} viagens</span>
      </div>

      {trips.length === 0 ? (
        <EmptyState message={empty} />
      ) : (
        <div className="trip-grid">
          {trips.map((trip) => (
            <article className="active-delivery-card" key={trip.id}>
              <div className="delivery-card-header">
                <div>
                  <h3>Viagem #{trip.id}</h3>
                  <span>Drone {trip.droneCode}</span>
                </div>
                <strong>{showProgress ? getPhaseLabel(trip.currentPhase) : getTripStatusLabel(trip.status)}</strong>
              </div>
              {showProgress && (
                <>
                  <div className="progress-bar" aria-label={`Progresso em ${trip.progressPercentage}%`}>
                    <div style={{ width: `${trip.progressPercentage}%` }} />
                  </div>
                  <p className="progress-copy">{trip.progressPercentage}% da viagem</p>
                </>
              )}
              <dl className="metric-grid">
                <div>
                  <dt>Pedidos</dt>
                  <dd>{trip.orders.map((order) => `#${order.orderId}`).join(", ")}</dd>
                </div>
                <div>
                  <dt>Carga</dt>
                  <dd>{formatDecimal(trip.capacityUsagePercentage)}%</dd>
                </div>
                <div>
                  <dt>Bateria minima</dt>
                  <dd>{formatDecimal(trip.minimumRequiredBatteryPercentage)}%</dd>
                </div>
                <div>
                  <dt>Proxima etapa</dt>
                  <dd>{formatTime(trip.nextPhaseAtUtc)}</dd>
                </div>
              </dl>
            </article>
          ))}
        </div>
      )}
    </section>
  );
}
