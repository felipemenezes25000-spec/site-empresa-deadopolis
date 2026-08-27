"use client";

import { useEffect, useMemo, useState, type FormEvent } from "react";
import { SearchField } from "@/components/ui";

type Asset = { id: string; originalFileName: string; mimeType: string; sizeBytes: number; status: string; altText: string; uploadedAt: string };

export function MediaManager() {
  const [items, setItems] = useState<Asset[]>([]);
  const [message, setMessage] = useState("");
  const [query, setQuery] = useState("");
  const filtered = useMemo(() => {
    const normalized = query.trim().toLocaleLowerCase("pt-BR");
    if (!normalized) return items;
    return items.filter((item) => `${item.originalFileName} ${item.mimeType} ${item.altText ?? ""} ${item.status}`.toLocaleLowerCase("pt-BR").includes(normalized));
  }, [items, query]);

  useEffect(() => {
    const controller = new AbortController();
    void fetch("/api/v1/admin/media", { signal: controller.signal })
      .then(async (response) => {
        if (response.ok && !controller.signal.aborted) setItems(await response.json() as Asset[]);
      })
      .catch(() => undefined);
    return () => controller.abort();
  }, []);

  async function load() {
    const response = await fetch("/api/v1/admin/media");
    if (response.ok) setItems(await response.json() as Asset[]);
  }

  async function upload(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const response = await fetch("/api/v1/admin/media/upload", { method: "POST", body: data });
    const body = await response.json().catch(() => null) as { status?: string; scan?: { scannerState?: string; state?: string; detail?: string }; detail?: string; title?: string } | null;
    setMessage(response.ok ? `Arquivo recebido. Status: ${body?.status ?? "—"}. Scanner: ${body?.scan?.scannerState ?? body?.scan?.state ?? "—"}.` : body?.detail ?? body?.title ?? `Erro ${response.status}`);
    if (response.ok) {
      event.currentTarget.reset();
      await load();
    }
  }

  return <div className="editor-grid">
    <form className="admin-panel editor-fields" onSubmit={upload}>
      <h2>Enviar mídia</h2>
      <label className="field">Arquivo<input name="file" type="file" accept="image/jpeg,image/png,image/webp,application/pdf" required /></label>
      <label className="field">Texto alternativo<input name="altText" /></label>
      <label className="field">Legenda<input name="caption" /></label>
      <label className="field">Crédito<input name="credit" /></label>
      <button className="action-button">Enviar</button>
      {message && <div className="form-message">{message}</div>}
      <small>O backend valida bytes reais, tamanho, SHA-256 e mantém quarentena quando o scanner de produção não está configurado.</small>
    </form>
    <section className="admin-panel"><h2>Biblioteca</h2><SearchField value={query} onChange={setQuery} label="Filtrar mídia" placeholder="Nome, tipo, texto alternativo ou status" /><div className="compact-list">{filtered.map((item) => <div className="compact-item" key={item.id}><div><strong>{item.originalFileName}</strong><small style={{ display: "block" }}>{item.mimeType} · {(item.sizeBytes / 1024).toFixed(0)} KB</small></div><span className="status-pill">{item.status}</span></div>)}</div>{filtered.length === 0 && <p className="text-muted">Nenhuma mídia corresponde ao filtro.</p>}</section>
  </div>;
}
