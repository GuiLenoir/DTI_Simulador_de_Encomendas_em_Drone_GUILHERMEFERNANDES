import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { ReportsPage } from "./ReportsPage";
import { getDrones } from "../services/dronesApi";
import { getReport } from "../services/reportsApi";
import type { ReportResponse } from "../types/api";

vi.mock("../services/dronesApi", () => ({
  getDrones: vi.fn()
}));

vi.mock("../services/reportsApi", () => ({
  getReport: vi.fn()
}));

const report: ReportResponse = {
  summary: {
    completedDeliveries: 3,
    averageDeliverySeconds: 120
  },
  mostEfficientDrone: {
    droneId: 1,
    droneCode: "DRN-001",
    completedDeliveries: 3,
    totalTransportedWeightKg: 6,
    totalDistanceKm: 18,
    totalBatteryConsumedPercentagePoints: 27,
    efficiencyScore: 0.2
  },
  map: {
    displayedDeliveries: 3,
    usedDrones: 2,
    totalDistanceKm: 18,
    journeys: [
      {
        id: "trip-1",
        tripId: 1,
        deliveryId: null,
        droneId: 1,
        droneCode: "DRN-001",
        completedAtUtc: "2026-07-26T12:00:00Z",
        distanceKm: 10,
        points: [
          { sequence: 0, type: "Base", orderId: null, orderCode: null, priority: null, weightKg: null, x: 0, y: 0, completedAtUtc: null },
          { sequence: 1, type: "Delivery", orderId: 201, orderCode: "PED-201", priority: "High", weightKg: 2, x: 2, y: 1, completedAtUtc: "2026-07-26T12:00:10Z" },
          { sequence: 2, type: "Delivery", orderId: 202, orderCode: "PED-202", priority: "Medium", weightKg: 3, x: 3, y: 2, completedAtUtc: "2026-07-26T12:00:20Z" }
        ]
      },
      {
        id: "delivery-2",
        tripId: null,
        deliveryId: 2,
        droneId: 2,
        droneCode: "DRN-002",
        completedAtUtc: "2026-07-26T12:10:00Z",
        distanceKm: 8,
        points: [
          { sequence: 0, type: "Base", orderId: null, orderCode: null, priority: null, weightKg: null, x: 0, y: 0, completedAtUtc: null },
          { sequence: 1, type: "Delivery", orderId: 203, orderCode: "PED-203", priority: "Low", weightKg: 1, x: -2, y: 2, completedAtUtc: "2026-07-26T12:10:10Z" }
        ]
      }
    ]
  }
};

describe("ReportsPage", () => {
  it("renders report data and selects a journey route from a map point", async () => {
    vi.mocked(getReport).mockResolvedValue(report);
    vi.mocked(getDrones).mockResolvedValue([
      {
        id: 1,
        code: "DRN-001",
        name: "Drone 1",
        maxPackageWeightKg: 10,
        maxRangeKm: 100,
        batteryLevelPercent: 100,
        currentX: 0,
        currentY: 0,
        status: "Idle",
        averageSpeedKmPerHour: 60,
        batteryConsumptionPercentagePerKm: 2.5,
        batterySafetyMarginPercentagePoints: 5,
        notes: null,
        isActive: true,
        hasExecutingTrip: false,
        hasPlannedTrips: false,
        chargingStartedAtUtc: null,
        chargingCompletedAtUtc: null,
        chargingProgressPercentage: 0,
        createdAt: "2026-07-26T10:00:00Z",
        updatedAt: "2026-07-26T10:00:00Z"
      }
    ]);
    const user = userEvent.setup();

    const { container } = render(<ReportsPage />);

    expect(await screen.findByText("Drone mais eficiente")).toBeInTheDocument();
    expect(screen.getAllByText("DRN-001").length).toBeGreaterThan(0);
    expect(screen.getByText("Entregas exibidas: 3 | Drones utilizados: 2")).toBeInTheDocument();
    expect(container.querySelector(".report-route-line")).not.toBeInTheDocument();

    await user.click(screen.getByText("Viagem #1"));

    expect(screen.getByText("Rota selecionada em destaque.")).toBeInTheDocument();
    expect(container.querySelector(".report-route-line.selected")).toBeInTheDocument();
    expect(container.querySelector(".report-return-line.selected")).toBeInTheDocument();

    await user.click(screen.getByText("Limpar selecao"));
    expect(screen.getByText("Selecione uma viagem para visualizar sua rota.")).toBeInTheDocument();

    await user.click(screen.getByLabelText("Mostrar todas as rotas"));
    expect(container.querySelectorAll(".report-route-line")).toHaveLength(2);
  });

  it("applies filters and refreshes the report", async () => {
    vi.mocked(getReport).mockResolvedValue(report);
    vi.mocked(getDrones).mockResolvedValue([]);
    const user = userEvent.setup();

    render(<ReportsPage />);

    await screen.findByText("Mapa das entregas");
    await user.selectOptions(screen.getByLabelText("Prioridade"), "High");

    await waitFor(() => {
      expect(getReport).toHaveBeenLastCalledWith(expect.objectContaining({ priority: "High" }));
    });
  });
});
