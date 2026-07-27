import { useMemo, useState } from "react";
import { DashboardPage } from "./pages/DashboardPage";
import { CustomerSimulationPage } from "./pages/CustomerSimulationPage";
import { DronePage } from "./pages/DronePage";
import { NoFlyZonePage } from "./pages/NoFlyZonePage";
import { OrderPage } from "./pages/OrderPage";
import { ReportsPage } from "./pages/ReportsPage";

type Page = "dashboard" | "orders" | "drones" | "noFlyZones" | "reports" | "customer";

export function App() {
  const [activePage, setActivePage] = useState<Page>("dashboard");
  const title = useMemo(() => {
    const titles: Record<Page, string> = {
      dashboard: "Painel em tempo real",
      orders: "Pedidos de entrega",
      drones: "Drones",
      noFlyZones: "Zonas de Exclusao Aerea",
      reports: "Relatorios",
      customer: "Cliente Simulado"
    };
    return titles[activePage];
  }, [activePage]);

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div>
          <p className="eyebrow">Simulador</p>
          <h1>Entregas por Drone</h1>
        </div>
        <nav className="nav-tabs" aria-label="Navegação principal">
          <button
            className={activePage === "dashboard" ? "active" : ""}
            type="button"
            onClick={() => setActivePage("dashboard")}
          >
            Painel
          </button>
          <button
            className={activePage === "orders" ? "active" : ""}
            type="button"
            onClick={() => setActivePage("orders")}
          >
            Pedidos
          </button>
          <button
            className={activePage === "drones" ? "active" : ""}
            type="button"
            onClick={() => setActivePage("drones")}
          >
            Drones
          </button>
          <button
            className={activePage === "noFlyZones" ? "active" : ""}
            type="button"
            onClick={() => setActivePage("noFlyZones")}
          >
            Zonas
          </button>
          <button
            className={activePage === "reports" ? "active" : ""}
            type="button"
            onClick={() => setActivePage("reports")}
          >
            Relatorios
          </button>
          <button
            className={activePage === "customer" ? "active" : ""}
            type="button"
            onClick={() => setActivePage("customer")}
          >
            Cliente Simulado
          </button>
        </nav>
      </aside>

      <main className="content">
        <header className="page-header">
          <p className="eyebrow">Operação</p>
          <h2>{title}</h2>
        </header>
        {activePage === "dashboard" && <DashboardPage />}
        {activePage === "orders" && <OrderPage />}
        {activePage === "drones" && <DronePage />}
        {activePage === "noFlyZones" && <NoFlyZonePage />}
        {activePage === "reports" && <ReportsPage />}
        {activePage === "customer" && <CustomerSimulationPage />}
      </main>
    </div>
  );
}
