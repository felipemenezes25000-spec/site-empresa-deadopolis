"use client";

import { useEffect, useRef, useState, type FormEvent } from "react";
import { SearchField, StatusBadge } from "@/components/ui";
import { ResponsiveMediaImage } from "@/components/portal/responsive-media-image";
import { MediaUploader } from "./media-uploader";

const PAGE_SIZE = 24;

type Asset = {
  id: string;
  originalFileName: string;
  mimeType: string;
  sizeBytes: number;
  sha256: string;
  status: string;
  altText: string;
  caption: string;
  credit: string;
  tagsCsv?: string;
  focalPointX?: number | null;
  focalPointY?: number | null;
  cropX?: number | null;
  cropY?: number | null;
  cropWidth?: number | null;
  cropHeight?: number | null;
  uploadedAt: string;
};

export function MediaManager() {
  const [items, setItems] = useState<Asset[]>([]);
  const [message, setMessage] = useState("");
  const [query, setQuery] = useState("");
  const [appliedQuery, setAppliedQuery] = useState("");
  const [status, setStatus] = useState("");
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);
  const [listState, setListState] = useState<"LOADING" | "READY" | "ERROR">("LOADING");
  const [reloadToken, setReloadToken] = useState(0);
  const preferredSelection = useRef<string | null>(null);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [rejectReason, setRejectReason] = useState("");
  const [busy, setBusy] = useState(false);
  const selected = items.find((item) => item.id === selectedId) ?? null;
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE));

  useEffect(() => {
    const controller = new AbortController();
    const parameters = new URLSearchParams({ page: String(page), pageSize: String(PAGE_SIZE) });
    if (appliedQuery) parameters.set("q", appliedQuery);
    if (status) parameters.set("status", status);
    void fetch(`/api/v1/admin/media?${parameters}`, { signal: controller.signal })
      .then(async (response) => {
        if (!response.ok) throw new Error("media");
        const next = await response.json() as Asset[];
        if (!controller.signal.aborted) {
          setItems(next);
          setTotal(Number(response.headers.get("X-Total-Count") ?? next.length));
          setListState("READY");
          const preferredId = preferredSelection.current;
          setSelectedId((current) => {
            const candidate = preferredId ?? current;
            return candidate && next.some((item) => item.id === candidate) ? candidate : null;
          });
          preferredSelection.current = null;
        }
      })
      .catch(() => { if (!controller.signal.aborted) setListState("ERROR"); });
    return () => controller.abort();
  }, [appliedQuery, page, reloadToken, status]);

  function refresh(preferredId?: string) {
    if (preferredId) preferredSelection.current = preferredId;
    setListState("LOADING");
    setReloadToken((current) => current + 1);
  }

  function uploaded(preferredId?: string) {
    setQuery("");
    setAppliedQuery("");
    setStatus("");
    setPage(1);
    refresh(preferredId);
  }

  function changePage(nextPage: number) {
    setListState("LOADING");
    setPage(nextPage);
  }

  async function saveMetadata(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selected) return;
    setBusy(true);
    const form = new FormData(event.currentTarget);
    const response = await fetch(`/api/v1/admin/media/${selected.id}/metadata`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ altText: form.get("altText"), caption: form.get("caption"), credit: form.get("credit") }),
    });
    setMessage(response.ok ? "Metadados de acessibilidade e crédito atualizados." : await errorText(response));
    if (response.ok) refresh(selected.id);
    setBusy(false);
  }

  async function savePresentation(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selected) return;
    const form = new FormData(event.currentTarget);
    const cropEnabled = form.get("cropEnabled") === "on";
    const normalized = (name: string) => Number(form.get(name) ?? 0) / 100;
    setBusy(true);
    const response = await fetch(`/api/v1/admin/media/${selected.id}/presentation`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        tags: form.get("tags"),
        focalPointX: normalized("focalPointX"),
        focalPointY: normalized("focalPointY"),
        cropX: cropEnabled ? normalized("cropX") : null,
        cropY: cropEnabled ? normalized("cropY") : null,
        cropWidth: cropEnabled ? normalized("cropWidth") : null,
        cropHeight: cropEnabled ? normalized("cropHeight") : null,
      }),
    });
    setMessage(response.ok ? "Tags, ponto focal e recorte governado atualizados." : await errorText(response));
    if (response.ok) refresh(selected.id);
    setBusy(false);
  }

  async function review() {
    if (!selected) return;
    setBusy(true);
    const response = await fetch(`/api/v1/admin/media/${selected.id}/review`, { method: "POST" });
    setMessage(response.ok ? "Arquivo revalidado pelo scanner e aprovado para uso público." : await errorText(response));
    refresh(selected.id);
    setBusy(false);
  }

  async function reject() {
    if (!selected) return;
    setBusy(true);
    const response = await fetch(`/api/v1/admin/media/${selected.id}/reject`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ reason: rejectReason || null }),
    });
    setMessage(response.ok ? "Mídia rejeitada e retirada da leitura pública." : await errorText(response));
    if (response.ok) setRejectReason("");
    refresh(selected.id);
    setBusy(false);
  }

  return <div className="editor-grid">
    <MediaUploader onUploaded={uploaded} />

    <section className="admin-panel">
      <h2>Biblioteca</h2>
      <SearchField value={query} onChange={setQuery} onSubmit={(value) => { setAppliedQuery(value.trim()); setPage(1); refresh(); }} label="Filtrar mídia" placeholder="Nome, tipo, ALT, legenda, crédito ou tags" />
      <label className="field mt-3">Status<select value={status} onChange={(event) => { setStatus(event.target.value); setPage(1); refresh(); }}><option value="">Todos</option><option value="APPROVED">Aprovada</option><option value="QUARANTINED">Em quarentena</option><option value="REJECTED">Rejeitada</option></select></label>
      {listState === "LOADING" && <p role="status" aria-live="polite">Carregando biblioteca…</p>}
      {listState === "ERROR" && <div className="form-message error" role="alert">Não foi possível carregar a biblioteca. <button type="button" className="action-button secondary" onClick={() => refresh()}>Tentar novamente</button></div>}
      {listState === "READY" && <div className="compact-list">{items.map((item) => <div className="compact-item" key={item.id}>
        <div><strong>{item.originalFileName}</strong><small style={{ display: "block" }}>{item.mimeType} · {(item.sizeBytes / 1024).toFixed(0)} KB · {shortHash(item.sha256)}</small>{item.tagsCsv && <small style={{ display: "block" }}>Tags: {item.tagsCsv}</small>}</div>
        <div className="button-row"><StatusBadge status={item.status} /><button type="button" className="action-button secondary" onClick={() => { setSelectedId(item.id); setRejectReason(""); setMessage(""); }}>Revisar</button></div>
      </div>)}</div>}
      {listState === "READY" && items.length === 0 && <p className="text-muted" role="status">Nenhuma mídia corresponde aos filtros.</p>}
      {listState === "READY" && <nav className="pagination-row" aria-label="Paginação da biblioteca"><button type="button" className="action-button secondary" disabled={page <= 1} onClick={() => changePage(Math.max(1, page - 1))}>Página anterior</button><span>Página {page} de {totalPages} · {total} itens</span><button type="button" className="action-button secondary" disabled={page >= totalPages} onClick={() => changePage(page + 1)}>Próxima página</button></nav>}
    </section>

    {selected && <section className="admin-panel editor-fields" style={{ gridColumn: "1 / -1" }}>
      <div className="resource-toolbar"><div><h2>Revisão da mídia</h2><p>{selected.originalFileName}</p></div><StatusBadge status={selected.status} /></div>
      {message && <div className="form-message" role="status" aria-live="polite">{message}</div>}
      <div className="editor-grid">
        <div>
          {selected.status === "APPROVED" && selected.mimeType.startsWith("image/") && <div style={{ overflow: "hidden", borderRadius: 12, aspectRatio: "3 / 2" }}><ResponsiveMediaImage className="media-review-image" src={`/api/v1/media/${selected.id}`} alt={selected.altText || "Prévia da mídia aprovada"} width={1200} height={800} sizes="(max-width: 900px) 100vw, 720px" style={{ objectPosition: `${percent(selected.focalPointX)}% ${percent(selected.focalPointY)}%` }} /></div>}
          {selected.status === "APPROVED" && selected.mimeType === "application/pdf" && <a className="action-button secondary" href={`/api/v1/media/${selected.id}`} target="_blank" rel="noopener noreferrer">Abrir PDF aprovado</a>}
          {selected.status !== "APPROVED" && <div className="empty-state"><h3>Prévia pública bloqueada</h3><p>A mídia só pode ser lida pela rota pública depois de aprovação por scanner ou enquanto permanecer aprovada.</p></div>}
          <dl className="definition-list"><div style={{ display: "contents" }}><dt>SHA-256</dt><dd style={{ wordBreak: "break-all" }}>{selected.sha256}</dd></div><div style={{ display: "contents" }}><dt>Enviado em</dt><dd>{formatDate(selected.uploadedAt)}</dd></div></dl>
        </div>
        <div className="editor-fields">
          <form key={`metadata-${selected.id}-${selected.altText}-${selected.caption}-${selected.credit}`} className="editor-fields" onSubmit={saveMetadata}>
            <h3>Acessibilidade e crédito</h3>
            <label className="field">Texto alternativo<input name="altText" defaultValue={selected.altText} maxLength={500} /></label>
            <label className="field">Legenda<textarea name="caption" defaultValue={selected.caption} maxLength={1000} rows={3} /></label>
            <label className="field">Crédito<input name="credit" defaultValue={selected.credit} maxLength={500} /></label>
            <button className="action-button" disabled={busy}>Salvar metadados</button>
          </form>
          {selected.mimeType.startsWith("image/") && <form key={`presentation-${selected.id}-${selected.tagsCsv}-${selected.focalPointX}-${selected.focalPointY}-${selected.cropX}-${selected.cropY}-${selected.cropWidth}-${selected.cropHeight}`} className="editor-fields" onSubmit={savePresentation}>
            <h3>Enquadramento editorial</h3>
            <label className="field">Tags<input name="tags" defaultValue={selected.tagsCsv ?? ""} maxLength={2000} placeholder="saúde, obras, evento" /><small>Até 20 tags, separadas por vírgula.</small></label>
            <label className="field">Ponto focal horizontal · {percent(selected.focalPointX)}%<input name="focalPointX" type="range" min="0" max="100" step="1" defaultValue={percent(selected.focalPointX)} /></label>
            <label className="field">Ponto focal vertical · {percent(selected.focalPointY)}%<input name="focalPointY" type="range" min="0" max="100" step="1" defaultValue={percent(selected.focalPointY)} /></label>
            <label><input name="cropEnabled" type="checkbox" defaultChecked={hasCrop(selected)} /> Definir recorte editorial normalizado</label>
            <div className="editor-grid">
              <label className="field">X (%)<input name="cropX" type="number" min="0" max="100" step="1" defaultValue={percent(selected.cropX, 0)} /></label>
              <label className="field">Y (%)<input name="cropY" type="number" min="0" max="100" step="1" defaultValue={percent(selected.cropY, 0)} /></label>
              <label className="field">Largura (%)<input name="cropWidth" type="number" min="1" max="100" step="1" defaultValue={percent(selected.cropWidth, 100)} /></label>
              <label className="field">Altura (%)<input name="cropHeight" type="number" min="1" max="100" step="1" defaultValue={percent(selected.cropHeight, 100)} /></label>
            </div>
            <small>O recorte é salvo como coordenadas 0..1 e não destrói o original. Derivados WebP/AVIF só serão declarados quando houver encoder testado no runtime de produção.</small>
            <button className="action-button" disabled={busy}>Salvar enquadramento</button>
          </form>}
        </div>
      </div>
      <div className="button-row">
        {selected.status !== "APPROVED" && <button type="button" className="action-button" disabled={busy || selected.status === "REJECTED"} onClick={() => void review()}>Revalidar com scanner e aprovar</button>}
        {selected.status !== "REJECTED" && <><label className="field" style={{ minWidth: 280 }}>Motivo da rejeição<input value={rejectReason} onChange={(event) => setRejectReason(event.target.value)} maxLength={500} placeholder="Opcional, fica registrado na auditoria" /></label><button type="button" className="action-button secondary" disabled={busy} onClick={() => void reject()}>Rejeitar mídia</button></>}
      </div>
    </section>}
  </div>;
}

function hasCrop(asset: Asset) { return [asset.cropX, asset.cropY, asset.cropWidth, asset.cropHeight].every((value) => typeof value === "number"); }
function percent(value: number | null | undefined, fallback = 50) { return Math.round((value ?? fallback / 100) * 100); }
function shortHash(value: string) { return value ? `SHA ${value.slice(0, 12)}…` : "SHA —"; }
function formatDate(value: string) { const date = new Date(value); return Number.isNaN(date.getTime()) ? value : new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "short" }).format(date); }
async function errorText(response: Response) { const body = await response.json().catch(() => null) as { title?: string; detail?: string; errors?: Record<string, string[]> } | null; const validation = Object.values(body?.errors ?? {}).flat().join(" "); return body?.detail ?? body?.title ?? (validation || `Erro ${response.status}`); }
