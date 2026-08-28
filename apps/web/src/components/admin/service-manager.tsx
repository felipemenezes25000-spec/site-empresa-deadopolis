"use client";

import { useEffect, useState, type FormEvent } from "react";

type Service = { id: string; name: string; slug: string; area: string; status: string; isFeatured: boolean };

export function ServiceManager() {
  const [items, setItems] = useState<Service[]>([]);
  const [message, setMessage] = useState("");
  const [listState, setListState] = useState<"LOADING" | "READY" | "ERROR">("LOADING");
  const [reloadToken, setReloadToken] = useState(0);

  useEffect(() => {
    const controller = new AbortController();
    void fetch("/api/v1/admin/services", { signal: controller.signal })
      .then(async (response) => {
        if (!response.ok) throw new Error("services");
        const next = await response.json() as Service[];
        if (controller.signal.aborted) return;
        setItems(next);
        setListState("READY");
      })
      .catch(() => { if (!controller.signal.aborted) setListState("ERROR"); });
    return () => controller.abort();
  }, [reloadToken]);

  function load() {
    setReloadToken((current) => current + 1);
  }

  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const payload = { name: form.get("name"), slug: form.get("slug"), description: form.get("description"), area: form.get("area"), audience: form.get("audience"), departmentId: null, requirements: form.get("requirements"), documents: form.get("documents"), steps: form.get("steps"), expectedDuration: form.get("duration"), cost: form.get("cost"), channels: form.get("channels"), isOnline: form.get("isOnline") === "on", onlineUrl: form.get("onlineUrl") || null, phone: "", address: "", openingHours: "", legalBasis: "", isFeatured: form.get("featured") === "on", published: true };
    const response = await fetch("/api/v1/admin/services", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(payload) });
    setMessage(response.ok ? "Serviço publicado no catálogo." : `Não foi possível salvar (${response.status}).`);
    if (response.ok) { event.currentTarget.reset(); load(); }
  }

  return <div className="editor-grid">
    <section className="admin-panel"><h2>Catálogo atual</h2>
      {listState === "LOADING" && <p role="status" aria-live="polite">Carregando catálogo de serviços…</p>}
      {listState === "ERROR" && <div className="form-message error" role="alert">Não foi possível carregar o catálogo. <button type="button" className="action-button secondary" onClick={load}>Tentar novamente</button></div>}
      {listState === "READY" && items.length === 0 && <div className="empty-state"><h3>Nenhum serviço cadastrado</h3><p>Publique o primeiro serviço no formulário ao lado.</p></div>}
      {listState === "READY" && items.length > 0 && <div className="compact-list">{items.map((item) => <div className="compact-item" key={item.id}><div><strong>{item.name}</strong><small style={{ display: "block" }}>{item.area} · /{item.slug}</small></div><span className="status-pill">{item.status}</span></div>)}</div>}
    </section>
    <form className="admin-panel editor-fields" onSubmit={create}>
      <h2>Novo serviço</h2>
      <label className="field">Nome<input name="name" required /></label><label className="field">Slug<input name="slug" required pattern="[a-z0-9-]+" /></label><label className="field">Descrição<textarea name="description" required rows={3} /></label><label className="field">Área<input name="area" required placeholder="Saúde, Educação, Tributos…" /></label><label className="field">Público<input name="audience" required /></label><label className="field">Requisitos<textarea name="requirements" rows={2} /></label><label className="field">Documentos<textarea name="documents" rows={2} /></label><label className="field">Etapas<textarea name="steps" rows={3} /></label><label className="field">Prazo<input name="duration" /></label><label className="field">Custo<input name="cost" defaultValue="Gratuito" /></label><label className="field">Canais<input name="channels" /></label><label><input name="isOnline" type="checkbox" /> Disponível online</label><label className="field">URL online<input name="onlineUrl" type="url" /></label><label><input name="featured" type="checkbox" /> Destacar</label><button className="action-button">Publicar serviço</button>{message && <div className="form-message">{message}</div>}
    </form>
  </div>;
}
