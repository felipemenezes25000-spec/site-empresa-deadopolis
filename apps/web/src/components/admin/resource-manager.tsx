"use client";

import { useEffect, useState, type FormEvent } from "react";

type Resource = { id: string; kind: string; slug: string; title: string; summary: string; status: string; displayOrder: number; version: number };
const kinds = ["PAGE", "BANNER", "EVENT", "LEGISLATION", "DATASET", "LOCATION", "CONTACT", "ALERT", "MENU", "HOME_BLOCK", "PROCUREMENT_LINK", "ESIC_LINK", "OUVIDORIA_LINK"];

export function ResourceManager() {
  const [items, setItems] = useState<Resource[]>([]);
  const [kind, setKind] = useState("PAGE");
  const [message, setMessage] = useState("");

  useEffect(() => {
    const controller = new AbortController();
    void fetch(`/api/v1/admin/resources?kind=${encodeURIComponent(kind)}`, { signal: controller.signal })
      .then(async (response) => {
        if (response.ok && !controller.signal.aborted) setItems(await response.json() as Resource[]);
      })
      .catch(() => undefined);
    return () => controller.abort();
  }, [kind]);

  async function load() {
    const response = await fetch(`/api/v1/admin/resources?kind=${encodeURIComponent(kind)}`);
    if (response.ok) setItems(await response.json() as Resource[]);
  }

  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const payload = { kind, slug: form.get("slug"), title: form.get("title"), summary: form.get("summary"), payloadJson: String(form.get("payloadJson") || "{}"), displayOrder: Number(form.get("displayOrder") || 0), startsAt: null, endsAt: null };
    const response = await fetch("/api/v1/admin/resources", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(payload) });
    setMessage(response.ok ? "Conteúdo criado como rascunho." : await errorText(response));
    if (response.ok) { event.currentTarget.reset(); await load(); }
  }

  async function transition(id: string, action: string) {
    const response = await fetch(`/api/v1/admin/resources/${id}/${action}`, { method: "POST" });
    setMessage(response.ok ? `Ação ${action} concluída.` : await errorText(response));
    await load();
  }

  return <div className="editor-grid">
    <section className="admin-panel">
      <div className="resource-toolbar"><label>Tipo <select value={kind} onChange={(event) => setKind(event.target.value)}>{kinds.map((value) => <option key={value}>{value}</option>)}</select></label></div>
      {items.length === 0 ? <div className="empty-state"><h3>Nenhum conteúdo deste tipo</h3><p>Crie um item no formulário ao lado.</p></div> : <div className="compact-list">{items.map((item) => <div className="compact-item" key={item.id}><div><strong>{item.title}</strong><small style={{ display: "block" }}>{item.slug} · v{item.version}</small></div><div className="button-row"><span className="status-pill">{item.status}</span>{item.status !== "PUBLISHED" && item.status !== "ARCHIVED" && <button type="button" className="action-button" onClick={() => transition(item.id, "publish")}>Publicar</button>}{item.status !== "ARCHIVED" && <button type="button" className="action-button secondary" onClick={() => transition(item.id, "archive")}>Arquivar</button>}{item.status === "ARCHIVED" && <button type="button" className="action-button secondary" onClick={() => transition(item.id, "restore")}>Restaurar</button>}</div></div>)}</div>}
      {message && <div className="form-message">{message}</div>}
    </section>
    <form className="admin-panel editor-fields" onSubmit={create}>
      <h2>Novo conteúdo</h2>
      <label className="field">Título<input name="title" required maxLength={220} /></label>
      <label className="field">Slug<input name="slug" required pattern="[a-z0-9-]+" /></label>
      <label className="field">Resumo<textarea name="summary" rows={3} maxLength={500} /></label>
      <label className="field">Ordem<input name="displayOrder" type="number" defaultValue={0} /></label>
      <label className="field">Detalhes estruturados (JSON)<textarea name="payloadJson" rows={8} defaultValue="{}" /><small>Campo avançado. Formulários especializados podem ser adicionados sem alterar o modelo de persistência.</small></label>
      <button className="action-button">Salvar rascunho</button>
    </form>
  </div>;
}

async function errorText(response: Response) { const body = await response.json().catch(() => null) as { title?: string; detail?: string; errors?: Record<string, string[]> } | null; const validation = Object.values(body?.errors ?? {}).flat().join(" "); return body?.detail ?? body?.title ?? (validation || `Erro ${response.status}`); }
