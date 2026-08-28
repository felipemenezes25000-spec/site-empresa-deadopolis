"use client";

import { useEffect, useMemo, useState, type FormEvent } from "react";

type MigrationJob = {
  id: string;
  sourceBaseUrl: string;
  allowedHost: string;
  maxDepth: number;
  maxPages: number;
  state: string | number;
  discoveredCount: number;
  importedCount: number;
  failedCount: number;
  lastError: string | null;
  createdAt: string;
  updatedAt: string;
  completedAt: string | null;
};

type LegacyUrl = {
  id: string;
  migrationJobId: string;
  url: string;
  normalizedPath: string;
  depth: number;
  contentType: string | null;
  contentLength: number | null;
  sha256: string | null;
  classification: string;
  state: string;
  failureReason: string | null;
  discoveredAt: string;
};

type MigrationEvidence = {
  id: string;
  migrationJobId: string;
  kind: string;
  reference: string;
  payloadJson: string;
  createdAt: string;
};

type JobDetail = {
  job: MigrationJob;
  urlCount: number;
  runStartedAt: string | null;
  runCompletedAt: string | null;
  evidence: MigrationEvidence[];
};

type InventoryPage = {
  page: number;
  pageSize: number;
  total: number;
  totalPages: number;
  items: LegacyUrl[];
};

type RedirectRule = {
  id: string;
  legacyPath: string;
  destinationPath: string;
  statusCode: number;
  isActive: boolean;
  createdAt: string;
  lastValidatedAt: string | null;
};

type DryRunSummary = {
  discovered: number; failed: number; externalLinks: number; redirects: number; html: number;
  documents: number; pdf: number; office: number; images: number; duplicatesByHash: number;
  uniqueNormalized: number; queueRemaining: number; truncatedByLimit: boolean;
};

type DryRunResponse = {
  id: string;
  state: string | number;
  summary: DryRunSummary;
};

const migrationStateNames = ["Criado", "Descobrindo", "Mapeando", "Dry-run concluído", "Importando", "Validando", "Concluído", "Concluído com alertas", "Falhou"];

export function MigrationManager() {
  const [jobs, setJobs] = useState<MigrationJob[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [detail, setDetail] = useState<JobDetail | null>(null);
  const [inventory, setInventory] = useState<InventoryPage | null>(null);
  const [inventoryPage, setInventoryPage] = useState(1);
  const [inventoryQuery, setInventoryQuery] = useState("");
  const [inventoryClassification, setInventoryClassification] = useState("");
  const [inventoryState, setInventoryState] = useState("");
  const [inventoryKind, setInventoryKind] = useState("");
  const [redirects, setRedirects] = useState<RedirectRule[]>([]);
  const [query, setQuery] = useState("");
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);
  const [runningDryRun, setRunningDryRun] = useState(false);
  const [loading, setLoading] = useState(true);
  const [dryRunSummary, setDryRunSummary] = useState<DryRunSummary | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    void Promise.all([
      fetch("/api/v1/admin/migration/jobs", { signal: controller.signal }),
      fetch("/api/v1/admin/redirects", { signal: controller.signal }),
    ])
      .then(async ([jobsResponse, redirectsResponse]) => {
        if (!jobsResponse.ok) throw new Error(await errorText(jobsResponse));
        if (!redirectsResponse.ok) throw new Error(await errorText(redirectsResponse));
        if (controller.signal.aborted) return;
        setJobs(await jobsResponse.json() as MigrationJob[]);
        setRedirects(await redirectsResponse.json() as RedirectRule[]);
      })
      .catch((error) => {
        if (!controller.signal.aborted) setMessage(error instanceof Error ? error.message : "Falha ao carregar a operação de migração.");
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });
    return () => controller.abort();
  }, []);

  useEffect(() => {
    if (!selectedId) return;
    const controller = new AbortController();
    void Promise.all([
      fetch(`/api/v1/admin/migration/jobs/${selectedId}`, { signal: controller.signal }),
      fetch(inventoryUrl(selectedId, inventoryPage, inventoryQuery, inventoryClassification, inventoryState, inventoryKind), { signal: controller.signal }),
    ])
      .then(async ([detailResponse, inventoryResponse]) => {
        if (!detailResponse.ok) throw new Error(await errorText(detailResponse));
        if (!inventoryResponse.ok) throw new Error(await errorText(inventoryResponse));
        if (!controller.signal.aborted) {
          setDetail(await detailResponse.json() as JobDetail);
          setInventory(await inventoryResponse.json() as InventoryPage);
        }
      })
      .catch((error) => {
        if (!controller.signal.aborted) setMessage(error instanceof Error ? error.message : "Falha ao carregar o job.");
      });
    return () => controller.abort();
  }, [selectedId, inventoryPage, inventoryQuery, inventoryClassification, inventoryState, inventoryKind]);

  useEffect(() => {
    if (!runningDryRun || !selectedId) return;
    let active = true;
    const poll = async () => {
      try {
        const [jobsResponse, detailResponse, inventoryResponse] = await Promise.all([
          fetch("/api/v1/admin/migration/jobs"),
          fetch(`/api/v1/admin/migration/jobs/${selectedId}`),
          fetch(inventoryUrl(selectedId, inventoryPage, inventoryQuery, inventoryClassification, inventoryState, inventoryKind)),
        ]);
        if (!active || !jobsResponse.ok || !detailResponse.ok || !inventoryResponse.ok) return;
        setJobs(await jobsResponse.json() as MigrationJob[]);
        setDetail(await detailResponse.json() as JobDetail);
        setInventory(await inventoryResponse.json() as InventoryPage);
      } catch {
        // A requisição principal preserva o erro final; uma sondagem transitória não deve apagar o progresso já visível.
      }
    };
    void poll();
    const timer = window.setInterval(() => void poll(), 3000);
    return () => { active = false; window.clearInterval(timer); };
  }, [runningDryRun, selectedId, inventoryPage, inventoryQuery, inventoryClassification, inventoryState, inventoryKind]);

  const filteredJobs = useMemo(() => {
    const normalized = query.trim().toLocaleLowerCase("pt-BR");
    if (!normalized) return jobs;
    return jobs.filter((job) => [job.sourceBaseUrl, job.allowedHost, stateLabel(job.state)]
      .some((value) => value.toLocaleLowerCase("pt-BR").includes(normalized)));
  }, [jobs, query]);

  const persistedDryRunSummary = useMemo(() => {
    const evidence = detail?.evidence.find((item) => item.kind === "DRY_RUN_SUMMARY");
    return evidence ? parseDryRunSummary(evidence.payloadJson) : null;
  }, [detail]);
  const visibleDryRunSummary = dryRunSummary ?? persistedDryRunSummary;

  async function refreshJobs(preferredId?: string) {
    const response = await fetch("/api/v1/admin/migration/jobs");
    if (!response.ok) throw new Error(await errorText(response));
    const next = await response.json() as MigrationJob[];
    setJobs(next);
    const id = preferredId ?? selectedId;
    if (id && next.some((job) => job.id === id)) setSelectedId(id);
  }

  async function refreshDetail(id: string) {
    const [detailResponse, inventoryResponse] = await Promise.all([
      fetch(`/api/v1/admin/migration/jobs/${id}`),
      fetch(inventoryUrl(id, inventoryPage, inventoryQuery, inventoryClassification, inventoryState, inventoryKind)),
    ]);
    if (!detailResponse.ok) throw new Error(await errorText(detailResponse));
    if (!inventoryResponse.ok) throw new Error(await errorText(inventoryResponse));
    setDetail(await detailResponse.json() as JobDetail);
    setInventory(await inventoryResponse.json() as InventoryPage);
  }

  async function refreshRedirects() {
    const response = await fetch("/api/v1/admin/redirects");
    if (!response.ok) throw new Error(await errorText(response));
    setRedirects(await response.json() as RedirectRule[]);
  }

  async function createJob(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formElement = event.currentTarget;
    const form = new FormData(formElement);
    const sourceBaseUrl = String(form.get("sourceBaseUrl") ?? "").trim();
    let allowedHost: string;
    try {
      allowedHost = new URL(sourceBaseUrl).hostname.toLocaleLowerCase("en-US");
    } catch {
      setMessage("Informe uma URL HTTP/HTTPS absoluta válida.");
      return;
    }

    setBusy(true);
    setMessage("");
    setDryRunSummary(null);
    try {
      const response = await fetch("/api/v1/admin/migration/jobs", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          sourceBaseUrl,
          allowedHost,
          maxDepth: Number(form.get("maxDepth") ?? 2),
          maxPages: Number(form.get("maxPages") ?? 20000),
        }),
      });
      if (!response.ok) throw new Error(await errorText(response));
      const created = await response.json() as MigrationJob;
      formElement.reset();
      await refreshJobs(created.id);
      setSelectedId(created.id);
      await refreshDetail(created.id);
      setMessage(`Job criado para o host autorizado ${created.allowedHost}. Nenhum conteúdo foi importado.`);
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "Não foi possível criar o job de migração.");
    } finally {
      setBusy(false);
    }
  }

  async function runDryRun() {
    if (!selectedId) return;
    setBusy(true);
    setRunningDryRun(true);
    setMessage("");
    setDryRunSummary(null);
    try {
      const response = await fetch(`/api/v1/admin/migration/jobs/${selectedId}/run-dry-run`, { method: "POST" });
      if (!response.ok) throw new Error(await errorText(response));
      const result = await response.json() as DryRunResponse;
      setDryRunSummary(result.summary);
      await Promise.all([refreshJobs(selectedId), refreshDetail(selectedId)]);
      setMessage(`Dry-run concluído: ${result.summary.discovered} URL(s), ${result.summary.documents} documento(s), ${result.summary.failed} falha(s) e ${result.summary.queueRemaining} item(ns) pendente(s) na fila.`);
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "Não foi possível executar o dry-run.");
    } finally {
      setRunningDryRun(false);
      setBusy(false);
    }
  }

  async function createRedirect(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formElement = event.currentTarget;
    const form = new FormData(formElement);
    setBusy(true);
    setMessage("");
    try {
      const response = await fetch("/api/v1/admin/redirects", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          legacyUrl: String(form.get("legacyUrl") ?? "").trim(),
          destinationPath: String(form.get("destinationPath") ?? "").trim(),
          permanent: form.get("permanent") === "on",
        }),
      });
      if (!response.ok) throw new Error(await errorText(response));
      formElement.reset();
      await refreshRedirects();
      setMessage("Redirect legado registrado e auditado.");
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "Não foi possível criar o redirect.");
    } finally {
      setBusy(false);
    }
  }

  async function deactivateRedirect(id: string) {
    setBusy(true);
    setMessage("");
    try {
      const response = await fetch(`/api/v1/admin/redirects/${id}/deactivate`, { method: "POST" });
      if (!response.ok) throw new Error(await errorText(response));
      await refreshRedirects();
      setMessage("Redirect desativado; o histórico foi preservado.");
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "Não foi possível desativar o redirect.");
    } finally {
      setBusy(false);
    }
  }

  return <>
    <div className="warning-box">
      <strong>Dry-run seguro.</strong> O crawler aceita somente HTTP/HTTPS no host explicitamente autorizado, não segue redirecionamentos automaticamente e bloqueia resolução para endereços privados, locais ou reservados. Esta tela não importa conteúdo durante o dry-run.
    </div>

    <div className="editor-grid" style={{ marginTop: 20 }}>
      <section className="admin-panel">
        <div className="resource-toolbar"><label>Buscar jobs <input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Host, URL ou estado" /></label></div>
        {loading ? <div className="empty-state" aria-busy="true"><h3>Carregando migrações…</h3></div> : filteredJobs.length === 0 ? <div className="empty-state"><h3>Nenhum job de migração</h3><p>Crie um dry-run para inventariar o portal legado sem importar ou alterar conteúdo.</p></div> : <div className="compact-list">{filteredJobs.map((job) => <button type="button" key={job.id} className="compact-item" onClick={() => { setSelectedId(job.id); setInventoryPage(1); setInventoryQuery(""); setInventoryClassification(""); setInventoryState(""); setInventoryKind(""); setDryRunSummary(null); }} style={{ width: "100%", cursor: "pointer" }}><span><strong>{job.allowedHost}</strong><small style={{ display: "block", overflowWrap: "anywhere" }}>{job.sourceBaseUrl}</small><small style={{ display: "block" }}>{job.discoveredCount} descobertas · {job.failedCount} falhas</small></span><span className="status-pill">{stateLabel(job.state)}</span></button>)}</div>}
      </section>

      <form className="admin-panel editor-fields" onSubmit={createJob}>
        <h2>Novo inventário</h2>
        <label className="field">URL inicial<input name="sourceBaseUrl" type="url" required placeholder="https://portal-legado.exemplo.gov.br/" /></label>
        <small>O host autorizado é derivado automaticamente desta URL e fica fixo no job.</small>
        <label className="field">Profundidade máxima<input name="maxDepth" type="number" min="0" max="10" defaultValue="2" required /></label>
        <label className="field">Máximo de páginas<input name="maxPages" type="number" min="1" max="20000" defaultValue="20000" required /></label>
        <button className="action-button" disabled={busy}>Criar job de dry-run</button>
      </form>
    </div>

    {detail && selectedId === detail.job.id && <section className="admin-panel" style={{ marginTop: 20 }}>
      <div className="admin-heading">
        <div><span className="kicker">{stateLabel(detail.job.state)}</span><h2>Inventário de {detail.job.allowedHost}</h2><p style={{ overflowWrap: "anywhere" }}>{detail.job.sourceBaseUrl}</p><small>Criado em {formatDate(detail.job.createdAt)} · execução iniciada em {formatDate(detail.runStartedAt)} · execução concluída em {formatDate(detail.runCompletedAt)}</small></div>
        <div className="button-row"><a className="action-button secondary" href={`/api/v1/admin/migration/jobs/${detail.job.id}/report.csv`} download>Exportar relatório CSV</a><button type="button" className="action-button" onClick={() => void runDryRun()} disabled={busy || isRunning(detail.job.state)}>Executar dry-run seguro</button></div>
      </div>

      <div className="stat-grid">
        <div className="stat-card"><strong>{detail.job.discoveredCount}</strong><span>URLs descobertas</span></div>
        <div className="stat-card"><strong>{detail.job.importedCount}</strong><span>Itens importados</span></div>
        <div className="stat-card"><strong>{detail.job.failedCount}</strong><span>Falhas</span></div>
        <div className="stat-card"><strong>{detail.urlCount}</strong><span>URLs persistidas</span></div>
        <div className="stat-card"><strong>{visibleDryRunSummary?.externalLinks ?? 0}</strong><span>Referências externas</span></div>
        <div className="stat-card"><strong>{visibleDryRunSummary?.queueRemaining ?? 0}</strong><span>Itens na fila</span></div>
        <div className="stat-card"><strong>{detail.evidence.length}</strong><span>Evidências</span></div>
      </div>

      {visibleDryRunSummary && <div className={visibleDryRunSummary.truncatedByLimit ? "warning-box" : "ok-box"} role="status">Resultado persistido: {visibleDryRunSummary.discovered} URLs únicas · {visibleDryRunSummary.html} HTML · {visibleDryRunSummary.documents} documentos ({visibleDryRunSummary.pdf} PDF, {visibleDryRunSummary.office} Office) · {visibleDryRunSummary.images} imagens · {visibleDryRunSummary.redirects} redirects · {visibleDryRunSummary.externalLinks} externos · {visibleDryRunSummary.duplicatesByHash} duplicatas por hash · {visibleDryRunSummary.failed} falhas · {visibleDryRunSummary.queueRemaining} na fila{visibleDryRunSummary.truncatedByLimit ? " · limite atingido, inventário incompleto" : " · fila esvaziada"}.</div>}
      {runningDryRun && <div className="warning-box" role="status" aria-live="polite"><strong>Dry-run em execução.</strong> O painel atualiza contagens e inventário a cada três segundos; mantenha esta página aberta até o resultado final.</div>}
      {detail.job.lastError && <div className="warning-box"><strong>Último erro:</strong> {detail.job.lastError}</div>}

      <div className="editor-grid" style={{ marginTop: 20 }}>
        <div>
          <h3>URLs inventariadas</h3>
          <label className="field">Buscar no inventário<input type="search" value={inventoryQuery} onChange={(event) => { setInventoryQuery(event.target.value); setInventoryPage(1); }} placeholder="Caminho ou URL de origem" /></label>
          <div className="editor-grid">
            <label className="field">Classificação<select value={inventoryClassification} onChange={(event) => { setInventoryClassification(event.target.value); setInventoryPage(1); }}><option value="">Todas</option><option value="MIGRATE">Migrar</option><option value="REDIRECT">Redirect</option><option value="IGNORE_WITH_REASON">Ignorar com motivo</option><option value="UNCLASSIFIED">Não classificada</option></select></label>
            <label className="field">Estado<select value={inventoryState} onChange={(event) => { setInventoryState(event.target.value); setInventoryPage(1); }}><option value="">Todos</option><option value="DISCOVERED">Descoberta</option><option value="MAPPED">Mapeada</option><option value="FAILED">Falha</option></select></label>
            <label className="field">Tipo<select value={inventoryKind} onChange={(event) => { setInventoryKind(event.target.value); setInventoryPage(1); }}><option value="">Todos</option><option value="HTML">HTML</option><option value="DOCUMENT">Documentos</option><option value="IMAGE">Imagens</option><option value="FAILURE">Falhas</option></select></label>
          </div>
          {inventory?.items.length === 0 ? <div className="empty-state"><p>Execute o dry-run ou ajuste a busca para produzir resultados.</p></div> : <div className="compact-list">{inventory?.items.map((url) => <div className="compact-item" key={url.id}><div style={{ minWidth: 0 }}><strong>{url.normalizedPath}</strong><small style={{ display: "block", overflowWrap: "anywhere" }}>{url.url}</small><small style={{ display: "block" }}>{url.classification} · profundidade {url.depth}{url.contentType ? ` · ${url.contentType}` : ""}{url.contentLength != null ? ` · ${formatBytes(url.contentLength)}` : ""}</small>{url.sha256 && <small style={{ display: "block" }}>SHA-256 <code>{url.sha256.slice(0, 20)}…</code></small>}{url.failureReason && <small style={{ display: "block" }}>Bloqueio/falha: {url.failureReason}</small>}</div><span className="status-pill">{url.state}</span></div>)}</div>}
          {inventory && inventory.totalPages > 1 && <div className="button-row" style={{ marginTop: 12 }}><button type="button" className="action-button secondary" disabled={busy || inventory.page <= 1} onClick={() => setInventoryPage((page) => Math.max(1, page - 1))}>Anterior</button><span>Página {inventory.page} de {inventory.totalPages} · {inventory.total} resultado(s)</span><button type="button" className="action-button secondary" disabled={busy || inventory.page >= inventory.totalPages} onClick={() => setInventoryPage((page) => page + 1)}>Próxima</button></div>}
        </div>
        <div>
          <h3>Evidências</h3>
          {detail.evidence.length === 0 ? <div className="empty-state"><p>Nenhuma evidência registrada.</p></div> : <div className="compact-list">{detail.evidence.map((evidence) => <details className="compact-item" key={evidence.id}><summary><strong>{evidence.kind}</strong><small style={{ display: "block" }}>{formatDate(evidence.createdAt)} · {evidence.reference}</small></summary><pre style={{ whiteSpace: "pre-wrap", overflowWrap: "anywhere" }}>{prettyJson(evidence.payloadJson)}</pre></details>)}</div>}
        </div>
      </div>
    </section>}

    <section className="admin-panel" style={{ marginTop: 20 }}>
      <div className="admin-heading"><div><h2>Mapa de redirects</h2><p>Preserve URLs antigas com destino interno explícito. Redirects desativados continuam no histórico.</p></div></div>
      <div className="editor-grid">
        <form className="editor-fields" onSubmit={createRedirect}>
          <label className="field">URL ou caminho legado<input name="legacyUrl" required placeholder="/noticia-antiga?id=123" /></label>
          <label className="field">Destino interno<input name="destinationPath" required pattern="/.*" placeholder="/noticias/nova-url" /></label>
          <label><input name="permanent" type="checkbox" defaultChecked /> Redirect permanente (301)</label>
          <button className="action-button secondary" disabled={busy}>Adicionar redirect</button>
        </form>
        <div>
          {redirects.length === 0 ? <div className="empty-state"><h3>Nenhum redirect cadastrado</h3></div> : <div className="compact-list">{redirects.map((rule) => <div className="compact-item" key={rule.id}><div><strong>{rule.legacyPath} → {rule.destinationPath}</strong><small style={{ display: "block" }}>HTTP {rule.statusCode} · criado em {formatDate(rule.createdAt)}</small></div><div className="button-row"><span className="status-pill">{rule.isActive ? "Ativo" : "Inativo"}</span>{rule.isActive && <button type="button" className="action-button secondary" disabled={busy} onClick={() => void deactivateRedirect(rule.id)}>Desativar</button>}</div></div>)}</div>}
        </div>
      </div>
    </section>

    {message && <div className="form-message" role="status" style={{ marginTop: 16 }}>{message}</div>}
  </>;
}

// Exportado para que o painel de importação use exatamente o mesmo vocabulário de estado.
export function stateLabel(value: string | number) {
  if (typeof value === "number") return migrationStateNames[value] ?? `Estado ${value}`;
  const numeric = Number(value);
  if (Number.isInteger(numeric) && String(numeric) === value) return migrationStateNames[numeric] ?? `Estado ${value}`;
  const labels: Record<string, string> = {
    CREATED: "Criado",
    DISCOVERING: "Descobrindo",
    MAPPING: "Mapeando",
    DRYRUN: "Dry-run concluído",
    DRY_RUN: "Dry-run concluído",
    IMPORTING: "Importando",
    VALIDATING: "Validando",
    COMPLETED: "Concluído",
    COMPLETEDWITHWARNINGS: "Concluído com alertas",
    COMPLETED_WITH_WARNINGS: "Concluído com alertas",
    FAILED: "Falhou",
  };
  const key = value.replaceAll("-", "_").replaceAll(" ", "_").toUpperCase();
  return labels[key] ?? value;
}

function isRunning(value: string | number) {
  const label = stateLabel(value);
  return label === "Descobrindo" || label === "Mapeando" || label === "Importando" || label === "Validando";
}

function formatDate(value: string | null) {
  if (!value) return "—";
  return new Intl.DateTimeFormat("pt-BR", { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));
}

function formatBytes(value: number) {
  if (value < 1024) return `${value} B`;
  if (value < 1024 * 1024) return `${(value / 1024).toFixed(1)} KB`;
  return `${(value / (1024 * 1024)).toFixed(1)} MB`;
}

function prettyJson(value: string) {
  try { return JSON.stringify(JSON.parse(value), null, 2); } catch { return value; }
}

function parseDryRunSummary(value: string): DryRunSummary | null {
  try {
    const parsed = JSON.parse(value) as Partial<DryRunSummary>;
    return typeof parsed.discovered === "number"
      && typeof parsed.queueRemaining === "number"
      && typeof parsed.truncatedByLimit === "boolean"
      ? parsed as DryRunSummary
      : null;
  } catch {
    return null;
  }
}

async function errorText(response: Response) {
  const body = await response.json().catch(() => null) as { title?: string; detail?: string; errors?: Record<string, string[]> } | null;
  const validation = Object.values(body?.errors ?? {}).flat().join(" ");
  return body?.detail ?? body?.title ?? (validation || `Erro ${response.status}`);
}

function inventoryUrl(jobId: string, page: number, query: string, classification: string, state: string, kind: string) {
  const parameters = new URLSearchParams({ page: String(page), pageSize: "100" });
  if (query.trim()) parameters.set("q", query.trim());
  if (classification) parameters.set("classification", classification);
  if (state) parameters.set("state", state);
  if (kind) parameters.set("kind", kind);
  return `/api/v1/admin/migration/jobs/${jobId}/urls?${parameters.toString()}`;
}
