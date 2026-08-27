"use client";

import { useEffect, useMemo, useState, type FormEvent, type ReactNode } from "react";
import { MediaPicker, RichTextEditor, type MediaPickerItem } from "@/components/ui";
import { NEWS_CATEGORIES } from "@/lib/news-categories";

type Draft = { title: string; slug: string; summary: string; body: string; category: string; coverImageUrl: string; coverImageAlt: string; isFeatured: boolean };
type Article = { id: string; status: string; version: number; verificationCode?: string };
type Asset = { id: string; originalFileName: string; mimeType: string; status: string; altText: string };
const EMPTY_DRAFT: Draft = { title: "", slug: "", summary: "", body: "", category: "GERAL", coverImageUrl: "", coverImageAlt: "", isFeatured: false };
const STORAGE_KEY = "deodapolis.news.draft";

export function NewsEditor() {
  const [draft, setDraft] = useState<Draft>(EMPTY_DRAFT);
  const [article, setArticle] = useState<Article | null>(null);
  const [media, setMedia] = useState<Asset[]>([]);
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    const saved = localStorage.getItem(STORAGE_KEY);
    if (!saved) return;
    const timer = window.setTimeout(() => {
      try { setDraft({ ...EMPTY_DRAFT, ...JSON.parse(saved) as Partial<Draft> }); }
      catch { localStorage.removeItem(STORAGE_KEY); }
    }, 0);
    return () => window.clearTimeout(timer);
  }, []);

  useEffect(() => {
    const controller = new AbortController();
    void fetch("/api/v1/admin/media", { signal: controller.signal }).then(async (response) => {
      if (response.ok && !controller.signal.aborted) setMedia(await response.json() as Asset[]);
    }).catch(() => undefined);
    return () => controller.abort();
  }, []);

  useEffect(() => { localStorage.setItem(STORAGE_KEY, JSON.stringify(draft)); }, [draft]);
  const canCreate = useMemo(() => Boolean(draft.title && draft.slug && draft.summary && draft.body), [draft]);
  const imageMedia: MediaPickerItem[] = useMemo(() => media.filter((asset) => asset.status === "APPROVED" && asset.mimeType.startsWith("image/")).map((asset) => ({ id: asset.id, name: asset.originalFileName, mimeType: asset.mimeType, altText: asset.altText, status: asset.status })), [media]);
  const selectedMediaId = draft.coverImageUrl.startsWith("/api/v1/media/") ? draft.coverImageUrl.split("/").at(-1) : undefined;

  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    const response = await fetch("/api/v1/admin/news", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ ...draft, coverImageUrl: draft.coverImageUrl || null, coverImageAlt: draft.coverImageAlt || null }) });
    if (response.ok) {
      const created = await response.json() as Article;
      setArticle(created);
      setMessage("Rascunho salvo no servidor. Agora você pode enviar para revisão.");
      localStorage.removeItem(STORAGE_KEY);
    } else setMessage(await errorText(response));
    setBusy(false);
  }

  async function action(name: string, body?: unknown) {
    if (!article) return;
    setBusy(true);
    const response = await fetch(`/api/v1/admin/news/${article.id}/${name}`, { method: "POST", headers: { "Content-Type": "application/json" }, body: body ? JSON.stringify(body) : undefined });
    if (response.ok) {
      setArticle(await response.json() as Article);
      setMessage(`Ação “${name}” concluída.`);
    } else setMessage(await errorText(response));
    setBusy(false);
  }

  return <div className="editor-grid">
    <form className="admin-panel editor-fields" onSubmit={create}>
      <h2>Nova notícia</h2>
      <Field label="Título"><input value={draft.title} onChange={(event) => setDraft({ ...draft, title: event.target.value })} maxLength={180} required /></Field>
      <Field label="Slug"><input value={draft.slug} onChange={(event) => setDraft({ ...draft, slug: event.target.value.toLowerCase().replace(/[^a-z0-9-]/g, "-") })} maxLength={180} required /></Field>
      <Field label="Linha fina"><textarea value={draft.summary} onChange={(event) => setDraft({ ...draft, summary: event.target.value })} maxLength={320} rows={3} required /></Field>
      <Field label="Área editorial"><select value={draft.category} onChange={(event) => setDraft({ ...draft, category: event.target.value })}>{NEWS_CATEGORIES.map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></Field>
      <RichTextEditor label="Conteúdo" value={draft.body} onChange={(body) => setDraft({ ...draft, body })} required />
      <details className="rounded-xl border border-border p-3"><summary className="cursor-pointer font-semibold">Selecionar capa da biblioteca</summary><div className="mt-3">{imageMedia.length > 0 ? <MediaPicker items={imageMedia} selectedId={selectedMediaId} onSelect={(item) => setDraft({ ...draft, coverImageUrl: `/api/v1/media/${item.id}`, coverImageAlt: item.altText || item.name })} /> : <p className="text-muted">Nenhuma imagem aprovada disponível. Envie e aprove a mídia na biblioteca antes de selecioná-la.</p>}</div></details>
      <Field label="URL da imagem de capa (opcional)"><input value={draft.coverImageUrl} onChange={(event) => setDraft({ ...draft, coverImageUrl: event.target.value })} /></Field>
      <Field label="Texto alternativo"><input value={draft.coverImageAlt} onChange={(event) => setDraft({ ...draft, coverImageAlt: event.target.value })} /></Field>
      <label><input type="checkbox" checked={draft.isFeatured} onChange={(event) => setDraft({ ...draft, isFeatured: event.target.checked })} /> Destaque na home</label>
      <small>Recuperação local do rascunho fica ativa enquanto a notícia ainda não foi criada no servidor.</small>
      <button className="action-button" disabled={!canCreate || busy || Boolean(article)}>{busy ? "Salvando…" : "Criar rascunho"}</button>
    </form>
    <aside className="admin-panel">
      <h2>Workflow editorial</h2>
      {article ? <><p><span className="status-pill">{article.status}</span> · versão {article.version}</p><div className="button-row">
        <button type="button" className="action-button secondary" disabled={busy || article.status !== "DRAFT"} onClick={() => action("submit")}>Enviar para revisão</button>
        <button type="button" className="action-button secondary" disabled={busy || article.status !== "IN_REVIEW"} onClick={() => action("approve")}>Aprovar</button>
        <button type="button" className="action-button" disabled={busy || !["APPROVED", "SCHEDULED"].includes(article.status)} onClick={() => action("publish")}>Publicar agora</button>
      </div><Schedule onSchedule={(date) => action("schedule", { publishAt: date })} disabled={busy || article.status !== "APPROVED"} /></> : <p>Crie o rascunho para liberar as etapas de revisão e publicação.</p>}
      {message && <div className="form-message" role="status">{message}</div>}
      <h3>Checklist de qualidade</h3><ul><li>Título objetivo</li><li>Linha fina preenchida</li><li>Imagem com ALT quando houver</li><li>Links revisados</li><li>Responsável e categoria definidos no processo editorial</li></ul>
    </aside>
  </div>;
}

function Field({ label, children }: { label: string; children: ReactNode }) { return <label className="field"><span>{label}</span>{children}</label>; }
function Schedule({ onSchedule, disabled }: { onSchedule: (value: string) => void; disabled: boolean }) { const [value, setValue] = useState(""); return <div className="field" style={{ marginTop: 18 }}><label htmlFor="schedule">Agendar publicação</label><input id="schedule" type="datetime-local" value={value} onChange={(event) => setValue(event.target.value)} /><button type="button" className="action-button secondary" disabled={disabled || !value} onClick={() => onSchedule(new Date(value).toISOString())}>Agendar</button></div>; }
async function errorText(response: Response) { const body = await response.json().catch(() => null) as { title?: string; detail?: string; errors?: Record<string, string[]> } | null; const validation = Object.values(body?.errors ?? {}).flat().join(" "); return body?.detail ?? body?.title ?? (validation || `Erro ${response.status}`); }
