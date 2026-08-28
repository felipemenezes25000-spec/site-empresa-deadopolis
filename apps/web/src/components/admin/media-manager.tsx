"use client";

import { useEffect, useRef, useState, type FormEvent } from "react";
import { SearchField, StatusBadge } from "@/components/ui";
import { ResponsiveMediaImage } from "@/components/portal/responsive-media-image";
import { MediaFramingEditor, type FramingPayload } from "./media-framing-editor";
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

  async function savePresentation(payload: FramingPayload) {
    if (!selected) return;
    setBusy(true);
    const response = await fetch(`/api/v1/admin/media/${selected.id}/presentation`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
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
      {/* "Aprovada" é o veredito do provedor antimalware configurado, não uma garantia absoluta.
          O estado real do provedor vive em Compliance; referenciá-lo evita fixar aqui um valor que
          deixaria de ser verdade assim que um scanner de produção fosse contratado. */}
      <p className="muted-note">A aprovação registra o resultado do provedor antimalware configurado neste ambiente. O provedor em vigor e seu estado aparecem em Compliance › Capacidades do runtime.</p>
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
          {selected.mimeType.startsWith("image/") && selected.status === "APPROVED" && <MediaFramingEditor key={`presentation-${selected.id}-${selected.tagsCsv}-${selected.focalPointX}-${selected.focalPointY}-${selected.cropX}-${selected.cropY}-${selected.cropWidth}-${selected.cropHeight}`} asset={selected} busy={busy} onSave={savePresentation} />}
          {selected.mimeType.startsWith("image/") && selected.status !== "APPROVED" && <div className="empty-state"><h3>Enquadramento indisponível</h3><p>O ponto focal e o recorte só podem ser ajustados depois que o scanner aprovar a mídia.</p></div>}
        </div>
      </div>
      <div className="button-row">
        {selected.status !== "APPROVED" && <button type="button" className="action-button" disabled={busy || selected.status === "REJECTED"} onClick={() => void review()}>Revalidar com scanner e aprovar</button>}
        {selected.status !== "REJECTED" && <><label className="field" style={{ minWidth: 280 }}>Motivo da rejeição<input value={rejectReason} onChange={(event) => setRejectReason(event.target.value)} maxLength={500} placeholder="Opcional, fica registrado na auditoria" /></label><button type="button" className="action-button secondary" disabled={busy} onClick={() => void reject()}>Rejeitar mídia</button></>}
      </div>
    </section>}
  </div>;
}

function percent(value: number | null | undefined, fallback = 50) { return Math.round((value ?? fallback / 100) * 100); }
function shortHash(value: string) { return value ? `SHA ${value.slice(0, 12)}…` : "SHA —"; }
function formatDate(value: string) { const date = new Date(value); return Number.isNaN(date.getTime()) ? value : new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "short" }).format(date); }
async function errorText(response: Response) { const body = await response.json().catch(() => null) as { title?: string; detail?: string; errors?: Record<string, string[]> } | null; const validation = Object.values(body?.errors ?? {}).flat().join(" "); return body?.detail ?? body?.title ?? (validation || `Erro ${response.status}`); }
