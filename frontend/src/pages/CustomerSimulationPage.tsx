import { FormEvent, useEffect, useMemo, useState } from "react";
import { EmptyState, ErrorState, LoadingState } from "../components/PageState";
import { ApiError } from "../services/apiClient";
import { createCustomerOrder, getCustomerTracking } from "../services/customerSimulationApi";
import type { CustomerOrderRequest, CustomerRoutePointResponse, CustomerTrackingResponse, OrderPriority } from "../types/api";
import { formatDateTime, formatDecimal } from "../utils/formatters";
import { getPriorityLabel, priorityLabels } from "../utils/labels";

const initialForm: CustomerOrderRequest = {
  customerName: "",
  packageDescription: "",
  packageWeightKg: 1,
  destinationX: 0,
  destinationY: 0,
  priority: "Medium"
};

export function CustomerSimulationPage() {
  const storedOrderId = Number(localStorage.getItem("customerSimulationOrderId") ?? 0);
  const [form, setForm] = useState<CustomerOrderRequest>(initialForm);
  const [trackedOrderId, setTrackedOrderId] = useState<number | null>(storedOrderId || null);
  const [tracking, setTracking] = useState<CustomerTrackingResponse | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [isLoading, setIsLoading] = useState(Boolean(storedOrderId));
  const [error, setError] = useState<string | null>(null);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsSaving(true);
    setError(null);
    try {
      const created = await createCustomerOrder(form);
      localStorage.setItem("customerSimulationOrderId", String(created.orderId));
      setTrackedOrderId(created.orderId);
      setForm(initialForm);
    } catch (err) {
      setError(getFriendlyError(err));
    } finally {
      setIsSaving(false);
    }
  }

  useEffect(() => {
    if (!trackedOrderId) {
      return;
    }

    let cancelled = false;
    async function load() {
      try {
        const result = await getCustomerTracking(trackedOrderId!);
        if (!cancelled) {
          setTracking(result);
          setError(null);
        }
      } catch (err) {
        if (!cancelled) setError(getFriendlyError(err));
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    }

    void load();
    const timer = window.setInterval(() => void load(), 2500);
    return () => {
      cancelled = true;
      window.clearInterval(timer);
    };
  }, [trackedOrderId]);

  if (!trackedOrderId) {
    return (
      <section className="panel form-panel customer-panel">
        <div className="panel-heading">
          <div>
            <h3>Fazer pedido</h3>
            <span>1 unidade no plano representa 1 quadra nesta simulação.</span>
          </div>
        </div>
        {error && <ErrorState message={error} />}
        <form className="customer-form" onSubmit={submit}>
          <label>
            Nome
            <input required value={form.customerName} onChange={(event) => setForm({ ...form, customerName: event.target.value })} />
          </label>
          <label>
            Descrição do pacote
            <input value={form.packageDescription ?? ""} onChange={(event) => setForm({ ...form, packageDescription: event.target.value })} />
          </label>
          <div className="field-row">
            <label>
              Peso
              <input min="0.01" step="0.01" type="number" value={form.packageWeightKg} onChange={(event) => setForm({ ...form, packageWeightKg: Number(event.target.value) })} />
            </label>
            <label>
              Prioridade
              <select value={form.priority} onChange={(event) => setForm({ ...form, priority: event.target.value as OrderPriority })}>
                {Object.entries(priorityLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
              </select>
            </label>
          </div>
          <div className="field-row">
            <label>
              Coordenada X
              <input step="0.01" type="number" value={form.destinationX} onChange={(event) => setForm({ ...form, destinationX: Number(event.target.value) })} />
            </label>
            <label>
              Coordenada Y
              <input step="0.01" type="number" value={form.destinationY} onChange={(event) => setForm({ ...form, destinationY: Number(event.target.value) })} />
            </label>
          </div>
          <button className="primary-action" type="submit" disabled={isSaving}>{isSaving ? "Enviando..." : "Fazer pedido"}</button>
        </form>
      </section>
    );
  }

  return (
    <section className="dashboard-layout">
      {error && <ErrorState message={error} />}
      {isLoading ? (
        <LoadingState message="Carregando acompanhamento..." />
      ) : tracking ? (
        <>
          <section className="panel table-panel customer-tracking">
            <div className="panel-heading">
              <div>
                <h3>{tracking.friendlyStatus}</h3>
                <span>{tracking.feedbackMessage}</span>
              </div>
              <button
                className="secondary-action"
                type="button"
                onClick={() => {
                  localStorage.removeItem("customerSimulationOrderId");
                  setTrackedOrderId(null);
                  setTracking(null);
                }}
              >
                Novo pedido
              </button>
            </div>
            <div className="detail-grid">
              <Detail label="Pedido" value={tracking.orderCode} />
              <Detail label="Prioridade" value={getPriorityLabel(tracking.priority)} />
              <Detail label="Peso" value={`${formatDecimal(tracking.weightKg)} kg`} />
              <Detail label="Drone" value={tracking.droneCode ?? "Aguardando"} />
              <Detail label="Previsão de recebimento" value={tracking.estimatedCompletionAtUtc ? formatDateTime(tracking.estimatedCompletionAtUtc) : "Aguardando planejamento"} />
              <Detail label="Distância restante" value={`${formatDecimal(tracking.remainingDistance)} km`} />
            </div>
            <div className="progress-bar"><div style={{ width: `${Math.max(0, Math.min(100, tracking.progressPercentage))}%` }} /></div>
            <p className="progress-copy">{tracking.progressPercentage}% concluído</p>
          </section>
          <section className="panel table-panel wide-panel">
            <div className="panel-heading">
              <h3>Mapa da sua entrega</h3>
              <span>Rota calculada pelo sistema de entregas</span>
            </div>
            <CustomerMap tracking={tracking} />
          </section>
        </>
      ) : (
        <EmptyState message="Nenhum pedido acompanhado." />
      )}
    </section>
  );
}

function CustomerMap({ tracking }: { tracking: CustomerTrackingResponse }) {
  const bounds = useMemo(() => getBounds([...tracking.route, tracking.currentPosition]), [tracking]);
  const routePoints = tracking.route.map((point) => `${toX(point.x, bounds)},${toY(point.y, bounds)}`).join(" ");
  const completed = tracking.internalStatus === "Received" || tracking.internalStatus === "Completed" || tracking.friendlyStatus === "Entrega concluída";

  return (
    <svg className="zone-map customer-map" viewBox="0 0 600 360" role="img" aria-label="Mapa de acompanhamento do cliente">
      <line x1="300" y1="0" x2="300" y2="360" />
      <line x1="0" y1="180" x2="600" y2="180" />
      <polyline className="route-return-line" points={routePoints} />
      {tracking.route.map((point) => (
        <g key={`${point.sequence}-${point.type}`}>
          <title>{`${point.orderCode ?? "Base"}\n(${formatDecimal(point.x)}, ${formatDecimal(point.y)})`}</title>
          <circle
            className={point.type === "CustomerDestination" ? "customer-destination-marker" : "route-point"}
            cx={toX(point.x, bounds)}
            cy={toY(point.y, bounds)}
            r={point.type === "Base" ? 10 : point.type === "CustomerDestination" ? 12 : 7}
          />
          {point.type !== "Base" && <text x={toX(point.x, bounds)} y={toY(point.y, bounds)} className="route-marker-text">{point.sequence}</text>}
        </g>
      ))}
      {!completed && (
        <g className="drone-marker">
          <circle cx={toX(tracking.currentPosition.x, bounds)} cy={toY(tracking.currentPosition.y, bounds)} r="9" />
          <text x={toX(tracking.currentPosition.x, bounds)} y={toY(tracking.currentPosition.y, bounds)} className="route-marker-text">D</text>
        </g>
      )}
    </svg>
  );
}

function Detail({ label, value }: { label: string; value: string }) {
  return <div><span>{label}</span><strong>{value}</strong></div>;
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

function getFriendlyError(error: unknown): string {
  if (error instanceof ApiError) {
    return "Ainda não foi possível definir uma rota segura para sua entrega.";
  }

  return "Não foi possível conectar ao servidor.";
}
