import { useCallback, useEffect, useMemo, useState } from "react";
import type { FormEvent } from "react";
import { ApiError } from "../services/apiClient";
import {
  activateDrone,
  createDrone,
  deactivateDrone,
  getDroneSettings,
  getDrones,
  updateDrone,
  updateDroneSettings
} from "../services/dronesApi";
import type { DroneRequest, DroneResponse, DroneSettingsResponse, DroneStatus } from "../types/api";
import { EmptyState, ErrorState, LoadingState } from "../components/PageState";
import { usePolling } from "../hooks/usePolling";
import { formatDateTime, formatDecimal, formatTime } from "../utils/formatters";
import { getDroneStatusLabel } from "../utils/labels";

const availableStatuses: DroneStatus[] = ["Idle", "Charging", "Maintenance", "Unavailable", "Flying"];
const editableStatuses: DroneStatus[] = ["Idle", "Charging", "Maintenance", "Unavailable", "Flying"];
const blankForm: DroneRequest = {
  code: "",
  name: "",
  maxPackageWeightKg: 1,
  maxRangeKm: 10,
  batteryLevelPercent: 100,
  averageSpeedKmPerHour: 60,
  batteryConsumptionPercentagePerKm: 2.5,
  currentX: 0,
  currentY: 0,
  status: "Idle",
  notes: "",
  isActive: true
};

export function DronePage() {
  const [drones, setDrones] = useState<DroneResponse[]>([]);
  const [settings, setSettings] = useState<DroneSettingsResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState("all");
  const [activeFilter, setActiveFilter] = useState("all");
  const [editingDrone, setEditingDrone] = useState<DroneResponse | null>(null);
  const [detailsDrone, setDetailsDrone] = useState<DroneResponse | null>(null);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const [form, setForm] = useState<DroneRequest>(blankForm);
  const [margin, setMargin] = useState(5);
  const [isSaving, setIsSaving] = useState(false);

  const loadData = useCallback(async () => {
    try {
      const [droneResult, settingsResult] = await Promise.all([getDrones(), getDroneSettings()]);
      setDrones(droneResult);
      setSettings(settingsResult);
      setMargin(settingsResult.batterySafetyMarginPercentagePoints);
      setError(null);
    } catch (err) {
      setError(getFriendlyError(err));
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  usePolling(loadData, isFormOpen || isSettingsOpen ? 0 : 1000);

  const filteredDrones = useMemo(
    () =>
      drones.filter((drone) => {
        const status = String(drone.status);
        const statusMatches = statusFilter === "all" || status === statusFilter;
        const activeMatches =
          activeFilter === "all" ||
          (activeFilter === "active" && drone.isActive) ||
          (activeFilter === "inactive" && !drone.isActive);
        return statusMatches && activeMatches;
      }),
    [activeFilter, drones, statusFilter]
  );

  function openCreate() {
    setEditingDrone(null);
    setForm(blankForm);
    setIsFormOpen(true);
    setError(null);
    setSuccess(null);
  }

  function openEdit(drone: DroneResponse) {
    setEditingDrone(drone);
    setForm(toForm(drone));
    setIsFormOpen(true);
    setError(null);
    setSuccess(null);
  }

  async function saveDrone(event: FormEvent) {
    event.preventDefault();
    setIsSaving(true);
    setError(null);
    try {
      if (editingDrone) {
        await updateDrone(editingDrone.id, form);
        setSuccess("Drone atualizado com sucesso.");
      } else {
        await createDrone(form);
        setSuccess("Drone cadastrado com sucesso.");
      }
      setIsFormOpen(false);
      await loadData();
    } catch (err) {
      setError(getFriendlyError(err));
    } finally {
      setIsSaving(false);
    }
  }

  async function toggleDrone(drone: DroneResponse) {
    if (drone.hasExecutingTrip) {
      setError("Nao e possivel alterar um drone em viagem.");
      return;
    }

    if (drone.isActive && drone.hasPlannedTrips) {
      const confirmed = window.confirm("Este drone possui viagens planejadas. Desativar cancela essas viagens e devolve os pedidos para a fila. Continuar?");
      if (!confirmed) {
        return;
      }
    }

    try {
      if (drone.isActive) {
        await deactivateDrone(drone.id);
        setSuccess("Drone desativado.");
      } else {
        await activateDrone(drone.id);
        setSuccess("Drone reativado.");
      }
      await loadData();
    } catch (err) {
      setError(getFriendlyError(err));
    }
  }

  async function saveSettings(event: FormEvent) {
    event.preventDefault();
    setIsSaving(true);
    setError(null);
    try {
      await updateDroneSettings(margin);
      setSuccess("Configuracoes dos drones atualizadas.");
      setIsSettingsOpen(false);
      await loadData();
    } catch (err) {
      setError(getFriendlyError(err));
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <section className="panel table-panel wide-panel">
      <div className="panel-heading">
        <div>
          <h3>Frota de drones</h3>
          <span>Margem global: {formatDecimal(settings?.batterySafetyMarginPercentagePoints ?? 5)} p.p.</span>
        </div>
        <div className="action-row">
          <button className="secondary-action icon-action" type="button" title="Configuracoes dos drones" onClick={() => setIsSettingsOpen(true)}>
            ⚙
          </button>
          <button className="primary-action compact-action" type="button" onClick={openCreate}>
            Novo drone
          </button>
          <button className="secondary-action" type="button" onClick={() => void loadData()}>
            Atualizar
          </button>
        </div>
      </div>

      {error && <ErrorState message={error} />}
      {success && <div className="state state-success">{success}</div>}

      <div className="filter-grid compact-filters">
        <label>
          Status
          <select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)}>
            <option value="all">Todos</option>
            {availableStatuses.map((status) => (
              <option key={status} value={status}>
                {getDroneStatusLabel(status)}
              </option>
            ))}
          </select>
        </label>
        <label>
          Situacao
          <select value={activeFilter} onChange={(event) => setActiveFilter(event.target.value)}>
            <option value="all">Todos</option>
            <option value="active">Ativos</option>
            <option value="inactive">Inativos</option>
          </select>
        </label>
      </div>

      {isLoading ? (
        <LoadingState message="Carregando drones..." />
      ) : filteredDrones.length === 0 ? (
        <EmptyState message="Nenhum drone encontrado." />
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Codigo</th>
                <th>Nome</th>
                <th>Capacidade</th>
                <th>Alcance</th>
                <th>Bateria</th>
                <th>Velocidade</th>
                <th>Consumo</th>
                <th>Status</th>
                <th>Ativo</th>
                <th>Acoes</th>
              </tr>
            </thead>
            <tbody>
              {filteredDrones.map((drone) => (
                <tr key={drone.id}>
                  <td>{drone.code}</td>
                  <td>{drone.name}</td>
                  <td>{formatDecimal(drone.maxPackageWeightKg)} kg</td>
                  <td>{formatDecimal(drone.maxRangeKm)} km</td>
                  <td>
                    <BatteryMeter value={drone.batteryLevelPercent} />
                  </td>
                  <td>{formatDecimal(drone.averageSpeedKmPerHour)} km/h</td>
                  <td>{formatDecimal(drone.batteryConsumptionPercentagePerKm)} p.p./km</td>
                  <td>
                    <span className="status-pill">{getDroneStatusLabel(drone.status)}</span>
                  </td>
                  <td>{drone.isActive ? "Sim" : "Nao"}</td>
                  <td>
                    <div className="row-action-group">
                      <button className="row-action muted-action" type="button" onClick={() => setDetailsDrone(drone)}>
                        Detalhes
                      </button>
                      <button className="row-action" type="button" onClick={() => openEdit(drone)}>
                        Editar
                      </button>
                      <button className="row-action muted-action" type="button" onClick={() => void toggleDrone(drone)} disabled={drone.hasExecutingTrip}>
                        {drone.isActive ? "Desativar" : "Reativar"}
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {isFormOpen && (
        <DroneFormModal
          drone={editingDrone}
          form={form}
          isSaving={isSaving}
          onChange={setForm}
          onClose={() => setIsFormOpen(false)}
          onSubmit={saveDrone}
        />
      )}

      {detailsDrone && <DroneDetailsModal drone={detailsDrone} onClose={() => setDetailsDrone(null)} />}

      {isSettingsOpen && (
        <div className="modal-backdrop" role="presentation">
          <form className="modal-panel compact-modal" onSubmit={saveSettings}>
            <div className="panel-heading">
              <h3>Configuracoes dos drones</h3>
              <button className="secondary-action" type="button" onClick={() => setIsSettingsOpen(false)}>
                Fechar
              </button>
            </div>
            <label>
              Margem de seguranca global da bateria (pontos percentuais)
              <input min="0" max="30" step="0.1" type="number" value={margin} onChange={(event) => setMargin(Number(event.target.value))} />
            </label>
            <button className="primary-action" type="submit" disabled={isSaving}>
              Salvar configuracoes
            </button>
          </form>
        </div>
      )}
    </section>
  );
}

function DroneFormModal({
  drone,
  form,
  isSaving,
  onChange,
  onClose,
  onSubmit
}: {
  drone: DroneResponse | null;
  form: DroneRequest;
  isSaving: boolean;
  onChange: (form: DroneRequest) => void;
  onClose: () => void;
  onSubmit: (event: FormEvent) => void;
}) {
  const operationalDisabled = Boolean(drone?.hasExecutingTrip);

  return (
    <div className="modal-backdrop" role="presentation">
      <form className="modal-panel" onSubmit={onSubmit}>
        <div className="panel-heading">
          <div>
            <h3>{drone ? "Editar drone" : "Novo drone"}</h3>
            {operationalDisabled && <span>Drone em viagem: apenas nome e observacoes podem ser alterados.</span>}
          </div>
          <button className="secondary-action" type="button" onClick={onClose}>
            Fechar
          </button>
        </div>
        <div className="field-row">
          <TextField label="Codigo" value={form.code} disabled={operationalDisabled} onChange={(value) => onChange({ ...form, code: value })} />
          <TextField label="Nome" value={form.name} onChange={(value) => onChange({ ...form, name: value })} />
        </div>
        <div className="field-row">
          <NumberField label="Capacidade maxima (kg)" value={form.maxPackageWeightKg} disabled={operationalDisabled} onChange={(value) => onChange({ ...form, maxPackageWeightKg: value })} />
          <NumberField label="Alcance maximo (km)" value={form.maxRangeKm} disabled={operationalDisabled} onChange={(value) => onChange({ ...form, maxRangeKm: value })} />
        </div>
        <div className="field-row">
          <NumberField label="Bateria atual (%)" min={0} max={100} value={form.batteryLevelPercent} disabled={operationalDisabled} onChange={(value) => onChange({ ...form, batteryLevelPercent: value })} />
          <NumberField label="Velocidade media (km/h)" value={form.averageSpeedKmPerHour} disabled={operationalDisabled} onChange={(value) => onChange({ ...form, averageSpeedKmPerHour: value })} />
        </div>
        <div className="field-row">
          <NumberField label="Consumo por km (p.p.)" value={form.batteryConsumptionPercentagePerKm} disabled={operationalDisabled} onChange={(value) => onChange({ ...form, batteryConsumptionPercentagePerKm: value })} />
          <label>
            Status operacional
            <select value={form.status} disabled={operationalDisabled} onChange={(event) => onChange({ ...form, status: event.target.value as DroneStatus })}>
              {editableStatuses.map((status) => (
                <option key={status} value={status}>
                  {getDroneStatusLabel(status)}
                </option>
              ))}
            </select>
          </label>
        </div>
        <div className="field-row">
          <NumberField label="Posicao X" value={form.currentX} disabled={operationalDisabled} onChange={(value) => onChange({ ...form, currentX: value })} />
          <NumberField label="Posicao Y" value={form.currentY} disabled={operationalDisabled} onChange={(value) => onChange({ ...form, currentY: value })} />
        </div>
        <label>
          Observacoes
          <textarea rows={3} value={form.notes ?? ""} onChange={(event) => onChange({ ...form, notes: event.target.value })} />
        </label>
        <label className="checkbox-row">
          <input type="checkbox" checked={form.isActive} disabled={operationalDisabled} onChange={(event) => onChange({ ...form, isActive: event.target.checked })} />
          Ativo
        </label>
        <button className="primary-action" type="submit" disabled={isSaving}>
          Salvar drone
        </button>
      </form>
    </div>
  );
}

function DroneDetailsModal({ drone, onClose }: { drone: DroneResponse; onClose: () => void }) {
  return (
    <div className="modal-backdrop" role="presentation">
      <div className="modal-panel compact-modal">
        <div className="panel-heading">
          <h3>Detalhes do drone</h3>
          <button className="secondary-action" type="button" onClick={onClose}>
            Fechar
          </button>
        </div>
        <div className="detail-grid">
          <Detail label="Codigo" value={drone.code} />
          <Detail label="Nome" value={drone.name} />
          <Detail label="Status" value={getDroneStatusLabel(drone.status)} />
          <Detail label="Ativo" value={drone.isActive ? "Sim" : "Nao"} />
          <Detail label="Capacidade" value={`${formatDecimal(drone.maxPackageWeightKg)} kg`} />
          <Detail label="Alcance" value={`${formatDecimal(drone.maxRangeKm)} km`} />
          <Detail label="Bateria" value={`${formatDecimal(drone.batteryLevelPercent)}%`} />
          <Detail label="Margem global" value={`${formatDecimal(drone.batterySafetyMarginPercentagePoints)} p.p.`} />
          <Detail label="Velocidade" value={`${formatDecimal(drone.averageSpeedKmPerHour)} km/h`} />
          <Detail label="Consumo" value={`${formatDecimal(drone.batteryConsumptionPercentagePerKm)} p.p./km`} />
          <Detail label="Posicao" value={`(${formatDecimal(drone.currentX)}, ${formatDecimal(drone.currentY)})`} />
          <Detail label="Atualizado" value={formatDateTime(drone.updatedAt)} />
        </div>
        <div className="state state-muted">
          {drone.notes ? drone.notes : "Sem observacoes registradas."}
          <br />
          {getBatteryStatus(drone)}
        </div>
      </div>
    </div>
  );
}

function Detail({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

function TextField({ label, value, disabled, onChange }: { label: string; value: string; disabled?: boolean; onChange: (value: string) => void }) {
  return (
    <label>
      {label}
      <input value={value} disabled={disabled} onChange={(event) => onChange(event.target.value)} />
    </label>
  );
}

function NumberField({
  label,
  value,
  min,
  max,
  disabled,
  onChange
}: {
  label: string;
  value: number;
  min?: number;
  max?: number;
  disabled?: boolean;
  onChange: (value: number) => void;
}) {
  return (
    <label>
      {label}
      <input min={min} max={max} step="0.1" type="number" value={value} disabled={disabled} onChange={(event) => onChange(Number(event.target.value))} />
    </label>
  );
}

function BatteryMeter({ value }: { value: number }) {
  return (
    <div className="battery" aria-label={`Bateria em ${formatDecimal(value, 0)}%`}>
      <div style={{ width: `${Math.max(0, Math.min(100, value))}%` }} />
      <span>{formatDecimal(value, 0)}%</span>
    </div>
  );
}

function toForm(drone: DroneResponse): DroneRequest {
  return {
    code: drone.code,
    name: drone.name,
    maxPackageWeightKg: drone.maxPackageWeightKg,
    maxRangeKm: drone.maxRangeKm,
    batteryLevelPercent: drone.batteryLevelPercent,
    averageSpeedKmPerHour: drone.averageSpeedKmPerHour,
    batteryConsumptionPercentagePerKm: drone.batteryConsumptionPercentagePerKm,
    currentX: drone.currentX,
    currentY: drone.currentY,
    status: typeof drone.status === "number" ? "Idle" : drone.status,
    notes: drone.notes ?? "",
    isActive: drone.isActive
  };
}

function getBatteryStatus(drone: DroneResponse): string {
  if (drone.chargingCompletedAtUtc && drone.batteryLevelPercent >= 100) {
    return "Bateria carregada.";
  }

  if (drone.status === "Charging" || drone.status === 6) {
    return drone.chargingCompletedAtUtc ? `Recarregando, completa as ${formatTime(drone.chargingCompletedAtUtc)}.` : "Recarregando.";
  }

  return "Bateria pronta para operacao.";
}

function getFriendlyError(error: unknown): string {
  if (error instanceof ApiError) {
    const messages: Record<string, string> = {
      DRONE_CODE_ALREADY_EXISTS: "Ja existe um drone com esse codigo.",
      DRONE_NOT_FOUND: "Drone nao encontrado.",
      DRONE_IS_EXECUTING_TRIP: "Nao e possivel alterar um drone em viagem.",
      DRONE_HAS_PLANNED_TRIPS: "O drone possui viagens planejadas.",
      INVALID_BATTERY_PERCENTAGE: "A bateria deve estar entre 0% e 100%.",
      INVALID_DRONE_CAPACITY: "Capacidade, velocidade e consumo devem ser maiores que zero.",
      INVALID_DRONE_RANGE: "O alcance deve ser maior que zero.",
      INVALID_DRONE_STATUS: "Status do drone invalido.",
      GLOBAL_SAFETY_MARGIN_INVALID: "A margem global deve estar entre 0 e 30 pontos percentuais."
    };
    return error.code && messages[error.code] ? messages[error.code] : "Nao foi possivel processar a operacao.";
  }

  return "Nao foi possivel conectar ao servidor.";
}
