"use client";

import { useState, type FormEvent } from "react";

type TrackedComment = { body: string; createdAt: string };
type TrackedTicket = {
  protocol: string;
  category: string;
  status: string;
  openedAt: string;
  firstResponseDueAt: string;
  resolutionDueAt: string;
  firstResponseAt: string | null;
  resolvedAt: string | null;
  comments: TrackedComment[];
};

const statusLabels: Record<string, string> = {
  OPEN: "Aberta — aguardando primeira resposta",
  IN_PROGRESS: "Em atendimento",
  RESOLVED: "Respondida e encerrada",
};

export function TicketTracker() {
  const [ticket, setTicket] = useState<TrackedTicket | null>(null);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const protocol = String(form.get("protocol") ?? "").trim();
    const code = String(form.get("trackingCode") ?? "").trim();
    if (!protocol || !code) {
      setError("Informe o protocolo e o código de acompanhamento recebidos no registro.");
      return;
    }
    setError("");
    setTicket(null);
    setLoading(true);
    try {
      const response = await fetch(`/api/v1/tickets/${encodeURIComponent(protocol)}?code=${encodeURIComponent(code)}`, { signal: AbortSignal.timeout(20_000) });
      if (response.ok) setTicket(await response.json() as TrackedTicket);
      else if (response.status === 404) setError("Nenhuma manifestação corresponde ao protocolo e código informados.");
      else if (response.status === 429) setError("Muitas consultas em sequência. Aguarde um minuto e tente novamente.");
      else setError("Não foi possível consultar a manifestação agora. Tente novamente em instantes.");
    } catch {
      setError("Falha de conexão ao consultar a manifestação. Verifique a rede e tente novamente.");
    }
    setLoading(false);
  }

  return <>
    <form className="ticket-form" onSubmit={submit}>
      <div className="field"><label htmlFor="tracking-protocol">Protocolo</label><input id="tracking-protocol" name="protocol" required maxLength={60} autoComplete="off" placeholder="DEO-20260827-A1B2C3D4" /></div>
      <div className="field"><label htmlFor="tracking-code">Código de acompanhamento</label><input id="tracking-code" name="trackingCode" required maxLength={64} autoComplete="off" /></div>
      {error && <div className="form-message error" role="alert">{error}</div>}
      <button className="action-button" disabled={loading}>{loading ? "Consultando…" : "Consultar manifestação"}</button>
      <small>Os dois códigos foram exibidos no momento do registro. Sem eles a Prefeitura não pode revelar o conteúdo da manifestação.</small>
    </form>
    <div aria-live="polite">{ticket && <TicketStatus ticket={ticket} />}</div>
  </>;
}

function TicketStatus({ ticket }: { ticket: TrackedTicket }) {
  return <section className="ticket-status" aria-label={`Situação da manifestação ${ticket.protocol}`}>
    <h3>Manifestação {ticket.protocol}</h3>
    <p className="ticket-status-line"><strong>{statusLabels[ticket.status] ?? ticket.status}</strong></p>
    <dl className="definition-list">
      <div style={{ display: "contents" }}><dt>Tipo</dt><dd>{ticket.category}</dd></div>
      <div style={{ display: "contents" }}><dt>Registrada em</dt><dd>{formatDateTime(ticket.openedAt)}</dd></div>
      <div style={{ display: "contents" }}><dt>Prazo de primeira resposta</dt><dd>{ticket.firstResponseAt ? `respondida em ${formatDateTime(ticket.firstResponseAt)}` : `até ${formatDateTime(ticket.firstResponseDueAt)}`}</dd></div>
      <div style={{ display: "contents" }}><dt>Prazo de conclusão</dt><dd>{ticket.resolvedAt ? `concluída em ${formatDateTime(ticket.resolvedAt)}` : `até ${formatDateTime(ticket.resolutionDueAt)}`}</dd></div>
    </dl>
    <h4>Respostas da Prefeitura</h4>
    {ticket.comments.length > 0
      ? <ol className="ticket-response-list">{ticket.comments.map((comment) => <li key={`${comment.createdAt}-${comment.body.slice(0, 24)}`}><time dateTime={comment.createdAt}>{formatDateTime(comment.createdAt)}</time><p>{comment.body}</p></li>)}</ol>
      : <p className="text-muted">Ainda não há resposta pública registrada para esta manifestação.</p>}
  </section>;
}

function formatDateTime(value: string) {
  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? value : new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "short" }).format(parsed);
}
