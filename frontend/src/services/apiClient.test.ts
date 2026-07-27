import { describe, expect, it, vi } from "vitest";
import { ApiError, apiRequest } from "./apiClient";

describe("apiRequest", () => {
  it("sends JSON requests to the configured API URL", async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ ok: true }), { status: 200 }));
    vi.stubGlobal("fetch", fetchMock);

    const result = await apiRequest<{ ok: boolean }>("/api/orders", {
      method: "POST",
      body: JSON.stringify({ customerName: "Ana" })
    });

    expect(result).toEqual({ ok: true });
    expect(fetchMock).toHaveBeenCalledWith("/api/orders", expect.objectContaining({
      method: "POST",
      headers: expect.objectContaining({ "Content-Type": "application/json" }),
      body: JSON.stringify({ customerName: "Ana" })
    }));
  });

  it("returns undefined for 204 responses", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(null, { status: 204 })));

    await expect(apiRequest<void>("/api/orders/1", { method: "DELETE" })).resolves.toBeUndefined();
  });

  it("throws ApiError with problem details when the API rejects the request", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(JSON.stringify({
      type: "INSUFFICIENT_BATTERY",
      title: "Insufficient battery",
      detail: "Battery is too low."
    }), { status: 422 })));

    await expect(apiRequest("/api/delivery-planning/plan")).rejects.toMatchObject({
      status: 422,
      code: "INSUFFICIENT_BATTERY",
      message: "Battery is too low."
    });
  });
});
