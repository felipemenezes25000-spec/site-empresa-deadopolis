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

type JobDetail = { job: MigrationJob };

type InventoryPage = {
  page: number;
  pageSize: number;
  total: number;
  totalPages: number;
  items: LegacyUrl[];
};

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

type DocumentImportResponse = {
  document: { id: string; title: string; status: string; category: string };
  asset: { id: string; status: string; sha256: string; mimeType: string; sizeBytes: number };
  reusedAsset: boolean;
  detail: string;
};

export function MigrationImportManager() {
  const [jobs, setJobs] = useState<MigrationJob[]>([]);
  const [selectedJobId, setSelectedJobId] = useState<string | null>(null);
  const [detail, setDetail] = useState<JobDetail | null>(null);
  const [inventory, setInventory] = useState<InventoryPage | null>(null);
  const [inventoryPage, setInventoryPage] = useState(1);
  const [inventoryQuery, setInventoryQuery] = useState("");
  const [imports, setImports] = useState<ImportedContent[]>([]);
  const [selectedLegacyId, setSelectedLegacyId] = useState<string | null>(null);
  const [batchDocuments, setBatchDocuments] = useState<LegacyUrl[]>([]);
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
      fetch(inventoryUrl(selectedJobId, inventoryPage, inventoryQuery), { signal: controller.signal }),
    ])
      .then(async ([detailResponse, importsResponse, inventoryResponse]) => {
        if (!detailResponse.ok) throw new Error(await errorText(detailResponse));
        if (!importsResponse.ok) throw new Error(await errorText(importsResponse));
        if (!inventoryResponse.ok) throw new Error(await errorText(inventoryResponse));
        if (controller.signal.aborted) return;
        setDetail(await detailResponse.json() as JobDetail);
        setImports(await importsResponse.json() as ImportedContent[]);
        setInventory(await inventoryResponse.json() as InventoryPage);
      })
      .catch((error) => {
        if (!controller.signal.aborted) setMessage(error instanceof Error ? error.message : "Falha ao carregar candidatos de importação.");
      });
    return () => controller.abort();
  }, [selectedJobId, inventoryPage, inventoryQuery]);

  const candidates = useMemo(() => {
    if (!inventory) return [];
    const importedIds = new Set(imports.map((item) => item.legacyUrlId));
    return inventory.items.filter((url) =>
      url.state.toLocaleUpperCase("en-US") === "MAPPED"
      && url.classification.toLocaleUpperCase("en-US") === "MIGRATE"
      && (isHtml(url.contentType) || isDocument(url))
      && Boolean(url.sha256)
      && !importedIds.has(url.id));
  }, [inventory, imports]);

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
    const [detailResponse, importsResponse, inventoryResponse] = await Promise.all([
      fetch(`/api/v1/admin/migration/jobs/${jobId}`),
      fetch(`/api/v1/admin/migration/jobs/${jobId}/imports`),
      fetch(inventoryUrl(jobId, inventoryPage, inventoryQuery)),
    ]);
    if (!detailResponse.ok) throw new Error(await errorText(detailResponse));
    if (!importsResponse.ok) throw new Error(await errorText(importsResponse));
    if (!inventoryResponse.ok) throw new Error(await errorText(inventoryResponse));
    setDetail(await detailResponse.json() as JobDetail);
    setImports(await importsResponse.json() as ImportedContent[]);
    setInventory(await inventoryResponse.json() as InventoryPage);
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

  async function importDocument(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selectedJobId || !selectedLegacy) return;
    const form = new FormData(event.currentTarget);
    setBusy(true);
    setMessage("");
    try {
      const response = await fetch(`/api/v1/admin/migration/jobs/${selectedJobId}/urls/${selectedLegacy.id}/import-document`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          category: String(form.get("category") ?? "").trim(),
          subcategory: optional(form.get("subcategory")),
          title: String(form.get("title") ?? "").trim(),
          description: optional(form.get("description")),
          documentNumber: optional(form.get("documentNumber")),
          processNumber: optional(form.get("processNumber")),
          referencePeriod: optional(form.get("referencePeriod")),
          publicationDate: optional(form.get("publicationDate")),
          responsibleDepartment: optional(form.get("responsibleDepartment")),
          documentType: String(form.get("documentType") ?? "DOCUMENT").trim(),
        }),
      });
      if (!response.ok) throw new Error(await errorText(response));
      const result = await response.json() as DocumentImportResponse;
      await refreshSelected(selectedJobId);
      const jobsResponse = await fetch("/api/v1/admin/migration/jobs");
      if (jobsResponse.ok) setJobs(await jobsResponse.json() as MigrationJob[]);
      setSelectedLegacyId(null);
      setBatchDocuments((items) => items.filter((item) => item.id !== selectedLegacy.id));
      setMessage(`${result.detail} ${result.document.title} criado em ${result.document.status}; asset ${result.asset.status}${result.reusedAsset ? " reutilizado por hash" : " persistido"}.`);
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "Não foi possível importar o documento legado.");
    } finally {
      setBusy(false);
    }
  }

  function toggleBatchDocument(item: LegacyUrl) {
    setBatchDocuments((current) => {
      if (current.some((selected) => selected.id === item.id)) return current.filter((selected) => selected.id !== item.id);
      if (current.length >= 10) {
        setMessage("O lote seguro aceita no máximo 10 documentos por operação.");
        return current;
      }
      return [...current, item];
    });
  }

  async function importDocumentBatch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selectedJobId || batchDocuments.length === 0) return;
    const form = new FormData(event.currentTarget);
    setBusy(true);
    setMessage("");
    const failures: Array<{ id: string; detail: string }> = [];
    let imported = 0;
    try {
      for (const item of batchDocuments) {
        const response = await fetch(`/api/v1/admin/migration/jobs/${selectedJobId}/urls/${item.id}/import-document`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            category: String(form.get("category") ?? "").trim(),
            subcategory: optional(form.get("subcategory")),
            title: titleFromPath(item.normalizedPath),
            description: optional(form.get("description")),
            documentNumber: null,
            processNumber: null,
            referencePeriod: optional(form.get("referencePeriod")),
            publicationDate: null,
            responsibleDepartment: optional(form.get("responsibleDepartment")),
            documentType: documentTypeFromPath(item.normalizedPath),
          }),
        });
        if (response.ok) imported++;
        else failures.push({ id: item.id, detail: await errorText(response) });
      }
      await refreshSelected(selectedJobId);
      setBatchDocuments((items) => items.filter((item) => failures.some((failure) => failure.id === item.id)));
      setMessage(`${imported} documento(s) criado(s) como rascunho. ${failures.length === 0 ? "Lote concluído sem falhas." : `${failures.length} falha(s) permaneceram selecionadas: ${failures.map((failure) => failure.detail).join(" | ")}`}`);
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "O lote foi interrompido; atualize o inventário antes de retomar.");
    } finally {
      setBusy(false);
    }
  }

  return <section className="admin-panel" style={{ marginTop: 20 }}>
    <div className="admin-heading">
      <div>
        <span className="kicker">ETL controlado</span>
        <h2>Importação controlada de conteúdo e documentos</h2>
        <p>Refaz o fetch com proteção SSRF, exige o mesmo SHA-256 do dry-run e encaminha HTML ao CMS ou arquivos ao acervo documental. Nada é publicado automaticamente.</p>
      </div>
      <button type="button" className="action-button secondary" onClick={() => void refreshJobs()} disabled={busy}>Atualizar importações</button>
    </div>

    <div className="warning-box">
      <strong>Integridade antes de velocidade.</strong> Se a origem mudou desde o inventário, a importação é recusada. HTML vira texto inerte; PDF, Office e imagens passam por magic bytes, MIME, malware scan, storage e quarentena antes do acervo.
    </div>

    {loading ? <div className="empty-state" aria-busy="true"><p>Carregando inventários…</p></div> : jobs.length === 0 ? <div className="empty-state"><h3>Nenhum inventário disponível</h3><p>Execute um dry-run acima antes de preparar conteúdo.</p></div> : <div className="compact-list" style={{ marginTop: 16 }}>
      {jobs.map((job) => <button key={job.id} type="button" className="compact-item" style={{ width: "100%", cursor: "pointer" }} onClick={() => { setSelectedJobId(job.id); setSelectedLegacyId(null); setBatchDocuments([]); setInventoryPage(1); setInventoryQuery(""); setMessage(""); }}>
        <span><strong>{job.allowedHost}</strong><small style={{ display: "block" }}>{job.discoveredCount} descobertas · {job.importedCount} importadas · {job.failedCount} falhas</small></span>
        <span className="status-pill">{String(job.state)}</span>
      </button>)}
    </div>}

    {detail && detail.job.id === selectedJobId && <div className="editor-grid" style={{ marginTop: 20 }}>
      <div>
        <div className="admin-heading"><div><h3>Conteúdos aptos</h3><p>{inventory?.total ?? 0} URL(s) mapeada(s) para migração.</p></div></div>
        <label className="field">Buscar no inventário<input type="search" value={inventoryQuery} onChange={(event) => { setInventoryQuery(event.target.value); setInventoryPage(1); }} placeholder="Caminho ou URL de origem" /></label>
        {candidates.length === 0 ? <div className="empty-state"><p>Nenhum HTML ou documento mapeado e íntegro está pendente de importação neste job.</p></div> : <div className="compact-list">
          {candidates.map((url) => <div key={url.id} className="compact-item"><button type="button" style={{ minWidth: 0, flex: 1, border: 0, background: "transparent", color: "inherit", cursor: "pointer", textAlign: "left" }} onClick={() => { setSelectedLegacyId(url.id); setMessage(""); }}><strong>{url.normalizedPath}</strong><small style={{ display: "block", overflowWrap: "anywhere" }}>{url.url}</small><small style={{ display: "block" }}>{url.contentType} · SHA-256 {url.sha256?.slice(0, 16)}…</small></button><div className="button-row"><span className="status-pill">{isHtml(url.contentType) ? "Página" : "Documento"}</span>{isDocument(url) && <button type="button" className="action-button secondary" aria-pressed={batchDocuments.some((item) => item.id === url.id)} onClick={() => toggleBatchDocument(url)}>{batchDocuments.some((item) => item.id === url.id) ? "Remover do lote" : "Adicionar ao lote"}</button>}</div></div>)}
        </div>}
        {inventory && inventory.totalPages > 1 && <div className="button-row" style={{ marginTop: 12 }}><button type="button" className="action-button secondary" disabled={busy || inventory.page <= 1} onClick={() => setInventoryPage((page) => Math.max(1, page - 1))}>Anterior</button><span>Página {inventory.page} de {inventory.totalPages}</span><button type="button" className="action-button secondary" disabled={busy || inventory.page >= inventory.totalPages} onClick={() => setInventoryPage((page) => page + 1)}>Próxima</button></div>}
      </div>

      <div>
        <h3>Conteúdo já preparado</h3>
        {imports.length === 0 ? <div className="empty-state"><p>Nenhum rascunho foi criado a partir deste inventário.</p></div> : <div className="compact-list">
          {imports.map((item) => <div className="compact-item" key={item.id}><span><strong>{item.targetType}</strong><small style={{ display: "block" }}>Recurso {item.targetReference}</small><small style={{ display: "block" }}>SHA-256 {item.sourceSha256.slice(0, 16)}… · {formatDate(item.importedAt)}</small></span><span className="status-pill">Importado</span></div>)}
        </div>}
      </div>
    </div>}

    {batchDocuments.length > 0 && <form className="admin-panel editor-fields" style={{ marginTop: 20 }} onSubmit={importDocumentBatch}>
      <div className="admin-heading"><div><span className="kicker">LOTE SEGURO · {batchDocuments.length}/10</span><h3>Preparar documentos selecionados</h3><p>Processamento sequencial e auditável. O título vem do nome do arquivo; os documentos permanecem em rascunho e cada falha é exibida sem desfazer sucessos anteriores.</p></div><button type="button" className="action-button secondary" onClick={() => setBatchDocuments([])} disabled={busy}>Limpar lote</button></div>
      <div className="compact-list">{batchDocuments.map((item) => <div className="compact-item" key={item.id}><span><strong>{titleFromPath(item.normalizedPath)}</strong><small style={{ display: "block", overflowWrap: "anywhere" }}>{item.normalizedPath}</small></span><button type="button" className="action-button secondary" onClick={() => toggleBatchDocument(item)} disabled={busy}>Remover</button></div>)}</div>
      <div className="editor-grid">
        <label className="field">Categoria<select name="category" required defaultValue="DOCUMENTOS"><option value="DOCUMENTOS">Documentos gerais</option><option value="LICITACOES">Licitações</option><option value="PRESTACAO_CONTAS">Prestação de contas</option><option value="INFORMATIVOS">Informativos</option></select></label>
        <label className="field">Subcategoria comum<input name="subcategory" maxLength={120} placeholder="Ex.: EDITAL, CONTRATO, RREO" /></label>
        <label className="field">Órgão responsável<input name="responsibleDepartment" maxLength={180} /></label>
        <label className="field">Período de referência<input name="referencePeriod" maxLength={120} /></label>
      </div>
      <label className="field">Descrição comum<textarea name="description" maxLength={2000} rows={3} /></label>
      <button className="action-button" disabled={busy}>Validar e criar {batchDocuments.length} rascunho(s)</button>
    </form>}

    {selectedLegacy && isHtml(selectedLegacy.contentType) && <form key={selectedLegacy.id} className="admin-panel editor-fields" style={{ marginTop: 20 }} onSubmit={importPage}>
      <h3>Preparar rascunho de {selectedLegacy.normalizedPath}</h3>
      <p>O destino é um recurso <strong>PAGE / DRAFT</strong>. Revise no CMS antes de qualquer publicação.</p>
      <label className="field">Slug do rascunho<input name="slug" required maxLength={180} pattern="[a-z0-9-]+" defaultValue={slugFromPath(selectedLegacy.normalizedPath)} /></label>
      <label className="field">Título opcional<input name="title" maxLength={220} placeholder="Se vazio, usa o título extraído da página" /></label>
      <label className="field">Resumo opcional<textarea name="summary" maxLength={500} rows={3} placeholder="Contexto editorial para revisão" /></label>
      <label className="field">Destino de redirect opcional<input name="redirectDestination" pattern="/.*" placeholder="/servicos ou outra rota interna já existente" /></label>
      <label><input name="permanentRedirect" type="checkbox" defaultChecked /> Redirect permanente (301), quando houver destino</label>
      <button className="action-button" disabled={busy}>Criar rascunho com evidência</button>
    </form>}

    {selectedLegacy && isDocument(selectedLegacy) && <form key={selectedLegacy.id} className="admin-panel editor-fields" style={{ marginTop: 20 }} onSubmit={importDocument}>
      <h3>Arquivar documento {selectedLegacy.normalizedPath}</h3>
      <p>O arquivo será revalidado e criado como <strong>DRAFT</strong>. A publicação exige aprovação do asset e ação administrativa explícita.</p>
      <div className="editor-grid">
        <label className="field">Categoria<select name="category" required defaultValue="DOCUMENTOS"><option value="DOCUMENTOS">Documentos gerais</option><option value="LICITACOES">Licitações</option><option value="PRESTACAO_CONTAS">Prestação de contas</option><option value="INFORMATIVOS">Informativos</option></select></label>
        <label className="field">Subcategoria<input name="subcategory" maxLength={120} placeholder="Ex.: RREO, EDITAL, CONTRATO" /></label>
      </div>
      <label className="field">Título<input name="title" required maxLength={220} defaultValue={titleFromPath(selectedLegacy.normalizedPath)} /></label>
      <label className="field">Descrição<textarea name="description" maxLength={2000} rows={3} /></label>
      <div className="editor-grid">
        <label className="field">Tipo documental<input name="documentType" required maxLength={80} defaultValue={documentTypeFromPath(selectedLegacy.normalizedPath)} /></label>
        <label className="field">Órgão responsável<input name="responsibleDepartment" maxLength={180} /></label>
        <label className="field">Número<input name="documentNumber" maxLength={120} /></label>
        <label className="field">Processo<input name="processNumber" maxLength={120} /></label>
        <label className="field">Período de referência<input name="referencePeriod" maxLength={120} placeholder="Ex.: 2025 ou 1º bimestre/2025" /></label>
        <label className="field">Data de publicação<input name="publicationDate" type="date" /></label>
      </div>
      <button className="action-button" disabled={busy}>Validar e criar documento no acervo</button>
    </form>}

    {message && <div className="form-message" role="status" style={{ marginTop: 16 }}>{message}</div>}
  </section>;
}

function isHtml(contentType: string | null) {
  return contentType?.toLocaleLowerCase("en-US") === "text/html" || contentType?.toLocaleLowerCase("en-US") === "application/xhtml+xml";
}

function isDocument(url: LegacyUrl) {
  const contentType = url.contentType?.toLocaleLowerCase("en-US") ?? "";
  return contentType === "application/pdf"
    || contentType.startsWith("image/")
    || contentType.includes("msword")
    || contentType.includes("officedocument")
    || contentType.includes("spreadsheet")
    || /\.(?:pdf|docx?|xlsx?|pptx?|jpe?g|png|webp)(?:\?|$)/i.test(url.normalizedPath);
}

function slugFromPath(path: string) {
  const pathname = path.split("?", 1)[0];
  const tail = pathname.split("/").filter(Boolean).at(-1) ?? "pagina-importada";
  const normalized = tail.normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLocaleLowerCase("en-US").replace(/[^a-z0-9]+/g, "-").replace(/^-+|-+$/g, "");
  return normalized || "pagina-importada";
}

function titleFromPath(path: string) {
  const fileName = decodeURIComponent(path.split("?", 1)[0].split("/").filter(Boolean).at(-1) ?? "Documento legado");
  return fileName.replace(/\.[a-z0-9]+$/i, "").replaceAll(/[-_]+/g, " ").replace(/^./, (character) => character.toLocaleUpperCase("pt-BR"));
}

function documentTypeFromPath(path: string) {
  const extension = path.split("?", 1)[0].split(".").at(-1)?.toLocaleUpperCase("en-US");
  return extension === "PDF" ? "PDF" : ["DOC", "DOCX", "XLS", "XLSX", "PPT", "PPTX"].includes(extension ?? "") ? "OFFICE" : "DOCUMENT";
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

function inventoryUrl(jobId: string, page: number, query: string) {
  const parameters = new URLSearchParams({
    page: String(page),
    pageSize: "100",
    classification: "MIGRATE",
    state: "MAPPED",
  });
  if (query.trim()) parameters.set("q", query.trim());
  return `/api/v1/admin/migration/jobs/${jobId}/urls?${parameters.toString()}`;
}
