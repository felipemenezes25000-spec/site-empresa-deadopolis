"use client";

import { useEffect, useMemo, useState, type FormEvent } from "react";

type MigrationJob = {
  id: string;
  allowedHost: string;
  state: string | number;
  discoveredCount: number;
  importedCount: number;
  failedCount: number;
};

type LegacyUrl = {
  id: string;
  url: string;
  normalizedPath: string;
  contentType: string | null;
  sha256: string | null;
  classification: string;
  state: string;
};

type JobDetail = { job: MigrationJob; urls: LegacyUrl[] };

type ImportedContent = {
  id: string;
  migrationJobId: string;
  legacyUrlId: string;
  targetType: string;
  targetReference: string;
  sourceSha256: string;
  importedAt: string;
};

type ImportResponse = {
  resource: { id: string; kind: string; slug: string; title: string; status: string; version: number };
  redirect: { id: string; legacyPath: string; destinationPath: string; statusCode: number } | null;
  detail: string;
};

export function MigrationImportManager() {
  const [jobs, setJobs] = useState<MigrationJob[]>([]);
  const [selectedJobId, setSelectedJobId] = useState<string | null>(null);
  const [detail, setDetail] = useState<JobDetail | null>(null);
  const [imports, setImports] = useState<ImportedContent[]>([]);
  const [selectedLegacyId, setSelectedLegacyId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState("");

  useEffect(() => {
    const controller = new AbortController();
    void fetch("/api/v1/admin/migration/jobs", { signal: controller.signal })
      .then(async (response) => {
        if (!response.ok) throw new Error(await errorText(response));
        if (!controller.signal.aborted) setJobs(await response.json() as MigrationJob[]);
      })
      .catch((error) => {
        if (!controller.signal.aborted) setMessage(error instanceof Error ? error.message : "Falha ao carregar jobs de migração.");
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });
    return () => controller.abort();
  }, []);

  useEffect(() => {
    if (!selectedJobId) return;
    const controller = new AbortController();
    void Promise.all([
      fetch(`/api/v1/admin/migration/jobs/${selectedJobId}`, { signal: controller.signal }),
      fetch(`/api/v1/admin/migration/jobs/${selectedJobId}/imports`, { signal: controller.signal }),
    ])
      .then(async ([detailResponse, importsResponse]) => {
        if (!detailResponse.ok) throw new Error(await errorText(detailResponse));
        if (!importsResponse.ok) throw new Error(await errorText(importsResponse));
        if (controller.signal.aborted) return;
        setDetail(await detailResponse.json() as JobDetail);
        setImports(await importsResponse.json() as ImportedContent[]);
      })
      .catch((error) => {
        if (!controller.signal.aborted) setMessage(error instanceof Error ? error.message : "Falha ao carregar candidatos de importação.");
      });
    return () => controller.abort();
  }, [selectedJobId]);

  const candidates = useMemo(() => {
    if (!detail) return [];
    const importedIds = new Set(imports.map((item) => item.legacyUrlId));
    return detail.urls.filter((url) =>
      url.state.toLocaleUpperCase("en-US") === "MAPPED"
      && url.classification.toLocaleUpperCase("en-US") === "MIGRATE"
      && isHtml(url.contentType)
      && Boolean(url.sha256)
      && !importedIds.has(url.id));
  }, [detail, imports]);

  const selectedLegacy = candidates.find((item) => item.id === selectedLegacyId) ?? null;

  async function refreshJobs() {
    setBusy(true);
    setMessage("");
    try {
      const response = await fetch("/api/v1/admin/migration/jobs");
      if (!response.ok) throw new Error(await errorText(response));
      setJobs(await response.json() as MigrationJob[]);
      if (selectedJobId) await refreshSelected(selectedJobId);
      setMessage("Inventários e importações atualizados.");
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "Não foi possível atualizar as importações.");
    } finally {
      setBusy(false);
    }
  }

  async function refreshSelected(jobId: string) {
    const [detailResponse, importsResponse] = await Promise.all([
      fetch(`/api/v1/admin/migration/jobs/${jobId}`),
      fetch(`/api/v1/admin/migration/jobs/${jobId}/imports`),
    ]);
    if (!detailResponse.ok) throw new Error(await errorText(detailResponse));
    if (!importsResponse.ok) throw new Error(await errorText(importsResponse));
    setDetail(await detailResponse.json() as JobDetail);
    setImports(await importsResponse.json() as ImportedContent[]);
  }

  async function importPage(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selectedJobId || !selectedLegacy) return;
    const formElement = event.currentTarget;
    const form = new FormData(formElement);
    setBusy(true);
    setMessage("");
    try {
      const response = await fetch(`/api/v1/admin/migration/jobs/${selectedJobId}/urls/${selectedLegacy.id}/import-page`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          slug: String(form.get("slug") ?? "").trim(),
          title: optional(form.get("title")),
          summary: optional(form.get("summary")),
          redirectDestination: optional(form.get("redirectDestination")),
          permanentRedirect: form.get("permanentRedirect") === "on",
        }),
      });
      if (!response.ok) throw new Error(await errorText(response));
      const result = await response.json() as ImportResponse;
      await refreshSelected(selectedJobId);
      const jobsResponse = await fetch("/api/v1/admin/migration/jobs");
      if (jobsResponse.ok) setJobs(await jobsResponse.json() as MigrationJob[]);
      setSelectedLegacyId(null);
      setMessage(`${result.detail} Rascunho ${result.resource.slug} criado em ${result.resource.status}.${result.redirect ? ` Redirect ${result.redirect.statusCode} registrado para ${result.redirect.destinationPath}.` : ""}`);
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "Não foi possível preparar o rascunho CMS.");
    } finally {
      setBusy(false);
    }
  }

  return <section className="admin-panel" style={{ marginTop: 20 }}>
    <div className="admin-heading">
      <div>
        <span className="kicker">ETL controlado</span>
        <h2>Importação para rascunhos CMS</h2>
        <p>Refaz o fetch com proteção SSRF, exige o mesmo SHA-256 do dry-run e converte HTML em texto inerte. Nada é publicado automaticamente.</p>
      </div>
      <button type="button" className="action-button secondary" onClick={() => void refreshJobs()} disabled={busy}>Atualizar importações</button>
    </div>

    <div className="warning-box">
      <strong>Integridade antes de velocidade.</strong> Se a origem mudou desde o inventário, a importação é recusada. Scripts, estilos e marcação executável não são levados para o CMS. PDFs e mídias continuam fora deste fluxo e devem passar pela biblioteca de mídia.
    </div>

    {loading ? <div className="empty-state" aria-busy="true"><p>Carregando inventários…</p></div> : jobs.length === 0 ? <div className="empty-state"><h3>Nenhum inventário disponível</h3><p>Execute um dry-run acima antes de preparar conteúdo.</p></div> : <div className="compact-list" style={{ marginTop: 16 }}>
      {jobs.map((job) => <button key={job.id} type="button" className="compact-item" style={{ width: "100%", cursor: "pointer" }} onClick={() => { setSelectedJobId(job.id); setSelectedLegacyId(null); setMessage(""); }}>
        <span><strong>{job.allowedHost}</strong><small style={{ display: "block" }}>{job.discoveredCount} descobertas · {job.importedCount} importadas · {job.failedCount} falhas</small></span>
        <span className="status-pill">{String(job.state)}</span>
      </button>)}
    </div>}

    {detail && detail.job.id === selectedJobId && <div className="editor-grid" style={{ marginTop: 20 }}>
      <div>
        <h3>Páginas aptas</h3>
        {candidates.length === 0 ? <div className="empty-state"><p>Nenhuma página HTML mapeada e íntegra está pendente de importação neste job.</p></div> : <div className="compact-list">
          {candidates.map((url) => <button key={url.id} type="button" className="compact-item" style={{ width: "100%", cursor: "pointer" }} onClick={() => { setSelectedLegacyId(url.id); setMessage(""); }}>
            <span style={{ minWidth: 0 }}><strong>{url.normalizedPath}</strong><small style={{ display: "block", overflowWrap: "anywhere" }}>{url.url}</small><small style={{ display: "block" }}>{url.contentType} · SHA-256 {url.sha256?.slice(0, 16)}…</small></span>
            <span className="status-pill">Preparar</span>
          </button>)}
        </div>}
      </div>

      <div>
        <h3>Conteúdo já preparado</h3>
        {imports.length === 0 ? <div className="empty-state"><p>Nenhum rascunho foi criado a partir deste inventário.</p></div> : <div className="compact-list">
          {imports.map((item) => <div className="compact-item" key={item.id}><span><strong>{item.targetType}</strong><small style={{ display: "block" }}>Recurso {item.targetReference}</small><small style={{ display: "block" }}>SHA-256 {item.sourceSha256.slice(0, 16)}… · {formatDate(item.importedAt)}</small></span><span className="status-pill">Importado</span></div>)}
        </div>}
      </div>
    </div>}

    {selectedLegacy && <form key={selectedLegacy.id} className="admin-panel editor-fields" style={{ marginTop: 20 }} onSubmit={importPage}>
      <h3>Preparar rascunho de {selectedLegacy.normalizedPath}</h3>
      <p>O destino é um recurso <strong>PAGE / DRAFT</strong>. Revise no CMS antes de qualquer publicação.</p>
      <label className="field">Slug do rascunho<input name="slug" required maxLength={180} pattern="[a-z0-9-]+" defaultValue={slugFromPath(selectedLegacy.normalizedPath)} /></label>
      <label className="field">Título opcional<input name="title" maxLength={220} placeholder="Se vazio, usa o título extraído da página" /></label>
      <label className="field">Resumo opcional<textarea name="summary" maxLength={500} rows={3} placeholder="Contexto editorial para revisão" /></label>
      <label className="field">Destino de redirect opcional<input name="redirectDestination" pattern="/.*" placeholder="/servicos ou outra rota interna já existente" /></label>
      <label><input name="permanentRedirect" type="checkbox" defaultChecked /> Redirect permanente (301), quando houver destino</label>
      <button className="action-button" disabled={busy}>Criar rascunho com evidência</button>
    </form>}

    {message && <div className="form-message" role="status" style={{ marginTop: 16 }}>{message}</div>}
  </section>;
}

function isHtml(contentType: string | null) {
  return contentType?.toLocaleLowerCase("en-US") === "text/html" || contentType?.toLocaleLowerCase("en-US") === "application/xhtml+xml";
}

function slugFromPath(path: string) {
  const pathname = path.split("?", 1)[0];
  const tail = pathname.split("/").filter(Boolean).at(-1) ?? "pagina-importada";
  const normalized = tail.normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLocaleLowerCase("en-US").replace(/[^a-z0-9]+/g, "-").replace(/^-+|-+$/g, "");
  return normalized || "pagina-importada";
}

function optional(value: FormDataEntryValue | null) {
  const normalized = String(value ?? "").trim();
  return normalized || null;
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "short" }).format(new Date(value));
}

async function errorText(response: Response) {
  try {
    const payload = await response.json() as { detail?: string; title?: string; errors?: Record<string, string[]> };
    return payload.detail ?? payload.title ?? (Object.values(payload.errors ?? {}).flat().join(" ") || `Falha HTTP ${response.status}`);
  } catch {
    return `Falha HTTP ${response.status}`;
  }
}
