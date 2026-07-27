import { FormEvent, useEffect, useMemo, useState } from "react";
import { EmptyState, ErrorState, LoadingState } from "../components/PageState";
import { ApiError } from "../services/apiClient";
import { createNoFlyZone, deleteNoFlyZone, getNoFlyZones, updateNoFlyZone } from "../services/noFlyZonesApi";
import type { NoFlyZonePoint, NoFlyZoneRequest, NoFlyZoneResponse } from "../types/api";

type FormState = {
  id?: number;
  name: string;
  isActive: boolean;
  pointsText: string;
};

const initialForm: FormState = {
  name: "",
  isActive: true,
  pointsText: "4,2\n7,2\n7,6\n4,6"
};

export function NoFlyZonePage() {
  const [zones, setZones] = useState<NoFlyZoneResponse[]>([]);
  const [form, setForm] = useState<FormState>(initialForm);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const bounds = useMemo(() => getBounds(zones), [zones]);

  async function loadZones() {
    setIsLoading(true);
    setError(null);
    try {
      setZones(await getNoFlyZones());
    } catch (err) {
      setError(getFriendlyError(err));
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    void loadZones();
  }, []);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setSuccess(null);
    setIsSaving(true);

    try {
      const request = toRequest(form);
      if (form.id) {
        await updateNoFlyZone(form.id, request);
        setSuccess("Zona atualizada com sucesso.");
      } else {
        await createNoFlyZone(request);
        setSuccess("Zona criada com sucesso.");
      }
      setForm(initialForm);
      await loadZones();
    } catch (err) {
      setError(getFriendlyError(err));
    } finally {
      setIsSaving(false);
    }
  }

  async function handleDelete(id: number) {
    setError(null);
    setSuccess(null);
    try {
      await deleteNoFlyZone(id);
      setSuccess("Zona excluida com sucesso.");
      await loadZones();
    } catch (err) {
      setError(getFriendlyError(err));
    }
  }

  async function handleToggle(zone: NoFlyZoneResponse) {
    setError(null);
    setSuccess(null);
    try {
      await updateNoFlyZone(zone.id, { name: zone.name, isActive: !zone.isActive, points: zone.points });
      setSuccess(zone.isActive ? "Zona desativada." : "Zona ativada.");
      await loadZones();
    } catch (err) {
      setError(getFriendlyError(err));
    }
  }

  return (
    <section className="page-grid no-fly-zone-layout">
      <form className="panel form-panel" onSubmit={handleSubmit}>
        <div className="panel-heading">
          <h3>Zonas de Exclusao Aerea</h3>
          <span>{form.id ? "Editar zona" : "Nova zona"}</span>
        </div>

        <label>
          Nome
          <input value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} required />
        </label>

        <label className="checkbox-row">
          <input
            checked={form.isActive}
            type="checkbox"
            onChange={(event) => setForm({ ...form, isActive: event.target.checked })}
          />
          Zona ativa
        </label>

        <label>
          Pontos do poligono
          <textarea
            value={form.pointsText}
            onChange={(event) => setForm({ ...form, pointsText: event.target.value })}
            rows={6}
            placeholder="x,y por linha"
          />
        </label>

        {error && <ErrorState message={error} />}
        {success && <div className="state state-success">{success}</div>}

        <div className="action-row">
          <button className="primary-action" disabled={isSaving} type="submit">
            {isSaving ? "Salvando..." : form.id ? "Salvar alteracoes" : "Criar zona"}
          </button>
          {form.id && (
            <button className="secondary-action" type="button" onClick={() => setForm(initialForm)}>
              Cancelar edicao
            </button>
          )}
        </div>
      </form>

      <section className="panel table-panel">
        <div className="panel-heading">
          <h3>Zonas cadastradas</h3>
          <button className="secondary-action" type="button" onClick={() => void loadZones()}>
            Atualizar
          </button>
        </div>

        {isLoading ? (
          <LoadingState message="Carregando zonas..." />
        ) : zones.length === 0 ? (
          <EmptyState message="Nenhuma zona cadastrada." />
        ) : (
          <div className="zone-list">
            {zones.map((zone) => (
              <article className="zone-item" key={zone.id}>
                <div>
                  <h4>{zone.name}</h4>
                  <span>{zone.isActive ? "Ativa" : "Inativa"} - {zone.points.length} pontos</span>
                  <p className="zone-points">{zone.points.map((point, index) => `${index + 1}: (${point.x}, ${point.y})`).join("  ")}</p>
                </div>
                <div className="row-action-group">
                  <button className="row-action muted-action" type="button" onClick={() => setForm(toForm(zone))}>
                    Editar
                  </button>
                  <button className="row-action muted-action" type="button" onClick={() => void handleToggle(zone)}>
                    {zone.isActive ? "Desativar" : "Ativar"}
                  </button>
                  <button className="row-action" type="button" onClick={() => void handleDelete(zone.id)}>
                    Excluir
                  </button>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>

      <section className="panel table-panel routes-panel">
        <div className="panel-heading">
          <h3>Mapa da malha</h3>
          <span>Poligonos ativos aparecem em destaque</span>
        </div>
        <svg className="zone-map" viewBox="0 0 600 360" role="img" aria-label="Mapa das zonas de exclusao aerea">
          <line x1="300" y1="0" x2="300" y2="360" />
          <line x1="0" y1="180" x2="600" y2="180" />
          <circle cx={toScreenX(0, bounds)} cy={toScreenY(0, bounds)} r="5" className="base-point" />
          {zones.map((zone) => (
            <g key={zone.id}>
              <polygon
                className={zone.isActive ? "zone-polygon active-zone" : "zone-polygon"}
                points={zone.points.map((point) => `${toScreenX(point.x, bounds)},${toScreenY(point.y, bounds)}`).join(" ")}
              />
              {zone.points.map((point, index) => (
                <g key={`${zone.id}-${index}`}>
                  <circle cx={toScreenX(point.x, bounds)} cy={toScreenY(point.y, bounds)} r="5" className="zone-point" />
                  <title>
                    Ponto {index + 1}: ({point.x}, {point.y})
                  </title>
                  <text
                    x={toScreenX(point.x, bounds) + getPointLabelOffset(index).x}
                    y={toScreenY(point.y, bounds) + getPointLabelOffset(index).y}
                    className="map-label zone-map-label"
                    textAnchor={getPointLabelOffset(index).anchor}
                  >
                    {index + 1}
                  </text>
                </g>
              ))}
            </g>
          ))}
        </svg>
      </section>
    </section>
  );
}

function toRequest(form: FormState): NoFlyZoneRequest {
  return {
    name: form.name,
    isActive: form.isActive,
    points: parsePoints(form.pointsText)
  };
}

function toForm(zone: NoFlyZoneResponse): FormState {
  return {
    id: zone.id,
    name: zone.name,
    isActive: zone.isActive,
    pointsText: zone.points.map((point) => `${point.x},${point.y}`).join("\n")
  };
}

function parsePoints(text: string): NoFlyZonePoint[] {
  const points = text
    .split("\n")
    .map((line) => line.trim())
    .filter(Boolean)
    .map((line) => {
      const [x, y] = line.split(",").map((value) => Number(value.trim()));
      if (!Number.isFinite(x) || !Number.isFinite(y)) {
        throw new Error("POINTS_INVALID");
      }
      return { x, y };
    });

  if (points.length < 3) {
    throw new Error("POINTS_REQUIRED");
  }

  return points;
}

function getBounds(zones: NoFlyZoneResponse[]) {
  const values = zones.flatMap((zone) => zone.points.flatMap((point) => [point.x, point.y]));
  const max = Math.max(10, ...values.map((value) => Math.abs(value)));
  return { min: -max - 1, max: max + 1 };
}

function toScreenX(value: number, bounds: { min: number; max: number }): number {
  return ((value - bounds.min) / (bounds.max - bounds.min)) * 600;
}

function toScreenY(value: number, bounds: { min: number; max: number }): number {
  return 360 - ((value - bounds.min) / (bounds.max - bounds.min)) * 360;
}

function getPointLabelOffset(index: number): { x: number; y: number; anchor: "start" | "middle" | "end" } {
  const offsets = [
    { x: 0, y: -12, anchor: "middle" as const },
    { x: 12, y: 2, anchor: "start" as const },
    { x: 0, y: 16, anchor: "middle" as const },
    { x: -12, y: 2, anchor: "end" as const }
  ];

  return offsets[index % offsets.length];
}

function getFriendlyError(error: unknown): string {
  if (error instanceof Error && error.message === "POINTS_INVALID") {
    return "Informe os pontos no formato x,y, um ponto por linha.";
  }

  if (error instanceof Error && error.message === "POINTS_REQUIRED") {
    return "Informe pelo menos tres pontos para formar o poligono.";
  }

  if (error instanceof ApiError) {
    const errors: Record<string, string> = {
      NO_FLY_ZONE_NAME_REQUIRED: "Informe o nome da zona.",
      NO_FLY_ZONE_REQUIRES_POLYGON: "A zona precisa ter pelo menos tres pontos.",
      NOT_FOUND: "A zona solicitada nao foi encontrada."
    };
    return (error.code && errors[error.code]) || error.message || "Nao foi possivel processar a operacao.";
  }

  return "Nao foi possivel conectar ao servidor.";
}
