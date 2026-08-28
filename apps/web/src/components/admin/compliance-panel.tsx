"use client";

import { useEffect, useState } from "react";
import { AuditViewer, StatusBadge, type AuditViewerItem } from "@/components/ui";

type StateDetail = { state: string; description?: string; detail?: string | null };
type Compliance = {
  generatedAt: string;
  readiness: { state: string; databaseReady: boolean };
  providers: {
    storage: StateDetail;
    digitalSignature: StateDetail;
    timestamp: StateDetail;
    institutionalEmail: StateDetail;
    malwareScanner: StateDetail;
    mediaVariants: { webp: StateDetail; avif: StateDetail };
  };
  evidence: {
    links: { total: number; degraded: number };
    migration: { total: number };
    backups: { total: number; restoreTested: number };
    gazette: { signatures: number; publications: number; corrections: number };
  };
  integrations: Array<{ provider: string; state: string; message: string; lastCheckedAt: string }>;
  externalDependencies: Array<{ name: string; state: string; requirement: string }>;
};

export function CompliancePanel() {
  const [compliance, setCompliance] = useState<Compliance | null>(null);
  const [audit, setAudit] = useState<AuditViewerItem[]>([]);
  const [error, setError] = useState("");

  useEffect(() => {
    const controller = new AbortController();
    void Promise.all([
      fetch("/api/v1/admin/compliance", { signal: controller.signal }),
      fetch("/api/v1/admin/audit", { signal: controller.signal }),
    ]).then(async ([complianceResponse, auditResponse]) => {
      if (!complianceResponse.ok) throw new Error(await errorText(complianceResponse));
      if (!auditResponse.ok) throw new Error(await errorText(auditResponse));
      const [complianceData, auditData] = await Promise.all([
        complianceResponse.json() as Promise<Compliance>,
        auditResponse.json() as Promise<AuditViewerItem[]>,
      ]);
      if (!controller.signal.aborted) {
        setCompliance(complianceData);
        setAudit(auditData);
      }
    }).catch((reason) => {
      if (!controller.signal.aborted) setError(reason instanceof Error ? reason.message : "Não foi possível carregar as evidências.");
    });
    return () => controller.abort();
  }, []);

  if (error) return <div className="form-message error" role="alert">{error}</div>;
  if (!compliance) return <div className="admin-panel" aria-busy="true">Carregando evidências de compliance…</div>;

  const providers = [
    ["Storage", compliance.providers.storage],
    ["Assinatura digital", compliance.providers.digitalSignature],
    ["Carimbo do tempo", compliance.providers.timestamp],
    ["E-mail institucional", compliance.providers.institutionalEmail],
    ["Scanner antimalware", compliance.providers.malwareScanner],
    ["WebP", compliance.providers.mediaVariants.webp],
    ["AVIF", compliance.providers.mediaVariants.avif],
  ] as const;

  const readiness = summarizeReadiness(providers.map(([, provider]) => provider.state));

  return <>
    <div className="admin-grid">
      <Metric title="Banco de dados" value={compliance.readiness.databaseReady ? "Pronto" : "Indisponível"} />
      <Metric title="Links degradados" value={`${compliance.evidence.links.degraded} de ${compliance.evidence.links.total}`} />
      <Metric title="Evidências de migração" value={String(compliance.evidence.migration.total)} />
      <Metric title="Restores evidenciados" value={`${compliance.evidence.backups.restoreTested} de ${compliance.evidence.backups.total}`} />
    </div>

    <section className="admin-panel">
      <div className="admin-heading"><div><h2>Prontidão para produção</h2><p>Contagem derivada dos estados reais do runtime. Demonstração e ausência de provider nunca entram como pronto.</p></div></div>
      <div className="admin-grid">
        <Metric title="Pronto" value={String(readiness.ready)} />
        <Metric title="Demonstração" value={String(readiness.demo)} />
        <Metric title="Aguardando configuração" value={String(readiness.pending)} />
        <Metric title="Atenção" value={String(readiness.attention)} />
      </div>
    </section>

    <section className="admin-panel">
      <div className="admin-heading"><div><h2>Capacidades do runtime</h2><p>Estados obtidos do processo em execução. Demonstração e ausência de provider permanecem explícitas.</p></div><small>Atualizado em {formatDate(compliance.generatedAt)}</small></div>
      <div className="compact-list">{providers.map(([name, provider]) => <div className="compact-item" key={name}><div><strong>{name}</strong><small style={{ display: "block" }}>{provider.description ?? provider.detail ?? "Sem detalhe fornecido."}</small></div><StatusBadge status={provider.state} /></div>)}</div>
    </section>

    <section className="admin-panel">
      <h2>Evidências persistidas</h2>
      <div className="admin-grid">
        <Metric title="Assinaturas do Diário" value={String(compliance.evidence.gazette.signatures)} />
        <Metric title="Publicações do Diário" value={String(compliance.evidence.gazette.publications)} />
        <Metric title="Correções vinculadas" value={String(compliance.evidence.gazette.corrections)} />
        <Metric title="Backups registrados" value={String(compliance.evidence.backups.total)} />
      </div>
    </section>

    <section className="admin-panel">
      <h2>Dependências externas para produção</h2>
      <div className="compact-list">{compliance.externalDependencies.map((dependency) => <div className="compact-item" key={dependency.name}><div><strong>{dependency.name}</strong><small style={{ display: "block" }}>{dependency.requirement}</small></div><StatusBadge status={dependency.state} /></div>)}</div>
    </section>

    <section className="admin-panel">
      <h2>Integrações cadastradas</h2>
      {compliance.integrations.length === 0 ? <div className="empty-state"><p>Nenhum estado de integração cadastrado.</p></div> : <div className="compact-list">{compliance.integrations.map((integration) => <div className="compact-item" key={integration.provider}><div><strong>{integration.provider}</strong><small style={{ display: "block" }}>{integration.message}</small></div><StatusBadge status={integration.state} /></div>)}</div>}
    </section>

    <section className="admin-panel"><h2>Últimos eventos de auditoria</h2><AuditViewer items={audit.slice(0, 20)} /></section>
  </>;
}

function Metric({ title, value }: { title: string; value: string }) {
  return <div className="metric-card"><span>{title}</span><strong>{value}</strong></div>;
}

// A API resume readiness a partir do banco de dados apenas, então "READY" convive com providers
// em demonstração. Aqui a prontidão é contada pelo que cada capacidade realmente reporta, e
// qualquer estado desconhecido cai em "Atenção" em vez de ser presumido saudável.
const readyStates = new Set(["AVAILABLE", "CONFIGURED", "OPERATIONAL", "READY", "ACTIVE"]);
const demonstrationStates = new Set(["DEMO_ONLY", "DEVELOPMENT_ONLY"]);

function summarizeReadiness(states: readonly string[]) {
  const summary = { ready: 0, demo: 0, pending: 0, attention: 0 };
  for (const state of states) {
    const normalized = state.toUpperCase();
    if (readyStates.has(normalized)) summary.ready += 1;
    else if (demonstrationStates.has(normalized)) summary.demo += 1;
    else if (normalized === "NOT_CONFIGURED") summary.pending += 1;
    else summary.attention += 1;
  }
  return summary;
}

function formatDate(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "short" }).format(date);
}

async function errorText(response: Response) {
  const body = await response.json().catch(() => null) as { title?: string; detail?: string } | null;
  return body?.detail ?? body?.title ?? `Erro ${response.status}`;
}
