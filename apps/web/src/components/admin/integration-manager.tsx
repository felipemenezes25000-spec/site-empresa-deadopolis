"use client";

import { useEffect, useState } from "react";
import { StatusBadge } from "@/components/ui";

type Integration = { provider: string; state: string; message: string; lastErrorCode: string | null; lastCheckedAt: string };
type ProviderState = { state: string; description?: string };
type Providers = { storage: ProviderState; signature: ProviderState; certificate: ProviderState; timestamp: ProviderState; validation: ProviderState };

export function IntegrationManager() {
  const [items, setItems] = useState<Integration[]>([]);
  const [providers, setProviders] = useState<Providers | null>(null);
  const [state, setState] = useState<"LOADING" | "READY" | "ERROR">("LOADING");

  useEffect(() => {
    const controller = new AbortController();
    void Promise.all([
      fetch("/api/v1/admin/integrations", { signal: controller.signal }),
      fetch("/api/v1/admin/providers", { signal: controller.signal }),
    ]).then(async ([integrationsResponse, providersResponse]) => {
      if (!integrationsResponse.ok) throw new Error("integrations");
      const integrations = await integrationsResponse.json() as Integration[];
      const runtime = providersResponse.ok ? await providersResponse.json() as Providers : null;
      if (controller.signal.aborted) return;
      setItems(integrations);
      setProviders(runtime);
      setState("READY");
    }).catch(() => { if (!controller.signal.aborted) setState("ERROR"); });
    return () => controller.abort();
  }, []);

  if (state === "LOADING") return <div className="admin-panel" aria-busy="true">Carregando estado das integrações…</div>;
  if (state === "ERROR") return <div className="form-message error" role="alert">Não foi possível carregar o estado das integrações.</div>;

  const runtimeRows: Array<[string, ProviderState]> = providers
    ? [["Storage", providers.storage], ["Assinatura digital", providers.signature], ["Certificado", providers.certificate], ["Carimbo do tempo", providers.timestamp], ["Validador", providers.validation]]
    : [];

  return <>
    <section className="admin-panel">
      <h2>Integrações cadastradas</h2>
      <p className="text-muted">Cada estado reflete a configuração real do ambiente. Uma dependência externa sem credencial permanece explicitamente não configurada.</p>
      {items.length === 0
        ? <div className="empty-state"><h3>Nenhuma integração cadastrada</h3><p>Os provedores institucionais aparecem aqui assim que forem registrados.</p></div>
        : <div className="compact-list">{items.map((item) => <div className="compact-item" key={item.provider}>
          <div><strong>{item.provider}</strong><small style={{ display: "block" }}>{item.message}</small>{item.lastErrorCode && <small style={{ display: "block" }}>Último erro: {item.lastErrorCode}</small>}<small style={{ display: "block" }}>Verificado em {formatDate(item.lastCheckedAt)}</small></div>
          <StatusBadge status={item.state} />
        </div>)}</div>}
    </section>

    {providers && <section className="admin-panel">
      <h2>Providers em runtime</h2>
      <div className="table-scroll"><table className="admin-table">
        <thead><tr><th>Provider</th><th>Estado</th><th>Detalhe</th></tr></thead>
        <tbody>{runtimeRows.map(([label, provider]) => <tr key={label}><th scope="row">{label}</th><td><StatusBadge status={provider.state} /></td><td>{provider.description ?? "—"}</td></tr>)}</tbody>
      </table></div>
    </section>}
  </>;
}

function formatDate(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "short" }).format(date);
}
