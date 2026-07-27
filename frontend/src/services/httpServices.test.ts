import { describe, expect, it, vi } from "vitest";
import { createDrone, updateDroneSettings } from "./dronesApi";
import { createOrder, queueOrder, removeOrderFromQueue } from "./ordersApi";
import { getReport } from "./reportsApi";

describe("HTTP services", () => {
  it("sends order creation and queue requests to the expected endpoints", async () => {
    const fetchMock = vi.fn().mockImplementation(() => Promise.resolve(new Response(JSON.stringify({ id: 10 }), { status: 200 })));
    vi.stubGlobal("fetch", fetchMock);

    await createOrder({
      customerName: "Cliente",
      destinationX: 3,
      destinationY: 4,
      packageWeightKg: 2,
      priority: "High"
    });
    await queueOrder(10);
    await removeOrderFromQueue(10);

    expect(fetchMock).toHaveBeenNthCalledWith(1, "/api/orders", expect.objectContaining({ method: "POST" }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, "/api/orders/10/queue", expect.objectContaining({ method: "POST" }));
    expect(fetchMock).toHaveBeenNthCalledWith(3, "/api/orders/10/queue", expect.objectContaining({ method: "DELETE" }));
  });

  it("sends drone creation and settings update bodies", async () => {
    const fetchMock = vi.fn().mockImplementation(() => Promise.resolve(new Response(JSON.stringify({ id: 1 }), { status: 200 })));
    vi.stubGlobal("fetch", fetchMock);

    await createDrone({
      code: "DRN-100",
      name: "Drone 100",
      maxPackageWeightKg: 5,
      maxRangeKm: 30,
      batteryLevelPercent: 90,
      averageSpeedKmPerHour: 60,
      batteryConsumptionPercentagePerKm: 2.5,
      currentX: 0,
      currentY: 0,
      status: "Idle",
      notes: null,
      isActive: true
    });
    await updateDroneSettings(8);

    expect(fetchMock).toHaveBeenNthCalledWith(1, "/api/drones", expect.objectContaining({
      method: "POST",
      body: expect.stringContaining("DRN-100")
    }));
    expect(fetchMock).toHaveBeenNthCalledWith(2, "/api/drone-settings", expect.objectContaining({
      method: "PUT",
      body: JSON.stringify({ batterySafetyMarginPercentagePoints: 8 })
    }));
  });

  it("builds report query parameters", async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ summary: {}, map: { journeys: [] } }), { status: 200 }));
    vi.stubGlobal("fetch", fetchMock);

    await getReport({
      from: "2026-07-01T00:00:00.000Z",
      to: "2026-07-26T00:00:00.000Z",
      droneId: 3,
      priority: "High"
    });

    expect(fetchMock).toHaveBeenCalledWith(
      "/api/reports?from=2026-07-01T00%3A00%3A00.000Z&to=2026-07-26T00%3A00%3A00.000Z&droneId=3&priority=High",
      expect.any(Object)
    );
  });
});
