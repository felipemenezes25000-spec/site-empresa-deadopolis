"use client";

import Link from "next/link";
import { useEffect, useMemo, useState, type FormEvent } from "react";

type Dataset = {
  id: string;
  title: string;
  slug: string;
  description: string;
  category: string;
  responsibleDepartment: string;
  license: string;
  updateFrequency: string;
  referencePeriod: string | null;
  lastUpdatedAt: string | null;
  nextExpectedUpdateAt: string | null;
  status: string | number;
  source: string | null;
  createdAt: string;
  updatedAt: string;
  publishedAt: string | null;
};

type DatasetVersion = {
  id: string;
  datasetId: string;
  version: number;
  fileName: string;
  mimeType: string;
  sizeBytes: number;
  sha256: string;
  format: string;
  metadataJson: string;
  publishedAt: string;
};

type StatusKey = "DRAFT" | "PUBLISHED" | "ARCHIVED";

export function DatasetManager() {
  const [items, setItems] = useState<Dataset[]>([]);
  const [selected, setSelected] = useState<Dataset | null>(null);
  const [versions, setVersions] = useState<DatasetVersion[]>([]);
  const [query, setQuery] = useState("");
  const [statusFilter, setStatusFilter] = useState<"ALL" | StatusKey>("ALL");
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const controller = new AbortController();
    void fetch("/api/v1/admin/datasets", { signal: controller.signal })
      .then(async (response) => {
        if (!response.ok) throw new Error(await errorText(response));
        if (!controller.signal.aborted) setItems(await response.json() as Dataset[]);
      })
      .catch((error) => {
        if (!controller.signal.aborted) setMessage(error instanceof Error ? error.message : "Falha ao carregar datasets.");
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });
    return () => controller.abort();
  }, []);

  const selectedId = selected?.id;
  useEffect(() => {
    if (!selectedId) {
      setVersions([]);
      return;
    }
    const controller = new AbortController();
    void fetch(`/api/v1/admin/datasets/${selectedId}/versions`, { signal: controller.signal })
      .then(async (response) => {
        if (!response.ok) throw new Error(await errorText(response));
        if (!controller.signal.aborted) setVersions(await response.json() as DatasetVersion[]);
      })
      .catch((error) => {
        if (!controller.signal.aborted) setMessage(error instanceof Error ? error.message : "Falha ao carregar versões.");
      });
    return () => controller.abort();
  }, [selectedId]);

  const filtered = useMemo(() => {
    const normalizedQuery = query.trim().toLocaleLowerCase("pt-BR");
    return items.filter((item) => {
      if (statusFilter !== "ALL" && datasetStatus(item.status) !== statusFilter) return false;
      if (!normalizedQuery) return true;
      return [item.title, item.slug, item.category, item.responsibleDepartment]
        .some((value) => value.toLocaleLowerCase("pt-BR").includes(normalizedQuery));
    });
  }, [items, query, statusFilter]);

  async function refresh(preferredId?: string) {
    const response = await fetch("/api/v1/admin/datasets");
    if (!response.ok) throw new Error(await errorText(response));
    const next = await response.json() as Dataset[];
    setItems(next);
    const id = preferredId ?? selected?.id;
    setSelected(id ? next.find((item) => item.id === id) ?? null : null);
  }

  async function refreshVersions(id: string) {
    const response = await fetch(`/api/v1/admin/datasets/${id}/versions`);
    if (!response.ok) throw new Error(await errorText(response));
    setVersions(await response.json() as DatasetVersion[]);
  }

  async function createDataset(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formElement = event.currentTarget;
    const form = new FormData(formElement);
    setBusy(true);
    setMessage("");
    try {
      const response = await fetch("/api/v1/admin/datasets", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(datasetPayload(form)),
      });
      if (!response.ok) throw new Error(await errorText(response));
      const created = await response.json() as Dataset;
      formElement.reset();
      await refresh(created.id);
      setMessage("Dataset criado como rascunho.");
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "Não foi possível criar o dataset.");
    } finally {
      setBusy(false);
    }
  }

  async function updateDataset(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selected) return;
    const form = new FormData(event.currentTarget);
    setBusy(true);
    setMessage("");
    try {
      const response = await fetch(`/api/v1/admin/datasets/${selected.id}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(datasetPayload(form, false)),
      });
      if (!response.ok) throw new Error(await errorText(response));
      await refresh(selected.id);
      setMessage("Metadados do dataset atualizados.");
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "Não foi possível atualizar o dataset.");
    } finally {
      setBusy(false);
    }
  }

  async function uploadVersion(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selected) return;
    const formElement = event.currentTarget;
    const form = new FormData(formElement);
    const file = form.get("file");
    if (!(file instanceof File) || file.size === 0) {
      setMessage("Selecione um arquivo CSV, JSON, XLSX ou PDF.");
      return;
    }
    setBusy(true);
    setMessage("");
    try {
      const response = await fetch(`/api/v1/admin/datasets/${selected.id}/versions`, { method: "POST", body: form });
      if (!response.ok) throw new Error(await errorText(response));
      formElement.reset();
      await Promise.all([refresh(selected.id), refreshVersions(selected.id)]);
      setMessage("Nova versão armazenada e registrada com hash SHA-256.");
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "Não foi possível publicar a versão.");
    } finally {
      setBusy(false);
    }
  }

  async function transition(action: "publish" | "archive") {
    if (!selected) return;
    setBusy(true);
    setMessage("");
    try {
      const response = await fetch(`/api/v1/admin/datasets/${selected.id}/${action}`, { method: "POST" });
      if (!response.ok) throw new Error(await errorText(response));
      await refresh(selected.id);
      setMessage(action === "publish" ? "Dataset publicado no catálogo público." : "Dataset arquivado.");
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "A ação não pôde ser concluída.");
    } finally {
      setBusy(false);
    }
  }

  return <>
    <div className="editor-grid">
      <section className="admin-panel">
        <div className="resource-toolbar">
          <label>Buscar <input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Título, slug, categoria ou órgão" /></label>
          <label>Status <select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value as "ALL" | StatusKey)}><option value="ALL">Todos</option><option value="DRAFT">Rascunhos</option><option value="PUBLISHED">Publicados</option><option value="ARCHIVED">Arquivados</option></select></label>
        </div>
        {loading ? <div className="empty-state" aria-busy="true"><h3>Carregando datasets…</h3></div> : filtered.length === 0 ? <div className="empty-state"><h3>Nenhum dataset encontrado</h3><p>Crie o primeiro dataset ou ajuste os filtros.</p></div> : <div className="compact-list">{filtered.map((item) => <button type="button" className="compact-item" key={item.id} onClick={() => setSelected(item)} style={{ width: "100%", cursor: "pointer" }}><span><strong>{item.title}</strong><small style={{ display: "block" }}>{item.category || "Sem categoria"} · {item.responsibleDepartment || "Órgão não informado"}</small></span><span className="status-pill">{statusLabel(item.status)}</span></button>)}</div>}
      </section>

      <form className="admin-panel editor-fields" onSubmit={createDataset}>
        <h2>Novo dataset</h2>
        <label className="field">Título do dataset<input name="title" required maxLength={220} /></label>
        <label className="field">Slug do dataset<input name="slug" required pattern="[a-z0-9]+(?:-[a-z0-9]+)*" placeholder="ex.: despesas-2026" /></label>
        <label className="field">Descrição<textarea name="description" rows={4} required /></label>
        <label className="field">Categoria<input name="category" required placeholder="Finanças, Saúde, Educação…" /></label>
        <label className="field">Órgão responsável<input name="responsibleDepartment" required /></label>
        <label className="field">Licença<input name="license" required defaultValue="Dados Abertos" /></label>
        <label className="field">Periodicidade de atualização<input name="updateFrequency" required placeholder="Mensal, anual, sob demanda…" /></label>
        <label className="field">Período de referência<input name="referencePeriod" placeholder="2026 / 1º semestre / agosto de 2026" /></label>
        <label className="field">Fonte<input name="source" placeholder="Secretaria, sistema de origem ou URL institucional" /></label>
        <label className="field">Próxima atualização prevista<input name="nextExpectedUpdateAt" type="date" /></label>
        <button className="action-button" disabled={busy}>Criar dataset</button>
      </form>
    </div>

    {selected && <section className="admin-panel" style={{ marginTop: 20 }}>
      <div className="admin-heading">
        <div><span className="kicker">{statusLabel(selected.status)}</span><h2>{selected.title}</h2><p><code>{selected.slug}</code> · criado em {formatDate(selected.createdAt)}</p></div>
        <div className="button-row">
          {datasetStatus(selected.status) === "PUBLISHED" && <Link className="action-button secondary" href={`/dados-abertos/${selected.slug}`} target="_blank">Ver público ↗</Link>}
          {datasetStatus(selected.status) !== "ARCHIVED" && <button type="button" className="action-button secondary" onClick={() => void transition("archive")} disabled={busy}>Arquivar</button>}
          {datasetStatus(selected.status) !== "ARCHIVED" && <button type="button" className="action-button" onClick={() => void transition("publish")} disabled={busy || versions.length === 0}>Publicar dataset</button>}
        </div>
      </div>

      <div className="editor-grid">
        <form className="editor-fields" key={`edit-${selected.id}-${selected.updatedAt}`} onSubmit={updateDataset}>
          <h3>Metadados</h3>
          <label className="field">Título<input name="title" defaultValue={selected.title} required /></label>
          <label className="field">Descrição<textarea name="description" rows={4} defaultValue={selected.description} required /></label>
          <label className="field">Categoria<input name="category" defaultValue={selected.category} required /></label>
          <label className="field">Órgão responsável<input name="responsibleDepartment" defaultValue={selected.responsibleDepartment} required /></label>
          <label className="field">Licença<input name="license" defaultValue={selected.license} required /></label>
          <label className="field">Periodicidade de atualização<input name="updateFrequency" defaultValue={selected.updateFrequency} required /></label>
          <label className="field">Período de referência<input name="referencePeriod" defaultValue={selected.referencePeriod ?? ""} /></label>
          <label className="field">Fonte<input name="source" defaultValue={selected.source ?? ""} /></label>
          <label className="field">Próxima atualização prevista<input name="nextExpectedUpdateAt" type="date" defaultValue={dateInputValue(selected.nextExpectedUpdateAt)} /></label>
          <button className="action-button secondary" disabled={busy || datasetStatus(selected.status) === "ARCHIVED"}>Salvar metadados</button>
        </form>

        <div>
          <form className="editor-fields" onSubmit={uploadVersion}>
            <h3>Nova versão</h3>
            <label className="field">Arquivo da versão<input name="file" type="file" accept=".csv,.json,.xlsx,.pdf" required disabled={datasetStatus(selected.status) === "ARCHIVED"} /></label>
            <label className="field">Metadados da versão (JSON)<textarea name="metadataJson" rows={4} defaultValue="{}" /></label>
            <small>Máximo de 50 MB. O servidor valida o conteúdo real do arquivo e impede versões duplicadas por SHA-256.</small>
            <button className="action-button secondary" disabled={busy || datasetStatus(selected.status) === "ARCHIVED"}>Adicionar versão</button>
          </form>
          <div style={{ marginTop: 20 }}>
            <h3>Histórico de versões</h3>
            {versions.length === 0 ? <div className="empty-state"><p>Nenhuma versão enviada. É necessário adicionar ao menos uma antes da publicação.</p></div> : <div className="compact-list">{versions.map((version) => <div className="compact-item" key={version.id}><div><strong>v{version.version} · {version.fileName}</strong><small style={{ display: "block" }}>{version.format} · {formatBytes(version.sizeBytes)} · {formatDate(version.publishedAt)}</small><small style={{ display: "block" }}>SHA-256 <code>{version.sha256.slice(0, 20)}…</code></small></div>{datasetStatus(selected.status) === "PUBLISHED" && <a className="action-button secondary" href={`/api/v1/public/datasets/${selected.id}/versions/${version.version}/download`}>Baixar</a>}</div>)}</div>}
          </div>
        </div>
      </div>
    </section>}

    {message && <div className="form-message" role="status" style={{ marginTop: 16 }}>{message}</div>}
  </>;
}

function datasetPayload(form: FormData, includeSlug = true) {
  const nextExpectedUpdateAt = String(form.get("nextExpectedUpdateAt") ?? "").trim();
  return {
    ...(includeSlug ? { slug: String(form.get("slug") ?? "").trim() } : {}),
    title: String(form.get("title") ?? "").trim(),
    description: String(form.get("description") ?? "").trim(),
    category: String(form.get("category") ?? "").trim(),
    responsibleDepartment: String(form.get("responsibleDepartment") ?? "").trim(),
    license: String(form.get("license") ?? "").trim(),
    updateFrequency: String(form.get("updateFrequency") ?? "").trim(),
    referencePeriod: emptyToNull(form.get("referencePeriod")),
    source: emptyToNull(form.get("source")),
    nextExpectedUpdateAt: nextExpectedUpdateAt ? new Date(`${nextExpectedUpdateAt}T12:00:00Z`).toISOString() : null,
  };
}

function emptyToNull(value: FormDataEntryValue | null) {
  const text = String(value ?? "").trim();
  return text || null;
}

function datasetStatus(value: string | number): StatusKey {
  if (value === 1 || String(value).toUpperCase() === "PUBLISHED") return "PUBLISHED";
  if (value === 2 || String(value).toUpperCase() === "ARCHIVED") return "ARCHIVED";
  return "DRAFT";
}

function statusLabel(value: string | number) {
  const status = datasetStatus(value);
  return status === "PUBLISHED" ? "Publicado" : status === "ARCHIVED" ? "Arquivado" : "Rascunho";
}

function formatDate(value: string | null) {
  if (!value) return "—";
  return new Intl.DateTimeFormat("pt-BR", { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));
}

function dateInputValue(value: string | null) {
  return value ? new Date(value).toISOString().slice(0, 10) : "";
}

function formatBytes(value: number) {
  if (value < 1024) return `${value} B`;
  if (value < 1024 * 1024) return `${(value / 1024).toFixed(1)} KB`;
  return `${(value / (1024 * 1024)).toFixed(1)} MB`;
}

async function errorText(response: Response) {
  const body = await response.json().catch(() => null) as { title?: string; detail?: string; errors?: Record<string, string[]> } | null;
  const validation = Object.values(body?.errors ?? {}).flat().join(" ");
  return body?.detail ?? body?.title ?? (validation || `Erro ${response.status}`);
}
