"use client";

import { useEffect, useMemo, useState, type FormEvent, type ReactNode } from "react";
import { MediaPicker, RichTextEditor, type MediaPickerItem } from "@/components/ui";
import { NEWS_CATEGORIES } from "@/lib/news-categories";

type Draft = { title: string; slug: string; summary: string; body: string; category: string; coverImageUrl: string; coverImageAlt: string; isFeatured: boolean };
type Article = Draft & { id: string; status: string; version: number; verificationCode?: string };
type ArticleResponse = Omit<Article, "coverImageUrl" | "coverImageAlt"> & { coverImageUrl: string | null; coverImageAlt: string | null };
type Asset = { id: string; originalFileName: string; mimeType: string; status: string; altText: string };
const EMPTY_DRAFT: Draft = { title: "", slug: "", summary: "", body: "", category: "GERAL", coverImageUrl: "", coverImageAlt: "", isFeatured: false };
const STORAGE_KEY = "deodapolis.news.draft";

export function NewsEditor({ articleId }: { articleId?: string }) {
  const [draft, setDraft] = useState<Draft>(EMPTY_DRAFT);
  const [article, setArticle] = useState<Article | null>(null);
  const [media, setMedia] = useState<Asset[]>([]);
  const [mediaState, setMediaState] = useState<"LOADING" | "READY" | "ERROR">("LOADING");
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);
  const [loadingArticle, setLoadingArticle] = useState(Boolean(articleId));
  const [loadError, setLoadError] = useState("");

  useEffect(() => {
    if (articleId) return;
    const saved = localStorage.getItem(STORAGE_KEY);
    if (!saved) return;
    const timer = window.setTimeout(() => {
      try { setDraft({ ...EMPTY_DRAFT, ...JSON.parse(saved) as Partial<Draft> }); }
      catch { localStorage.removeItem(STORAGE_KEY); }
    }, 0);
    return () => window.clearTimeout(timer);
  }, [articleId]);

  useEffect(() => {
    if (!articleId) return;
    const controller = new AbortController();
    void fetch(`/api/v1/admin/news/${articleId}`, { signal: controller.signal })
      .then(async (response) => {
        if (!response.ok) throw new Error(await errorText(response));
        const loaded = await response.json() as ArticleResponse;
        if (controller.signal.aborted) return;
        const normalized = { ...loaded, coverImageUrl: loaded.coverImageUrl ?? "", coverImageAlt: loaded.coverImageAlt ?? "" };
        setArticle(normalized);
        setDraft(normalized);
      })
      .catch((error) => {
        if (!controller.signal.aborted) setLoadError(error instanceof Error ? error.message : "Não foi possível abrir a notícia.");
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoadingArticle(false);
      });
    return () => controller.abort();
  }, [articleId]);

  useEffect(() => {
    const controller = new AbortController();
    void fetch("/api/v1/admin/media?status=APPROVED&pageSize=100", { signal: controller.signal }).then(async (response) => {
      if (!response.ok) throw new Error("media");
      const assets = await response.json() as Asset[];
      if (controller.signal.aborted) return;
      setMedia(assets);
      setMediaState("READY");
    }).catch(() => { if (!controller.signal.aborted) setMediaState("ERROR"); });
    return () => controller.abort();
  }, []);

  useEffect(() => { if (!articleId) localStorage.setItem(STORAGE_KEY, JSON.stringify(draft)); }, [articleId, draft]);
  const canCreate = useMemo(() => Boolean(draft.title && draft.slug && draft.summary && draft.body), [draft]);
  const imageMedia: MediaPickerItem[] = useMemo(() => media.filter((asset) => asset.status === "APPROVED" && asset.mimeType.startsWith("image/")).map((asset) => ({ id: asset.id, name: asset.originalFileName, mimeType: asset.mimeType, altText: asset.altText, status: asset.status })), [media]);
  const selectedMediaId = draft.coverImageUrl.startsWith("/api/v1/media/") ? draft.coverImageUrl.split("/").at(-1) : undefined;

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    const editing = Boolean(article);
    const response = await fetch(editing ? `/api/v1/admin/news/${article!.id}` : "/api/v1/admin/news", {
      method: editing ? "PUT" : "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        ...draft,
        coverImageUrl: draft.coverImageUrl || null,
        coverImageAlt: draft.coverImageAlt || null,
        ...(editing ? { expectedVersion: article!.version } : {}),
      }),
    });
    if (response.ok) {
      const result = await response.json() as ArticleResponse;
      const normalized = { ...result, coverImageUrl: result.coverImageUrl ?? "", coverImageAlt: result.coverImageAlt ?? "" };
      setArticle(normalized);
      setDraft(normalized);
      setMessage(editing ? `Alterações salvas como versão ${result.version}.` : "Rascunho salvo no servidor. Agora você pode enviar para revisão.");
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

  if (loadingArticle) return <div className="admin-panel empty-state" aria-busy="true"><h2>Carregando notícia…</h2></div>;
  if (loadError) return <div className="admin-panel form-message error" role="alert"><h2>Notícia indisponível</h2><p>{loadError}</p></div>;

  const immutable = article?.status === "PUBLISHED" || article?.status === "ARCHIVED";

  return <div className="editor-grid">
    <form className="admin-panel editor-fields" onSubmit={save}>
      <h2>{articleId ? "Editar notícia" : "Nova notícia"}</h2>
      <Field label="Título"><input value={draft.title} onChange={(event) => setDraft({ ...draft, title: event.target.value })} maxLength={180} required /></Field>
      <Field label="Slug"><input value={draft.slug} onChange={(event) => setDraft({ ...draft, slug: event.target.value.toLowerCase().replace(/[^a-z0-9-]/g, "-") })} maxLength={180} disabled={Boolean(articleId)} required /><small>O endereço fica estável após a criação para preservar links públicos.</small></Field>
      <Field label="Linha fina"><textarea value={draft.summary} onChange={(event) => setDraft({ ...draft, summary: event.target.value })} maxLength={320} rows={3} required /></Field>
      <Field label="Área editorial"><select value={draft.category} onChange={(event) => setDraft({ ...draft, category: event.target.value })}>{NEWS_CATEGORIES.map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></Field>
      <RichTextEditor label="Conteúdo" value={draft.body} onChange={(body) => setDraft({ ...draft, body })} required />
      <details className="rounded-xl border border-border p-3"><summary className="cursor-pointer font-semibold">Selecionar capa da biblioteca</summary><div className="mt-3">{mediaState === "LOADING" ? <p role="status" aria-live="polite">Carregando biblioteca de mídia…</p> : mediaState === "ERROR" ? <p className="form-message error" role="alert">Não foi possível carregar a biblioteca de mídia; a lista abaixo pode estar incompleta. Recarregue a página antes de escolher uma capa.</p> : imageMedia.length > 0 ? <MediaPicker items={imageMedia} selectedId={selectedMediaId} onSelect={(item) => setDraft({ ...draft, coverImageUrl: `/api/v1/media/${item.id}`, coverImageAlt: item.altText || item.name })} /> : <p className="text-muted">Nenhuma imagem aprovada disponível. Envie e aprove a mídia na biblioteca antes de selecioná-la.</p>}</div></details>
      {draft.coverImageUrl && <div className="compact-item" aria-live="polite"><span><strong>Capa selecionada</strong><small style={{ display: "block" }}>{draft.coverImageUrl}</small></span><button type="button" className="action-button secondary" onClick={() => setDraft({ ...draft, coverImageUrl: "", coverImageAlt: "" })}>Remover capa</button></div>}
      <Field label="Texto alternativo"><input value={draft.coverImageAlt} onChange={(event) => setDraft({ ...draft, coverImageAlt: event.target.value })} /></Field>
      <label><input type="checkbox" checked={draft.isFeatured} onChange={(event) => setDraft({ ...draft, isFeatured: event.target.checked })} /> Destaque na home</label>
      {!articleId && <small>Recuperação local do rascunho fica ativa enquanto a notícia ainda não foi criada no servidor.</small>}
      {immutable && <div className="warning-box">Conteúdo publicado ou arquivado é imutável neste fluxo. Uma nova versão editorial deve ser criada.</div>}
      <button className="action-button" disabled={!canCreate || busy || immutable}>{busy ? "Salvando…" : articleId ? "Salvar alterações" : "Criar rascunho"}</button>
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
