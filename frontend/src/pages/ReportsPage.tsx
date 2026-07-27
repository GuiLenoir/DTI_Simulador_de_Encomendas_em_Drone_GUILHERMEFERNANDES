import { PointerEvent, useEffect, useMemo, useRef, useState } from "react";
import { EmptyState, ErrorState, LoadingState } from "../components/PageState";
import { ApiError } from "../services/apiClient";
import { getDrones } from "../services/dronesApi";
import { getReport } from "../services/reportsApi";
import type { DeliveryMapJourneyResponse, DeliveryMapPointResponse, DroneResponse, OrderPriority, ReportResponse } from "../types/api";
import { formatDateTime, formatDecimal } from "../utils/formatters";
import { getPriorityLabel, priorityLabels } from "../utils/labels";

const colors = ["#2563eb", "#2f6f63", "#d97706", "#7c3aed", "#dc2626", "#0891b2"];

export function ReportsPage() {
  const [period, setPeriod] = useState("all");
  const [droneId, setDroneId] = useState("");
  const [priority, setPriority] = useState("");
  const [showRoutes, setShowRoutes] = useState(false);
  const [report, setReport] = useState<ReportResponse | null>(null);
  const [drones, setDrones] = useState<DroneResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const filters = useMemo(() => {
    const now = new Date();
    const from = period === "today"
      ? new Date(now.getFullYear(), now.getMonth(), now.getDate()).toISOString()
      : period === "7"
        ? new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000).toISOString()
        : period === "30"
          ? new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000).toISOString()
          : undefined;
    return {
      from,
      droneId: droneId ? Number(droneId) : undefined,
      priority: priority ? priority as OrderPriority : undefined
    };
  }, [droneId, period, priority]);

  useEffect(() => {
    async function load() {
      setIsLoading(true);
      try {
        const [reportResult, dronesResult] = await Promise.all([getReport(filters), getDrones()]);
        setReport(reportResult);
        setDrones(dronesResult);
        setError(null);
      } catch (err) {
        setError(getFriendlyError(err));
      } finally {
        setIsLoading(false);
      }
    }
    void load();
  }, [filters]);

  return (
    <section className="dashboard-layout">
      <div className="summary-grid">
        <article className="summary-card">
          <span>Entregas realizadas</span>
          <strong>{report?.summary.completedDeliveries ?? 0}</strong>
        </article>
        <article className="summary-card tooltip-anchor" data-tooltip="Tempo entre o inicio da operacao e a conclusao de cada entrega, ignorando registros incompletos.">
          <span>Tempo medio por entrega</span>
          <strong>{formatDuration(report?.summary.averageDeliverySeconds ?? 0)}</strong>
        </article>
        <article className="summary-card wide-summary">
          <span>Drone mais eficiente</span>
          {report?.mostEfficientDrone ? (
            <strong>{report.mostEfficientDrone.droneCode}</strong>
          ) : (
            <p>Ainda nao ha dados suficientes para calcular.</p>
          )}
        </article>
        <article className="summary-card">
          <span>Distancia exibida</span>
          <strong>{formatDecimal(report?.map.totalDistanceKm ?? 0)} km</strong>
        </article>
      </div>

      {report?.mostEfficientDrone && (
        <section className="panel table-panel wide-panel">
          <div className="panel-heading">
            <h3>Detalhes de eficiencia</h3>
            <span>Formula: (entregas + peso) / (distancia + bateria)</span>
          </div>
          <div className="detail-grid">
            <Detail label="Entregas" value={`${report.mostEfficientDrone.completedDeliveries}`} />
            <Detail label="Peso transportado" value={`${formatDecimal(report.mostEfficientDrone.totalTransportedWeightKg)} kg`} />
            <Detail label="Distancia" value={`${formatDecimal(report.mostEfficientDrone.totalDistanceKm)} km`} />
            <Detail label="Bateria consumida" value={`${formatDecimal(report.mostEfficientDrone.totalBatteryConsumedPercentagePoints)} p.p.`} />
            <Detail label="Indice" value={formatDecimal(report.mostEfficientDrone.efficiencyScore, 4)} />
          </div>
        </section>
      )}

      <section className="panel table-panel wide-panel">
        <div className="panel-heading">
          <h3>Mapa das entregas</h3>
          <span>{report ? `Entregas exibidas: ${report.map.displayedDeliveries} | Drones utilizados: ${report.map.usedDrones}` : "Carregando"}</span>
        </div>
        <div className="filter-grid">
          <label>
            Periodo
            <select value={period} onChange={(event) => setPeriod(event.target.value)}>
              <option value="today">Hoje</option>
              <option value="7">Ultimos 7 dias</option>
              <option value="30">Ultimos 30 dias</option>
              <option value="all">Todo o periodo</option>
            </select>
          </label>
          <label>
            Drone
            <select value={droneId} onChange={(event) => setDroneId(event.target.value)}>
              <option value="">Todos</option>
              {drones.map((drone) => (
                <option key={drone.id} value={drone.id}>{drone.code}</option>
              ))}
            </select>
          </label>
          <label>
            Prioridade
            <select value={priority} onChange={(event) => setPriority(event.target.value)}>
              <option value="">Todas</option>
              {Object.entries(priorityLabels).map(([value, label]) => (
                <option key={value} value={value}>{label}</option>
              ))}
            </select>
          </label>
          <label className="checkbox-row">
            <input type="checkbox" checked={showRoutes} onChange={(event) => setShowRoutes(event.target.checked)} />
            Mostrar todas as rotas
          </label>
        </div>
        {error && <ErrorState message={error} />}
        {isLoading ? (
          <LoadingState message="Carregando relatorios..." />
        ) : !report || report.map.journeys.length === 0 ? (
          <EmptyState message="Nenhuma entrega encontrada para os filtros selecionados." />
        ) : (
          <ReportMap journeys={report.map.journeys} showRoutes={showRoutes} />
        )}
      </section>
    </section>
  );
}

function ReportMap({ journeys, showRoutes }: { journeys: DeliveryMapJourneyResponse[]; showRoutes: boolean }) {
  const [selectedTripId, setSelectedTripId] = useState<string | null>(null);
  const [hoveredTripId, setHoveredTripId] = useState<string | null>(null);
  const [pan, setPan] = useState({ x: 0, y: 0 });
  const [zoom, setZoom] = useState(1);
  const [dragStart, setDragStart] = useState<{ pointerId: number; clientX: number; clientY: number; panX: number; panY: number; moved: boolean } | null>(null);
  const mapFrameRef = useRef<HTMLDivElement | null>(null);
  const journeyColors = useMemo(
    () => new Map(journeys.map((journey, index) => [journey.id, colors[index % colors.length]])),
    [journeys]
  );
  const selectedJourney = journeys.find((journey) => journey.id === selectedTripId) ?? null;
  const points = useMemo(() => journeys.flatMap((journey) => journey.points), [journeys]);
  const bounds = useMemo(() => getBounds(selectedJourney?.points ?? points), [points, selectedJourney]);
  const viewWidth = 600 / zoom;
  const viewHeight = 360 / zoom;
  const selectedPointIds = useMemo(
    () => new Set((selectedJourney?.points ?? []).filter((point) => point.type !== "Base").map((point) => pointKey(selectedJourney!.id, point))),
    [selectedJourney]
  );

  useEffect(() => {
    if (selectedTripId && !journeys.some((journey) => journey.id === selectedTripId)) {
      setSelectedTripId(null);
    }
  }, [journeys, selectedTripId]);

  useEffect(() => {
    setPan({ x: 0, y: 0 });
    setZoom(1);
  }, [bounds.min, bounds.max, selectedTripId]);

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

  function handleMapPointerDown(event: PointerEvent<SVGSVGElement>) {
    event.preventDefault();
    event.currentTarget.setPointerCapture(event.pointerId);
    setDragStart({ pointerId: event.pointerId, clientX: event.clientX, clientY: event.clientY, panX: pan.x, panY: pan.y, moved: false });
  }

  function handleMapPointerMove(event: PointerEvent<SVGSVGElement>) {
    if (!dragStart) {
      return;
    }

    event.preventDefault();
    const deltaX = event.clientX - dragStart.clientX;
    const deltaY = event.clientY - dragStart.clientY;
    const moved = dragStart.moved || Math.abs(deltaX) > 3 || Math.abs(deltaY) > 3;
    setDragStart({ ...dragStart, moved });
    setPan({
      x: dragStart.panX - deltaX / zoom,
      y: dragStart.panY - deltaY / zoom
    });
  }

  function handleMapPointerEnd(event: PointerEvent<SVGSVGElement>) {
    event.preventDefault();
    if (event.currentTarget.hasPointerCapture(dragStart?.pointerId ?? event.pointerId)) {
      event.currentTarget.releasePointerCapture(dragStart?.pointerId ?? event.pointerId);
    }
    setDragStart(null);
  }

  function selectJourney(journeyId: string) {
    if (dragStart?.moved) {
      return;
    }

    setSelectedTripId(selectedTripId === journeyId ? null : journeyId);
  }

  return (
    <div className="report-map-layout">
      <div className="report-map-panel">
        <div className="map-controls report-map-controls" aria-label="Controles do mapa de relatorios">
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
        <div className="report-map-stage" ref={mapFrameRef}>
          <p className="map-hint report-map-hint">
            {selectedJourney ? "Rota selecionada em destaque." : "Selecione uma viagem para visualizar sua rota."}
          </p>
          <span className="map-hint report-drag-hint">Arraste para mover e use o scroll para zoom</span>
          <svg
            className={dragStart ? "zone-map report-map dragging-map" : "zone-map report-map"}
            viewBox={`${pan.x + (600 - viewWidth) / 2} ${pan.y + (360 - viewHeight) / 2} ${viewWidth} ${viewHeight}`}
            role="img"
            aria-label="Mapa das entregas concluidas"
            onPointerDown={handleMapPointerDown}
            onPointerMove={handleMapPointerMove}
            onPointerUp={handleMapPointerEnd}
            onPointerCancel={handleMapPointerEnd}
          >
            <line x1="300" y1="-3000" x2="300" y2="3000" />
            <line x1="-3000" y1="180" x2="3000" y2="180" />
            {showRoutes && journeys.map((journey) => (
              <ReportRoute
                bounds={bounds}
                color={journeyColors.get(journey.id) ?? colors[0]}
                isSelected={journey.id === selectedTripId}
                journey={journey}
                key={`route-${journey.id}`}
                muted={selectedTripId !== null && journey.id !== selectedTripId}
              />
            ))}
            {!showRoutes && selectedJourney && (
              <ReportRoute
                bounds={bounds}
                color={journeyColors.get(selectedJourney.id) ?? colors[0]}
                isSelected
                journey={selectedJourney}
                muted={false}
              />
            )}
            <circle cx={toX(0, bounds)} cy={toY(0, bounds)} r="12" className="route-base-marker" />
            {journeys.map((journey) => {
              const color = journeyColors.get(journey.id) ?? colors[0];
              const isSelected = journey.id === selectedTripId;
              const isHighlighted = isSelected || journey.id === hoveredTripId;
              const isDimmed = selectedTripId !== null ? !isSelected : hoveredTripId !== null && !isHighlighted;
              return journey.points.filter((point) => point.type !== "Base").map((point) => {
                const key = pointKey(journey.id, point);
                const showNumber = selectedPointIds.has(key);
                return (
                  <g
                    className={showNumber ? "report-numbered-stop" : "report-delivery-point"}
                    key={key}
                    opacity={isDimmed ? 0.26 : isHighlighted ? 1 : 0.72}
                    onClick={() => selectJourney(journey.id)}
                    onPointerDown={(event) => event.stopPropagation()}
                  >
                    <title>{`${point.orderCode}\nCoordenada (${formatDecimal(point.x)}, ${formatDecimal(point.y)})\nPrioridade ${point.priority ? getPriorityLabel(point.priority) : "-"}\nDrone ${journey.droneCode}\nViagem ${journey.tripId ?? journey.deliveryId}\nConcluido em ${point.completedAtUtc ? formatDateTime(point.completedAtUtc) : "-"}`}</title>
                    <circle
                      cx={toX(point.x, bounds)}
                      cy={toY(point.y, bounds)}
                      r={showNumber ? 10 : 5.5}
                      style={showNumber ? { fill: color, stroke: "#ffffff" } : { fill: color }}
                    />
                    {showNumber && (
                      <text x={toX(point.x, bounds)} y={toY(point.y, bounds)} className="route-marker-text">
                        {point.sequence}
                      </text>
                    )}
                  </g>
                );
              });
            })}
            <text x={toX(0, bounds) + 18} y={toY(0, bounds) - 18} className="map-label report-base-label">Base</text>
          </svg>
        </div>
      </div>
      <aside className="report-trip-selector">
        <div className="panel-heading compact-heading">
          <h3>Viagens</h3>
          <button className="secondary-action compact-action" type="button" onClick={() => setSelectedTripId(null)} disabled={!selectedTripId}>
            Limpar selecao
          </button>
        </div>
        <div className="report-trip-list">
          {journeys.map((journey) => {
            const color = journeyColors.get(journey.id) ?? colors[0];
            const deliveryCount = journey.points.filter((point) => point.type !== "Base").length;
            const isSelected = journey.id === selectedTripId;
            return (
              <button
                className={isSelected ? "report-trip-item selected" : "report-trip-item"}
                key={journey.id}
                type="button"
                onClick={() => setSelectedTripId(isSelected ? null : journey.id)}
                onMouseEnter={() => setHoveredTripId(journey.id)}
                onMouseLeave={() => setHoveredTripId(null)}
              >
                <span className="route-color-dot" style={{ background: color }} />
                <strong>{journey.tripId ? `Viagem #${journey.tripId}` : `Entrega #${journey.deliveryId}`}</strong>
                <small>{journey.droneCode} - {deliveryCount} entregas - {formatDecimal(journey.distanceKm)} km</small>
              </button>
            );
          })}
        </div>
      </aside>
    </div>
  );
}

function ReportRoute({
  journey,
  bounds,
  color,
  isSelected,
  muted
}: {
  journey: DeliveryMapJourneyResponse;
  bounds: { min: number; max: number };
  color: string;
  isSelected: boolean;
  muted: boolean;
}) {
  const routePoints = journey.points;
  const deliveryPoints = routePoints.filter((point) => point.type !== "Base");
  const base = routePoints.find((point) => point.type === "Base") ?? { x: 0, y: 0 };
  const deliveryLinePoints = [base, ...deliveryPoints].map((point) => `${toX(point.x, bounds)},${toY(point.y, bounds)}`).join(" ");
  const lastDelivery = deliveryPoints[deliveryPoints.length - 1];
  return (
    <g opacity={muted ? 0.18 : isSelected ? 1 : 0.25}>
      <polyline
        className={isSelected ? "report-route-line selected" : "report-route-line"}
        points={deliveryLinePoints}
        style={{ stroke: color }}
      />
      {lastDelivery && (
        <line
          className={isSelected ? "report-return-line selected" : "report-return-line"}
          style={{ stroke: color }}
          x1={toX(lastDelivery.x, bounds)}
          y1={toY(lastDelivery.y, bounds)}
          x2={toX(base.x, bounds)}
          y2={toY(base.y, bounds)}
        />
      )}
    </g>
  );
}

function Detail({ label, value }: { label: string; value: string }) {
  return <div><span>{label}</span><strong>{value}</strong></div>;
}

function formatDuration(seconds: number): string {
  const minutes = Math.floor(seconds / 60);
  const rest = seconds % 60;
  return `${minutes} min ${rest} s`;
}

function getBounds(points: Array<{ x: number; y: number }>) {
  const values = points.flatMap((point) => [point.x, point.y, 0]);
  const max = Math.max(10, ...values.map((value) => Math.abs(value)));
  return { min: -max - 1, max: max + 1 };
}

function toX(value: number, bounds: { min: number; max: number }) {
  return ((value - bounds.min) / (bounds.max - bounds.min)) * 600;
}

function toY(value: number, bounds: { min: number; max: number }) {
  return 360 - ((value - bounds.min) / (bounds.max - bounds.min)) * 360;
}

function pointKey(journeyId: string, point: DeliveryMapPointResponse) {
  return `${journeyId}-${point.orderId ?? point.sequence}`;
}

function getFriendlyError(error: unknown): string {
  return error instanceof ApiError ? "Nao foi possivel carregar os relatorios." : "Nao foi possivel conectar ao servidor.";
}
