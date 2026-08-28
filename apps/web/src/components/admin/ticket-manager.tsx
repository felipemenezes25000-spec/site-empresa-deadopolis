"use client";

import { useEffect, useState, type FormEvent } from "react";
import { StatusBadge } from "@/components/ui";

type Ticket = { id: string; protocol: string; requesterName: string; category: string; priority: string; status: string; openedAt: string; firstResponseDueAt: string; resolutionDueAt: string; firstResponseAt: string | null; resolvedAt: string | null };
type TicketComment = { id: string; body: string; isInternal: boolean; createdAt: string; author: string };
type TicketDetail = Ticket & { contact: string; description: string; comments: TicketComment[] };
type Violation = { id: string; protocol: string; firstResponseBreached: boolean; resolutionBreached: boolean };

const priorities = [["CRITICAL", "Crítica · 1h/4h"], ["HIGH", "Alta · 4h/16h"], ["NORMAL", "Normal · 8h/40h"], ["LOW", "Baixa · 16h/80h"]] as const;
const priorityLabels: Record<string, string> = { CRITICAL: "Crítica", HIGH: "Alta", NORMAL: "Normal", LOW: "Baixa" };

// O filtro logo acima já oferece "Aberto / Em atendimento / Resolvido"; o selo mostrava o enum
// cru em inglês na mesma tela. O rótulo passa a ser o mesmo vocabulário nos dois lugares.
const ticketStatusLabels: Record<string, string> = { OPEN: "Aberto", IN_PROGRESS: "Em atendimento", RESOLVED: "Resolvido" };

function ticketStatusLabel(status: string) {
  return ticketStatusLabels[status.trim().toUpperCase()] ?? status;
}

export function TicketManager() {
  const [items, setItems] = useState<Ticket[]>([]);
  const [violations, setViolations] = useState<Violation[]>([]);
  const [violationsState, setViolationsState] = useState<"READY" | "ERROR">("READY");
  const [listState, setListState] = useState<"LOADING" | "READY" | "ERROR">("LOADING");
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [detail, setDetail] = useState<TicketDetail | null>(null);
  const [detailState, setDetailState] = useState<"LOADING" | "READY" | "ERROR">("LOADING");
  const [statusFilter, setStatusFilter] = useState("");
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);
  const [queueToken, setQueueToken] = useState(0);
  const [detailToken, setDetailToken] = useState(0);

  useEffect(() => {
    const controller = new AbortController();
    void Promise.all([
      fetch("/api/v1/admin/tickets", { signal: controller.signal }),
      fetch("/api/v1/admin/tickets/sla/violations", { signal: controller.signal }),
    ]).then(async ([ticketsResponse, violationsResponse]) => {
      if (!ticketsResponse.ok) throw new Error("tickets");
      const tickets = await ticketsResponse.json() as Ticket[];
      // Uma consulta de SLA que falhou não é uma fila sem violações: sinalize a ausência de
      // resposta em vez de exibir um "tudo certo" que a plataforma não conseguiu apurar.
      const breaches = violationsResponse.ok ? await violationsResponse.json() as Violation[] : [];
      if (controller.signal.aborted) return;
      setItems(tickets);
      setViolations(breaches);
      setViolationsState(violationsResponse.ok ? "READY" : "ERROR");
      setListState("READY");
    }).catch(() => { if (!controller.signal.aborted) setListState("ERROR"); });
    return () => controller.abort();
  }, [queueToken]);

  useEffect(() => {
    if (!selectedId) return;
    const controller = new AbortController();
    void fetch(`/api/v1/admin/tickets/${selectedId}`, { signal: controller.signal })
      .then(async (response) => {
        if (!response.ok) throw new Error("detail");
        const next = await response.json() as TicketDetail;
        if (controller.signal.aborted) return;
        setDetail(next);
        setDetailState("READY");
      })
      .catch(() => { if (!controller.signal.aborted) { setDetail(null); setDetailState("ERROR"); } });
    return () => controller.abort();
  }, [detailToken, selectedId]);

  function select(id: string) {
    setMessage("");
    if (id === selectedId) { setSelectedId(null); setDetail(null); return; }
    setDetail(null);
    setDetailState("LOADING");
    setSelectedId(id);
  }

  function refresh() {
    setQueueToken((current) => current + 1);
    setDetailToken((current) => current + 1);
  }

  async function act(id: string, path: string, init: RequestInit, success: string) {
    setBusy(true);
    const response = await fetch(`/api/v1/admin/tickets/${id}${path}`, init);
    setMessage(response.ok ? success : await errorText(response));
    if (response.ok) refresh();
    setBusy(false);
    return response.ok;
  }

  async function reply(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!detail) return;
    const form = event.currentTarget;
    const data = new FormData(form);
    const body = String(data.get("body") ?? "").trim();
    if (!body) { setMessage("Escreva a resposta antes de registrar."); return; }
    const internal = data.get("internal") === "on";
    const sent = await act(detail.id, "/comments", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ body, internal }) },
      internal ? "Nota interna registrada; ela não aparece no acompanhamento do cidadão." : "Resposta publicada no acompanhamento do cidadão.");
    if (sent) form.reset();
  }

  const visible = statusFilter ? items.filter((item) => item.status === statusFilter) : items;
  const breached = new Set(violations.map((violation) => violation.id));

  return <>
    <section className="admin-panel">
      <div className="resource-toolbar"><h2>Fila de atendimento</h2><label className="field">Status<select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)}><option value="">Todos</option><option value="OPEN">Aberto</option><option value="IN_PROGRESS">Em atendimento</option><option value="RESOLVED">Resolvido</option></select></label></div>
      {message && <div className="form-message" role="status" aria-live="polite">{message}</div>}
      {listState === "LOADING" && <p role="status" aria-live="polite">Carregando fila de atendimento…</p>}
      {listState === "ERROR" && <div className="form-message error" role="alert">Não foi possível carregar a fila. <button type="button" className="action-button secondary" onClick={refresh}>Tentar novamente</button></div>}
      {listState === "READY" && visible.length === 0 && <div className="empty-state"><h3>Nenhum ticket nesta visão</h3><p>Novas manifestações da Ouvidoria aparecerão aqui.</p></div>}
      {listState === "READY" && visible.length > 0 && <div className="table-scroll"><table className="admin-table">
        <thead><tr><th>Protocolo</th><th>Solicitante</th><th>Prioridade</th><th>Status</th><th>SLA resolução</th><th>Ações</th></tr></thead>
        <tbody>{visible.map((item) => <tr key={item.id}>
          <td>{item.protocol}<br /><small>{item.category}</small></td>
          <td>{item.requesterName}</td>
          <td>{priorityLabels[item.priority.toUpperCase()] ?? item.priority}</td>
          <td><StatusBadge status={ticketStatusLabel(item.status)} />{breached.has(item.id) && <><br /><small className="text-danger">SLA estourado</small></>}</td>
          <td>{formatDateTime(item.resolutionDueAt)}</td>
          <td><button type="button" className="action-button secondary" aria-expanded={selectedId === item.id} onClick={() => select(item.id)}>{selectedId === item.id ? "Fechar" : "Atender"}</button></td>
        </tr>)}</tbody>
      </table></div>}
    </section>

    {selectedId && <section className="admin-panel editor-fields" aria-label="Atendimento do ticket selecionado">
      {detailState === "LOADING" && <p role="status" aria-live="polite">Carregando manifestação…</p>}
      {detailState === "ERROR" && <div className="form-message error" role="alert">Não foi possível carregar a manifestação. <button type="button" className="action-button secondary" onClick={refresh}>Tentar novamente</button></div>}
      {detailState === "READY" && detail && <>
        <div className="resource-toolbar"><div><h2>{detail.protocol}</h2><p>{detail.category} · aberta em {formatDateTime(detail.openedAt)}</p></div><StatusBadge status={detail.status} /></div>
        <dl className="definition-list">
          <div style={{ display: "contents" }}><dt>Solicitante</dt><dd>{detail.requesterName}</dd></div>
          <div style={{ display: "contents" }}><dt>Contato</dt><dd>{detail.contact}</dd></div>
          <div style={{ display: "contents" }}><dt>Primeira resposta</dt><dd>{detail.firstResponseAt ? formatDateTime(detail.firstResponseAt) : `pendente — prazo ${formatDateTime(detail.firstResponseDueAt)}`}</dd></div>
          <div style={{ display: "contents" }}><dt>Conclusão</dt><dd>{detail.resolvedAt ? formatDateTime(detail.resolvedAt) : `pendente — prazo ${formatDateTime(detail.resolutionDueAt)}`}</dd></div>
        </dl>
        <div><h3>Descrição do cidadão</h3><p className="ticket-description">{detail.description}</p></div>

        <div><h3>Histórico de atendimento</h3>{detail.comments.length === 0 ? <p className="text-muted">Nenhuma resposta ou nota registrada.</p> : <ol className="ticket-response-list">{detail.comments.map((comment) => <li key={comment.id}><time dateTime={comment.createdAt}>{formatDateTime(comment.createdAt)}</time> · <strong>{comment.author}</strong> · <span className="status-pill">{comment.isInternal ? "Nota interna" : "Resposta ao cidadão"}</span><p>{comment.body}</p></li>)}</ol>}</div>

        <form className="editor-fields" onSubmit={reply}>
          <h3>Registrar resposta</h3>
          <label className="field" htmlFor="ticket-reply-body">Texto da resposta</label>
          <textarea id="ticket-reply-body" name="body" rows={4} maxLength={8000} required />
          <label><input type="checkbox" name="internal" /> Nota interna (não aparece no acompanhamento público)</label>
          <button className="action-button" disabled={busy}>Registrar</button>
        </form>

        <div className="button-row">
          <label className="field">Prioridade e SLA<select aria-label={`Prioridade de ${detail.protocol}`} value={detail.priority.toUpperCase()} disabled={busy} onChange={(event) => void act(detail.id, "/priority", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ priority: event.target.value }) }, "Prioridade alterada e prazos de SLA recalculados.")}>{priorities.map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
          {detail.status !== "RESOLVED"
            ? <button type="button" className="action-button self-end" disabled={busy} onClick={() => void act(detail.id, "/resolve", { method: "POST" }, "Ticket resolvido e prazo de conclusão cumprido.")}>Resolver</button>
            : <button type="button" className="action-button secondary self-end" disabled={busy} onClick={() => void act(detail.id, "/reopen", { method: "POST" }, "Ticket reaberto para novo atendimento.")}>Reabrir</button>}
        </div>
      </>}
    </section>}

    <section className="admin-panel"><h2>Violações de SLA</h2>{violationsState === "ERROR"
      ? <div className="warning-box" role="alert"><p>Não foi possível apurar as violações de SLA agora.</p><p>Este painel não está afirmando que a fila está em dia — a consulta de prazos não respondeu. Recarregue para tentar novamente.</p></div>
      : violations.length === 0
      ? <div className="ok-box">Nenhuma violação identificada agora.</div>
      : <div className="warning-box"><p>{violations.length} ticket(s) exigem atenção imediata.</p><ul>{violations.map((violation) => <li key={violation.id}>{violation.protocol} — {[violation.firstResponseBreached && "primeira resposta vencida", violation.resolutionBreached && "conclusão vencida"].filter(Boolean).join(" e ")}</li>)}</ul></div>}
    </section>
  </>;
}

function formatDateTime(value: string) {
  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? value : new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "short" }).format(parsed);
}

async function errorText(response: Response) {
  const body = await response.json().catch(() => null) as { title?: string; detail?: string; errors?: Record<string, string[]> } | null;
  const validation = Object.values(body?.errors ?? {}).flat().join(" ");
  return body?.detail ?? body?.title ?? (validation || `Erro ${response.status}`);
}
